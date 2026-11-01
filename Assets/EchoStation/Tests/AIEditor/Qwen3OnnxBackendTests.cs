using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using EchoStation.AI.Inference;
using EchoStation.AI.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3OnnxBackendTests
 {
  private const string ReferenceSha="246e42b93d0b80c5eee890cf024093f140259ba250fa7b7c1dde87d782f04b4d";
  private sealed class Work{public readonly CancellationTokenSource Cancel=new CancellationTokenSource();public int Done,Ready,Owner;public bool Clean;public string Error;}
  private Work active;
  [TearDown]public void CancelUnfinishedOwner(){var work=active;if(work!=null&&Volatile.Read(ref work.Done)==0)work.Cancel.Cancel();}
  [Test]public void UnityMainCannotCreateNativeResources()=>Assert.Throws<InvalidOperationException>(()=>new Qwen3OnnxBackend(null,Thread.CurrentThread.ManagedThreadId));
  private static void Need(bool value,string message){if(!value)throw new InvalidDataException(message);}
  private static void Reject<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new InvalidDataException("Expected "+typeof(T).Name);}
  private static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
  private static float[] Floats(JToken spec)
  {
   var path=Path.GetFullPath(spec["path"].Value<string>());var allowed=Path.GetFullPath("Library/EchoStation/S3Cache/qwen3-native-reference-A")+Path.DirectorySeparatorChar;
   Need(path.StartsWith(allowed,StringComparison.OrdinalIgnoreCase),"Reference path escaped cache");var length=new FileInfo(path).Length;
   Need(length==spec["bytes"].Value<long>()&&length<=32*1024*1024&&length%4==0&&BitConverter.IsLittleEndian,"Reference binary size/endianness differs");
   var bytes=File.ReadAllBytes(path);Need(Hash(bytes)==spec["sha256"].Value<string>(),"Reference binary SHA differs");var result=new float[bytes.Length/4];Buffer.BlockCopy(bytes,0,result,0,bytes.Length);FiniteTensorValidator.RequireFinite(result);return result;
  }
  private static double Compare(ReadOnlySpan<float> actual,float[] expected,int offset=0)
  {Need(offset>=0&&offset+actual.Length<=expected.Length,"Numeric reference count differs");double maximum=0;for(var i=0;i<actual.Length;i++)maximum=Math.Max(maximum,Math.Abs((double)actual[i]-expected[offset+i]));Need(maximum<=.0001,"Numeric maxabs exceeds1e-4: "+maximum);return maximum;}
  private static int Greedy(ReadOnlySpan<float> values){var best=0;for(var i=1;i<values.Length;i++)if(values[i]>values[best])best=i;return best;}
  [UnityTest,Timeout(300000)]public IEnumerator ActualMultiLengthLogitsKvOwnershipAndFiveReleasesMatchReference()
  {
   var main=Thread.CurrentThread.ManagedThreadId;var work=active=new Work();var output=Path.GetFullPath("Docs/Tests/Stage3/Evidence/S3-Q3-A03b/Backend_"+DateTime.UtcNow.Ticks+".json");
   new Thread(()=>Run(work,main,output)){IsBackground=true,Name="Q3-Formal-Backend-Owner",Priority=ThreadPriority.BelowNormal}.Start();var clock=Stopwatch.StartNew();
   try
   {
    while(Volatile.Read(ref work.Done)==0&&clock.Elapsed.TotalSeconds<280){if(Volatile.Read(ref work.Ready)==0&&clock.Elapsed.TotalSeconds>=30||clock.Elapsed.TotalSeconds>=260)work.Cancel.Cancel();yield return null;}
    Assert.That(Volatile.Read(ref work.Done),Is.EqualTo(1),"Owner still draining: "+output);Assert.That(work.Error,Is.Null,work.Error+"; "+output);Assert.That(work.Clean,Is.True,output);Assert.That(work.Owner,Is.Not.EqualTo(main));
   }
   finally{work.Cancel.Cancel();if(Volatile.Read(ref work.Done)==1)work.Cancel.Dispose();}
  }
  private static void Run(Work work,int main,string output)
  {
   var clock=Stopwatch.StartNew();work.Owner=Thread.CurrentThread.ManagedThreadId;var rounds=new JArray();VerifiedModelPackage package=null;
   var report=new JObject{{"scope","formal Qwen3 backend numeric/ownership only, not game or performance acceptance"},{"mainThread",main},{"ownerThread",work.Owner},{"cpuThreads",4},{"referenceSha256",ReferenceSha},{"rounds",rounds},{"completed",false},{"passed",false}};
   try
   {
    Directory.CreateDirectory(Path.GetDirectoryName(output));using(var unique=new FileStream(output,FileMode.CreateNew,FileAccess.Write,FileShare.Read)){};
    var bytes=File.ReadAllBytes("Docs/Tests/Stage3/Evidence/S3-Q3-A03r/Reference.json");Need(Hash(bytes)==ReferenceSha,"Independent reference changed");var reference=JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
    Need(reference["completed"].Value<bool>()&&reference["passed"].Value<bool>()&&reference["nativeCalls"].Value<int>()==15,"Independent reference incomplete");
    Need(reference["modelSha256"].Value<string>()==Qwen3Catalog.Current.Files["model.onnx"].Sha256,"Reference model differs");
    package=ModelPackage.Verify(Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName),Qwen3Catalog.Current,work.Cancel.Token);
    for(var cycle=0;cycle<5;cycle++)
    {
     work.Cancel.Token.ThrowIfCancellationRequested();Qwen3OnnxBackend backend=null;var row=new JObject{{"cycle",cycle}};rounds.Add(row);
     try
     {
      backend=new Qwen3OnnxBackend(package,main);if(cycle==0){Need(clock.Elapsed.TotalSeconds<30,"Cold preparation exceeded30s");work.Cancel.Token.ThrowIfCancellationRequested();Volatile.Write(ref work.Ready,1);report["coldSeconds"]=clock.Elapsed.TotalSeconds;}
      var selected=cycle==0?reference["cases"].ToArray():reference["cases"].Where(item=>item["id"].Value<string>()=="len64").ToArray();var cases=new JArray();row["cases"]=cases;
      foreach(var item in selected)
      {
       work.Cancel.Token.ThrowIfCancellationRequested();var ids=item["inputIds"].Values<int>().ToArray();var timer=Stopwatch.StartNew();backend.Prefill(ids);Need(timer.Elapsed.TotalSeconds<20,"Prefill exceeded20s");
       var steps=new JArray();cases.Add(new JObject{{"id",item["id"].Value<string>()},{"steps",steps}});
       foreach(var expected in item["steps"])
       {
        work.Cancel.Token.ThrowIfCancellationRequested();Need(backend.ConsumedTokens==expected["consumed"].Value<int>(),"KV position differs");var logits=Floats(expected["logits"]);Need(logits.Length==151936,"Reference vocabulary differs");
        var error=Compare(backend.Logits,logits);var token=Greedy(backend.Logits);Need(token==expected["token"].Value<int>(),"Greedy prefix differs");var step=new JObject{{"step",expected["step"].Value<int>()},{"token",token},{"logitsMaxAbs",error}};steps.Add(step);
        if(expected["kv"]!=null)
        {
         var kv=Floats(expected["kv"]);var count=8*backend.ConsumedTokens*128;Need(kv.Length==56*count,"Reference KV inventory differs");double maximum=0;
         for(var layer=0;layer<28;layer++)for(var value=0;value<2;value++)maximum=Math.Max(maximum,Compare(backend.KeyValue(layer,value==1),kv,(layer*2+value)*count));step["all56KvMaxAbs"]=maximum;
        }
        else step["kvScope"]="long KV shape/full-finite validated by backend; no stored numeric KV reference";
        if(expected["step"].Value<int>()<2){timer.Restart();backend.Step(token);Need(timer.Elapsed.TotalSeconds<20,"Decode exceeded20s");}
       }
       backend.ResetCache();Need(backend.ConsumedTokens==0,"Cross-request KV not cleared");
      }
      var before=backend.TotalNativeCalls;Reject<ArgumentException>(()=>backend.Prefill(Array.Empty<int>()));Reject<ArgumentOutOfRangeException>(()=>backend.Step(-1));Reject<InvalidOperationException>(()=>backend.Step(1));Reject<ArgumentOutOfRangeException>(()=>new Qwen3OnnxBackend(package,main,8));Need(backend.TotalNativeCalls==before,"Bad request reached native");
      string foreign=null;using(var done=new ManualResetEvent(false))
      {new Thread(()=>{try{backend.Dispose();foreign="accepted";}catch(Exception error){foreign=error.GetType().FullName;}finally{done.Set();}}){IsBackground=true}.Start();Need(done.WaitOne(5000),"Foreign guard did not finish");}
      Need(foreign==typeof(InvalidOperationException).FullName&&!backend.IsDisposed,"Foreign Owner was accepted");
     }
     finally
     {
      backend?.Dispose();if(backend!=null)
      {
       var clean=backend.IsDisposed&&backend.DisposeThread==work.Owner&&backend.EmptyPastCreated==56&&backend.EmptyPastDisposed==56&&backend.CollectionsCreated==backend.CollectionsDisposed&&backend.MainThreadNativeCalls==0;
       row["nativeCalls"]=backend.TotalNativeCalls;row["emptyCreated"]=backend.EmptyPastCreated;row["emptyDisposed"]=backend.EmptyPastDisposed;row["collectionsCreated"]=backend.CollectionsCreated;row["collectionsDisposed"]=backend.CollectionsDisposed;row["disposeThread"]=backend.DisposeThread;row["mainThreadNativeCalls"]=backend.MainThreadNativeCalls;row["ownerCleanupComplete"]=clean;
       Need(clean,"Formal backend native cleanup differs");Reject<ObjectDisposedException>(()=>backend.ResetCache());backend.Dispose();
      }
     }
    }
    work.Clean=rounds.Count==5&&rounds.All(round=>round["ownerCleanupComplete"].Value<bool>());
   }
   catch(Exception error){work.Error=error.ToString();report["error"]=work.Error;}
   finally
   {
    try{package?.Dispose();}catch(Exception error){work.Error=(work.Error??"")+"\nPackage cleanup: "+error;work.Clean=false;}
    report["ownerCleanupComplete"]=work.Clean;report["seconds"]=clock.Elapsed.TotalSeconds;report["completed"]=true;report["passed"]=work.Error==null&&work.Clean;
    try{File.WriteAllText(output,report.ToString(),new System.Text.UTF8Encoding(false));}catch(Exception error){work.Error=(work.Error??"")+"\nEvidence: "+error;}
    Volatile.Write(ref work.Done,1);
   }
  }
 }
}
