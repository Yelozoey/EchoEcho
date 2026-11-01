using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EchoStation.Memory;

namespace EchoStation.AI.Models
{
 public sealed class VerifiedModelPackage:IDisposable
 {
  private readonly Dictionary<string,FileStream> held;
  public string DirectoryPath {get;}
  public string ReleaseId {get;}
  public string ModelPath=>PathFor("model.onnx");
  public bool IsDisposed {get;private set;}
  internal VerifiedModelPackage(string root,string release,Dictionary<string,FileStream> files){DirectoryPath=root;ReleaseId=release;held=files;}
  public string PathFor(string name)
  {
   if(IsDisposed)throw new ObjectDisposedException(nameof(VerifiedModelPackage));
   if(!held.ContainsKey(name))throw new ArgumentException("File not in verified release");
   return Path.Combine(DirectoryPath,name);
  }
  public string ReadUtf8(string name)=>ReadUtf8(name,8*1024*1024);
  public string ReadUtf8(string name,int maximumBytes)
  {
   if(IsDisposed)throw new ObjectDisposedException(nameof(VerifiedModelPackage));
   if(maximumBytes<1||maximumBytes>16*1024*1024)throw new ArgumentOutOfRangeException(nameof(maximumBytes),"Verified text budget must be1..16MiB");
   if(!held.TryGetValue(name,out var stream)||stream.Length>maximumBytes)throw new InvalidOperationException("Not an allowed bounded text asset");
   stream.Position=0;using(var reader=new StreamReader(stream,new UTF8Encoding(false,true),true,65536,true))return reader.ReadToEnd();
  }
  public void Dispose(){if(IsDisposed)return;IsDisposed=true;foreach(var stream in held.Values)stream.Dispose();held.Clear();}
 }
 public static class ModelPackage
 {
  private static ModelPackageException Fail(ModelPackageError code,string message,Exception inner=null)=>new ModelPackageException(code,message,inner);
  public static VerifiedModelPackage Verify(string directory,ModelRelease trusted,CancellationToken cancellation=default)
  {
   if(trusted==null)throw new ArgumentNullException(nameof(trusted));
   var held=new Dictionary<string,FileStream>(StringComparer.Ordinal);
   try
   {
    cancellation.ThrowIfCancellationRequested();var root=Path.GetFullPath(directory);
    if(!Directory.Exists(root))throw Fail(ModelPackageError.MissingFile,"Model directory is missing");
    for(var ancestor=new DirectoryInfo(root);ancestor!=null;ancestor=ancestor.Parent)RejectLink(ancestor.FullName);
    var manifestPath=Path.Combine(root,"manifest.json");if(!File.Exists(manifestPath))throw Fail(ModelPackageError.MissingFile,"Model manifest is missing");RejectLink(manifestPath);
    using(var stream=new FileStream(manifestPath,FileMode.Open,FileAccess.Read,FileShare.Read))
    {
     if(stream.Length>65536)throw Fail(ModelPackageError.InvalidManifest,"Manifest exceeds64KiB");
     using(var text=new StreamReader(stream,new UTF8Encoding(false,true)))ValidateManifest(text.ReadToEnd(),trusted);
    }
    foreach(var entry in Directory.EnumerateFileSystemEntries(root))
    {
     RejectLink(entry);var name=Path.GetFileName(entry);
     var known=name=="manifest.json"||trusted.Files.ContainsKey(name);
     var meta=name.EndsWith(".meta",StringComparison.Ordinal)&&(name=="manifest.json.meta"||trusted.Files.ContainsKey(name.Substring(0,name.Length-5)));
     if(Directory.Exists(entry)||(!known&&!meta))throw Fail(ModelPackageError.UnexpectedFile,"Unlisted entry in model directory");
    }
    foreach(var file in trusted.Files.Values)
    {
     cancellation.ThrowIfCancellationRequested();var path=Path.Combine(root,file.Name);
     if(!File.Exists(path))throw Fail(ModelPackageError.MissingFile,"Required model asset is missing: "+file.Name);RejectLink(path);
     var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.SequentialScan);held.Add(file.Name,stream);
     if(stream.Length!=file.Bytes)throw Fail(ModelPackageError.HashMismatch,"Model asset size differs: "+file.Name);
     if(Hash(stream,cancellation)!=file.Sha256)throw Fail(ModelPackageError.HashMismatch,"Model asset hash differs: "+file.Name);stream.Position=0;
    }
    return new VerifiedModelPackage(root,trusted.Id,held);
   }
   catch(ModelPackageException){Dispose(held);throw;}
   catch(OperationCanceledException){Dispose(held);throw;}
   catch(Exception error) when(error is JsonException||error is OverflowException||error is FormatException||error is DecoderFallbackException){Dispose(held);throw Fail(ModelPackageError.InvalidManifest,"Model manifest is invalid",error);}
   catch(Exception error){Dispose(held);throw Fail(ModelPackageError.Unreadable,"Model package could not be read",error);}
  }
  public static void ValidateManifest(string json,ModelRelease trusted)
  {
   if(trusted==null)throw new ArgumentNullException(nameof(trusted));
   if(json==null||Encoding.UTF8.GetByteCount(json)>65536)throw Fail(ModelPackageError.InvalidManifest,"Invalid manifest length");
   try
   {
    MemoryJson.ValidateLexemes(json);
    using(var scan=new JsonTextReader(new StringReader(json)){MaxDepth=8,DateParseHandling=DateParseHandling.None,SupportMultipleContent=true})
    {while(scan.Read())if(scan.TokenType==JsonToken.Comment)throw Fail(ModelPackageError.InvalidManifest,"Comments are forbidden");}
    JObject manifest;
    using(var reader=new JsonTextReader(new StringReader(json)){MaxDepth=8,DateParseHandling=DateParseHandling.None,SupportMultipleContent=true})
    {manifest=JObject.Load(reader,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});if(reader.Read())throw Fail(ModelPackageError.InvalidManifest,"Multiple JSON values");}
    Exact(manifest,"version","releaseId","files");
    if(manifest["version"].Type!=JTokenType.Integer)throw Fail(ModelPackageError.InvalidManifest,"Integer manifest version required");
    if(manifest["version"].Value<long>()!=1)throw Fail(ModelPackageError.UnsupportedVersion,"Unsupported manifest version");
    if(manifest["releaseId"].Type!=JTokenType.String||manifest["releaseId"].Value<string>()!=trusted.Id)throw Fail(ModelPackageError.WrongRelease,"Release identity differs");
    var files=manifest["files"] as JArray;if(files==null||files.Count!=trusted.Files.Count)throw Fail(ModelPackageError.InvalidManifest,"File inventory count differs");
    var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach(var token in files)
    {
     var file=token as JObject;if(file==null)throw Fail(ModelPackageError.InvalidManifest,"File entry must be an object");Exact(file,"name","bytes","sha256");
     if(file["name"].Type!=JTokenType.String)throw Fail(ModelPackageError.InvalidManifest,"File name must be a string");var name=file["name"].Value<string>();
     if(!ModelFileSpec.IsSafeName(name))throw Fail(ModelPackageError.UnsafePath,"Unsafe file name");
     if(!seen.Add(name)||!trusted.Files.TryGetValue(name,out var expected))throw Fail(ModelPackageError.UnexpectedFile,"Untrusted or duplicate file name");
     if(file["bytes"].Type!=JTokenType.Integer||file["sha256"].Type!=JTokenType.String||file["bytes"].Value<long>()!=expected.Bytes||file["sha256"].Value<string>()!=expected.Sha256)throw Fail(ModelPackageError.HashMismatch,"Manifest differs from trusted release");
    }
   }
   catch(ModelPackageException){throw;}
   catch(Exception error) when(error is JsonException||error is OverflowException||error is FormatException||error is ArgumentException){throw Fail(ModelPackageError.InvalidManifest,"Invalid model manifest",error);}
  }
  private static void Exact(JObject value,params string[] names){if(value.Properties().Count()!=names.Length||value.Properties().Any(p=>!names.Contains(p.Name,StringComparer.Ordinal)))throw Fail(ModelPackageError.InvalidManifest,"Unknown or missing manifest fields");}
  private static void RejectLink(string path){if((File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)throw Fail(ModelPackageError.UnsafePath,"Reparse points are forbidden in model package");}
  private static void Dispose(Dictionary<string,FileStream> streams){foreach(var stream in streams.Values)stream.Dispose();streams.Clear();}
  private static string Hash(Stream stream,CancellationToken cancellation)
  {
   using(var sha=SHA256.Create())
   {var buffer=new byte[1024*1024];int count;while((count=stream.Read(buffer,0,buffer.Length))>0){cancellation.ThrowIfCancellationRequested();sha.TransformBlock(buffer,0,count,buffer,0);}sha.TransformFinalBlock(Array.Empty<byte>(),0,0);return BitConverter.ToString(sha.Hash).Replace("-","").ToLowerInvariant();}
  }
 }
}
