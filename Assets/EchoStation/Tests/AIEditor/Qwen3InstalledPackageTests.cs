using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using EchoStation.AI.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3InstalledPackageTests
 {
  private sealed class Work
  {public readonly CancellationTokenSource Cancel=new CancellationTokenSource();public int Done,Owner;public string Error;public bool Clean;}
  private Work active;
  [TearDown]public void CancelUnfinishedOwner()
  {var work=active;if(work!=null&&Volatile.Read(ref work.Done)==0)work.Cancel.Cancel();}
  [UnityTest,Timeout(90000)]public IEnumerator ActualInstalledFilesVerifyOnBackgroundAndReleaseAllLeases()
  {
   var main=Thread.CurrentThread.ManagedThreadId;var work=active=new Work();
   var path=Path.GetFullPath("Docs/Tests/Stage3/Evidence/S3-Q3-A01c/Package_"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ")+"_"+Guid.NewGuid().ToString("N")+".json");
   new Thread(()=>Verify(work,main,path)){IsBackground=true,Name="Q3-Package-Verify",Priority=ThreadPriority.BelowNormal}.Start();
   var clock=Stopwatch.StartNew();
   try
   {
    while(Volatile.Read(ref work.Done)==0&&clock.Elapsed.TotalSeconds<60)
    {if(clock.Elapsed.TotalSeconds>=30)work.Cancel.Cancel();yield return null;}
    Assert.That(Volatile.Read(ref work.Done),Is.EqualTo(1),"Verification still draining: "+path);
    Assert.That(work.Error,Is.Null,"Package verification: "+work.Error+"; "+path);Assert.That(work.Clean,Is.True,"Lease cleanup: "+path);Assert.That(work.Owner,Is.Not.EqualTo(main));
   }
   finally{work.Cancel.Cancel();if(Volatile.Read(ref work.Done)==1)work.Cancel.Dispose();}
  }
  private static void Verify(Work work,int main,string path)
  {
   var clock=Stopwatch.StartNew();var root=Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName);
   work.Owner=Thread.CurrentThread.ManagedThreadId;VerifiedModelPackage package=null;var claimed=false;
   var record=new JObject{{"scope","installed byte package only; not tokenizer, native inference, game or hardware acceptance"},{"mainThread",main},{"ownerThread",work.Owner},{"releaseId",Qwen3Catalog.Current.Id},{"packagePath",root},{"nativeSessionCreated",false},{"completed",false},{"passed",false}};
   try
   {
    Directory.CreateDirectory(Path.GetDirectoryName(path));using(var unique=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){}claimed=true;
    if(work.Owner==main)throw new InvalidOperationException("Package hash verification must be background");
    package=ModelPackage.Verify(root,Qwen3Catalog.Current,work.Cancel.Token);record["allNineFilesShaVerified"]=true;record["verifySeconds"]=clock.Elapsed.TotalSeconds;
    var tokens=package.ReadUtf8("tokenizer.json",16*1024*1024);record["tokenizerUtf16Characters"]=tokens.Length;record["tokenizerUtf8Bytes"]=Encoding.UTF8.GetByteCount(tokens);
    if(Encoding.UTF8.GetByteCount(tokens)!=Qwen3Catalog.Current.Files["tokenizer.json"].Bytes)throw new InvalidDataException("Verified UTF8 text roundtrip differs");
    record["licenseContainsApache"]=package.ReadUtf8("LICENSE.txt").Contains("Apache License");
    var locked=false;
    try{using(var write=new FileStream(package.ModelPath,FileMode.Open,FileAccess.Write,FileShare.ReadWrite)){};}
    catch(IOException){locked=true;}
    if(!locked)throw new InvalidOperationException("Model did not retain its read lease");record["modelWriteBlockedDuringLease"]=locked;
    record["packageBytes"]=Qwen3Catalog.Current.Files.Values.Sum(file=>file.Bytes);record["modelSha256"]=Qwen3Catalog.Current.Files["model.onnx"].Sha256;
    work.Cancel.Token.ThrowIfCancellationRequested();if(clock.Elapsed.TotalSeconds>=30)throw new TimeoutException("Package cold preparation exceeded30s");
   }
   catch(Exception error){work.Error=error.ToString();record["error"]=work.Error;}
   finally
   {
    try
    {
     package?.Dispose();work.Clean=package==null||package.IsDisposed;
     foreach(var file in Qwen3Catalog.Current.Files.Keys)
      using(var probe=new FileStream(Path.Combine(root,file),FileMode.Open,FileAccess.Write,FileShare.ReadWrite))
       if(!probe.CanWrite)throw new IOException("Package write access still blocked after disposal");
     record["allFilesUnlockedAfterOwnerDispose"]=true;record["disposeThread"]=Thread.CurrentThread.ManagedThreadId;
    }
    catch(Exception error){work.Clean=false;work.Error=(work.Error??"")+"\nCleanup: "+error;record["cleanupError"]=error.ToString();}
    record["ownerCleanupComplete"]=work.Clean;record["seconds"]=clock.Elapsed.TotalSeconds;record["completed"]=true;record["passed"]=work.Error==null&&work.Clean;
    try{if(claimed)File.WriteAllText(path,record.ToString(),new UTF8Encoding(false));}catch(Exception error){work.Error=(work.Error??"")+"\nEvidence: "+error;}
    Volatile.Write(ref work.Done,1);
   }
  }
 }
}
