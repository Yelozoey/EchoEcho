using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using EchoStation.AI.Models;
using EchoStation.AI.Probe;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class ModelPackageTests
 {
  private string root;private ModelRelease trusted;
  private static string Sha(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
  [SetUp]public void Setup()
  {
   root=Path.GetFullPath("Library/EchoStation/S3Tests/A01/"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
   var weights=Encoding.UTF8.GetBytes("small contract fixture; not a language model");var tokens=Encoding.UTF8.GetBytes("{\"fixture\":true}");
   File.WriteAllBytes(Path.Combine(root,"model.onnx"),weights);File.WriteAllBytes(Path.Combine(root,"tokenizer.json"),tokens);
   trusted=new ModelRelease("unit-release",new[]{new ModelFileSpec("model.onnx",weights.Length,Sha(weights)),new ModelFileSpec("tokenizer.json",tokens.Length,Sha(tokens))});Write(Manifest());
  }
  [TearDown]public void Cleanup()
  {
   var prefix=Path.GetFullPath("Library/EchoStation/S3Tests/A01")+Path.DirectorySeparatorChar;
   if(root==null||!Path.GetFullPath(root).StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Unsafe test cleanup");
   foreach(var directory in Directory.GetDirectories(root))Directory.Delete(directory,false); // Only empty test dirs or junction itself, never recursive.
   foreach(var file in Directory.GetFiles(root))File.Delete(file);Directory.Delete(root,false);
  }
  private JObject Manifest()=>new JObject{{"version",1},{"releaseId",trusted.Id},{"files",new JArray(trusted.Files.Values.Select(f=>new JObject{{"name",f.Name},{"bytes",f.Bytes},{"sha256",f.Sha256}}))}};
  private void Write(JObject value)=>File.WriteAllText(Path.Combine(root,"manifest.json"),value.ToString(Formatting.None));
  private void Reject(ModelPackageError expected)=>Assert.That(Assert.Throws<ModelPackageException>(()=>ModelPackage.Verify(root,trusted)).Code,Is.EqualTo(expected));
  [Test]public void ValidPackageHoldsReadLocksAndReleasesThem()
  {
   var path=Path.Combine(root,"model.onnx");
   using(var package=ModelPackage.Verify(root,trusted))
   {Assert.That(package.ModelPath,Is.EqualTo(path));Assert.That(package.ReadUtf8("tokenizer.json"),Is.EqualTo("{\"fixture\":true}"));Assert.Throws<IOException>(()=>{using(var write=new FileStream(path,FileMode.Open,FileAccess.Write,FileShare.ReadWrite)){};});Assert.Throws<ArgumentException>(()=>package.PathFor("other.onnx"));}
   using(var write=new FileStream(path,FileMode.Open,FileAccess.Write,FileShare.ReadWrite))Assert.That(write.CanWrite,Is.True);
  }
  [Test]public void MissingAndTamperedFilesFailAndDoNotLeaveLocks()
  {
   var path=Path.Combine(root,"model.onnx");var original=File.ReadAllBytes(path);var corrupt=(byte[])original.Clone();corrupt[0]^=1;File.WriteAllBytes(path,corrupt);Reject(ModelPackageError.HashMismatch);
   File.WriteAllBytes(path,original);using(var package=ModelPackage.Verify(root,trusted))Assert.That(package.IsDisposed,Is.False);
   File.Delete(path);Reject(ModelPackageError.MissingFile);
  }
  [Test]public void ManifestCannotReplaceTrustedHashesOrReleaseIdentity()
  {
   var m=Manifest();m["files"][0]["sha256"]=new string('0',64);Write(m);Reject(ModelPackageError.HashMismatch);
   m=Manifest();m["releaseId"]="another-release";Write(m);Reject(ModelPackageError.WrongRelease);
  }
  [TestCase(2)][TestCase(-1)]public void OtherManifestVersionsAreRejected(int version){var m=Manifest();m["version"]=version;Write(m);Reject(ModelPackageError.UnsupportedVersion);}
  [TestCase("single")][TestCase("unquoted")][TestCase("comment")][TestCase("trailing")][TestCase("multiple")][TestCase("duplicate")]
  public void JavascriptAndMultipleJsonAreRejected(string kind)
  {
   var json=Manifest().ToString(Formatting.None);
   switch(kind){case "single":json=json.Replace('"','\'');break;case "unquoted":json=json.Replace("\"version\":","version:");break;case "comment":json="/*x*/"+json;break;case "trailing":json=json.Substring(0,json.Length-1)+",}";break;case "multiple":json+=json;break;case "duplicate":json="{\"version\":1,"+json.Substring(1);break;}
   File.WriteAllText(Path.Combine(root,"manifest.json"),json);Reject(ModelPackageError.InvalidManifest);
  }
  [Test]public void UnknownFieldsWrongTypesAndDuplicateFileNamesAreRejected()
  {
   var m=Manifest();m["url"]="https://invalid.example/never-requested";Write(m);Reject(ModelPackageError.InvalidManifest);
   m=Manifest();m["version"]="1";Write(m);Reject(ModelPackageError.InvalidManifest);
   m=Manifest();m["files"][1]["name"]=m["files"][0]["name"].DeepClone();Write(m);Reject(ModelPackageError.UnexpectedFile);
  }
  [TestCase("../model.onnx")][TestCase("E:\\model.onnx")][TestCase("/model.onnx")]
  public void TraversalAndAbsoluteNamesAreRejected(string name){var m=Manifest();m["files"][0]["name"]=name;Write(m);Reject(ModelPackageError.UnsafePath);}
  [Test]public void ExtraFilesAndDirectoriesAreRejectedButExactUnityMetaIsIgnored()
  {
   var extra=Path.Combine(root,"extra.dll");File.WriteAllText(extra,"never loaded");Reject(ModelPackageError.UnexpectedFile);File.Delete(extra);
   var directory=Path.Combine(root,"nested");Directory.CreateDirectory(directory);Reject(ModelPackageError.UnexpectedFile);Directory.Delete(directory,false);
   File.WriteAllText(Path.Combine(root,"model.onnx.meta"),"fileFormatVersion:2");using(var package=ModelPackage.Verify(root,trusted))Assert.That(package.ReleaseId,Is.EqualTo(trusted.Id));
  }
  [Test]public void RootJunctionIsRejectedWithoutFollowingIt()
  {
   var link=Path.Combine(root,"packageLink");
   var info=new System.Diagnostics.ProcessStartInfo("cmd.exe","/c mklink /J \""+link+"\" \""+root+"\""){CreateNoWindow=true,UseShellExecute=false,WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden,RedirectStandardOutput=true,RedirectStandardError=true};
   using(var process=System.Diagnostics.Process.Start(info)){var text=process.StandardOutput.ReadToEnd()+process.StandardError.ReadToEnd();Assert.That(process.WaitForExit(5000),Is.True);Assert.That(process.ExitCode,Is.EqualTo(0),text);}
   Assert.That(Assert.Throws<ModelPackageException>(()=>ModelPackage.Verify(link,trusted)).Code,Is.EqualTo(ModelPackageError.UnsafePath));
  }
  [Test]public void CancelledVerificationDoesNotHoldFiles()
  {
   using(var cancel=new CancellationTokenSource()){cancel.Cancel();Assert.Throws<OperationCanceledException>(()=>ModelPackage.Verify(root,trusted,cancel.Token));}
   using(var write=new FileStream(Path.Combine(root,"model.onnx"),FileMode.Open,FileAccess.Write,FileShare.ReadWrite))Assert.That(write.CanWrite,Is.True);
  }
  [Test]public void TrustedInventoryAndManifestSizeHaveHardBudgets()
  {
   Assert.Throws<ArgumentException>(()=>new ModelRelease("oversized",new[]{new ModelFileSpec("model.onnx",ModelRelease.MaximumBytes+1,new string('0',64))}));
   File.WriteAllText(Path.Combine(root,"manifest.json"),new string(' ',65537));Reject(ModelPackageError.InvalidManifest);
  }
  [UnityTest]public IEnumerator ActualPinnedReleasePassesFullByteHashAndNativeLoadWithReadLease()
  {
   var started=System.Diagnostics.Stopwatch.StartNew();var directory=Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+ModelCatalog.DirectoryName);
   using(var package=ModelPackage.Verify(directory,ModelCatalog.Current))
   {
    Assert.That(new FileInfo(package.ModelPath).Length,Is.EqualTo(965074339));Assert.That(package.ReadUtf8("LICENSE.txt"),Does.Contain("Apache License"));
    var hashSeconds=started.Elapsed.TotalSeconds;var evidence=Path.GetFullPath("Docs/Tests/Stage3/Evidence/S3-A01");Directory.CreateDirectory(evidence);
    var output=Path.Combine(evidence,"NativeUnderLease_"+DateTime.UtcNow.Ticks+".json");var g01=Path.GetFullPath("Docs/Tests/Stage3/Evidence/S3-G01");
    var owner=OrtCompatibilityProbe.Start(package.ModelPath,Path.Combine(g01,"PrefillFixture.json"),Path.Combine(g01,"WEIGHT8_ACC4_A01"),output,Thread.CurrentThread.ManagedThreadId,1);
    var deadline=System.Diagnostics.Stopwatch.StartNew();while(owner.IsAlive&&deadline.Elapsed.TotalSeconds<20)yield return null;
    Assert.That(owner.IsAlive,Is.False);var native=JObject.Parse(File.ReadAllText(output));Assert.That((bool)native["passed"],Is.True,native["error"]?.ToString());
    File.WriteAllText(Path.Combine(evidence,"ActualPackage_"+DateTime.UtcNow.Ticks+".json"),new JObject{{"releaseId",package.ReleaseId},{"bytes",ModelCatalog.Current.Files.Values.Sum(f=>f.Bytes)},{"hashSeconds",hashSeconds},{"totalSeconds",started.Elapsed.TotalSeconds},{"modelSha256",ModelCatalog.Current.Files["model.onnx"].Sha256},{"allAssetsVerified",true},{"nativeLoadWhileLeased",true}}.ToString());
   }
  }
 }
}
