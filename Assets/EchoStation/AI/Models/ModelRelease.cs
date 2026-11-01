using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace EchoStation.AI.Models
{
 public enum ModelPackageError { MissingFile,InvalidManifest,UnsupportedVersion,WrongRelease,UnexpectedFile,UnsafePath,TooLarge,HashMismatch,Unreadable }
 public sealed class ModelPackageException:Exception
 {
  public ModelPackageError Code {get;}
  public ModelPackageException(ModelPackageError code,string message,Exception inner=null):base(message,inner){Code=code;}
 }
 public sealed class ModelFileSpec
 {
  public string Name {get;}
  public long Bytes {get;}
  public string Sha256 {get;}
  public ModelFileSpec(string name,long bytes,string sha256)
  {
   if(!IsSafeName(name)||name=="manifest.json"||bytes<0||!Regex.IsMatch(sha256??"",@"\A[0-9a-f]{64}\z"))throw new ArgumentException("Invalid trusted model file specification");
   Name=name;Bytes=bytes;Sha256=sha256;
  }
  internal static bool IsSafeName(string value)=>value!=null&&value.Length<=100&&value!="."&&value!=".."&&Regex.IsMatch(value,@"\A[a-zA-Z0-9_][a-zA-Z0-9_.-]*\z");
 }
 public sealed class ModelRelease
 {
  public const long MaximumBytes=1L<<30;
  public string Id {get;}
  public IReadOnlyDictionary<string,ModelFileSpec> Files {get;}
  public ModelRelease(string id,IEnumerable<ModelFileSpec> files)
  {
   if(!ModelFileSpec.IsSafeName(id))throw new ArgumentException("Invalid trusted release ID");
   var values=(files??throw new ArgumentNullException(nameof(files))).ToArray();
   if(values.Length<1||values.Length>16||values.Any(f=>f==null)||values.Select(f=>f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=values.Length||!values.Any(f=>f.Name=="model.onnx"))throw new ArgumentException("Invalid trusted file inventory");
   long total=0;foreach(var file in values){if(file.Bytes>MaximumBytes-total)throw new ArgumentException("Trusted release exceeds1GiB");total+=file.Bytes;}
   Id=id;Files=new ReadOnlyDictionary<string,ModelFileSpec>(values.ToDictionary(f=>f.Name,StringComparer.Ordinal));
  }
 }
}
