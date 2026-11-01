using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using EchoStation.AI.Inference;
using EchoStation.AI.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class OnnxBackendTests
 {
  [Test]public void UnityMainCannotCreateInferenceResources()=>Assert.Throws<InvalidOperationException>(()=>new QwenOnnxBackend(null,Thread.CurrentThread.ManagedThreadId));
  private static void Check(bool value,string why){if(!value)throw new InvalidDataException(why);}
  private static void Throws<T>(Action action)where T:Exception{try{action();}catch(T){return;}throw new InvalidDataException("Expected "+typeof(T).Name);}
  private static float[] Floats(string path){var bytes=File.ReadAllBytes(path);var values=new float[bytes.Length/4];Buffer.BlockCopy(bytes,0,values,0,bytes.Length);return values;}
  private static double Compare(QwenOnnxBackend backend,string reference)
  {var expected=Floats(reference);var actual=backend.Logits;Check(actual.Length==expected.Length,"Logit length");double error=0;for(var i=0;i<expected.Length;i++)error=Math.Max(error,Math.Abs(actual[i]-expected[i]));Check(error<=.002,"Logits/reference max error "+error);return error;}
  private static int Greedy(QwenOnnxBackend backend){var logits=backend.Logits;var best=0;for(var i=1;i<logits.Length;i++)if(logits[i]>logits[best])best=i;return best;}
  [UnityTest]public IEnumerator RealCacheMultiLengthReferenceAndFiveReleases()
  {
   var main=Thread.CurrentThread.ManagedThreadId;var folder=Path.GetFullPath("Docs/Tests/Stage3/Evidence/S3-A03");var g01=Path.GetFullPath("Docs/Tests/Stage3/Evidence/S3-G01");var path=Path.Combine(folder,"NativeBackend_"+DateTime.UtcNow.Ticks+".json");var packagePath=Path.GetFullPath("Assets/StreamingAssets/EchoLLM/"+ModelCatalog.DirectoryName);
   var ids=JObject.Parse(File.ReadAllText(Path.Combine(g01,"PrefillFixture.json")))["inputIds"].ToObject<int[]>();
   var worker=new Thread(()=>
   {
    var report=new JObject{{"mainThread",main},{"ownerThread",Thread.CurrentThread.ManagedThreadId}};var rounds=new JArray();report["rounds"]=rounds;
    try
    {
     using(var package=ModelPackage.Verify(packagePath,ModelCatalog.Current))
     {
      for(var cycle=0;cycle<5;cycle++)
      {
       var watch=System.Diagnostics.Stopwatch.StartNew();JObject round;
       using(var backend=new QwenOnnxBackend(package,main))
       {
        round=new JObject{{"loadSeconds",watch.Elapsed.TotalSeconds},{"owner",backend.OwnerThreadId}};rounds.Add(round);backend.Prefill(ids);var errors=new JArray();var tokens=new JArray();
        for(var step=0;step<3;step++){errors.Add(Compare(backend,Path.Combine(g01,"WEIGHT8_ACC4_A01",$"PythonStep{step}Logits.bin")));var token=Greedy(backend);tokens.Add(token);Check(backend.ConsumedTokens==ids.Length+step,"KV length after decode");if(step<2)backend.Step(token);}
        round["errors"]=errors;round["tokens"]=tokens;Check(tokens.ToObject<int[]>().SequenceEqual(new[]{73594,2236,198}),"Greedy prefix");
        if(cycle==0)
        {
         var calls=backend.NativeCalls;Throws<ArgumentException>(()=>backend.Prefill(Array.Empty<int>()));Throws<ArgumentOutOfRangeException>(()=>backend.Step(-1));Check(backend.NativeCalls==calls,"Bad input reached native Run");
         Exception foreign=null;var other=new Thread(()=>{try{backend.ResetCache();}catch(Exception e){foreign=e;}}){IsBackground=true};other.Start();Check(other.Join(5000),"Foreign guard thread did not return");Check(foreign is InvalidOperationException,"Wrong Owner was accepted");
         backend.ResetCache();Check(backend.ConsumedTokens==0,"Reset KV length");Throws<InvalidOperationException>(()=>backend.Step(1));
         foreach(var length in new[]{1,16}){backend.Prefill(ids.Take(length).ToArray());Check(backend.ConsumedTokens==length&&backend.Logits.Length==151936,"Short prefill shape");}
         var longIds=Enumerable.Range(0,1536).Select(i=>ids[i%ids.Length]).ToArray();watch.Restart();backend.Prefill(longIds);var longErrors=new JArray();var longTokens=new JArray();
         for(var step=0;step<3;step++){longErrors.Add(Compare(backend,Path.Combine(g01,"WEIGHT8_LAST_LONG_A01",$"PythonStep{step}Logits.bin")));var token=Greedy(backend);longTokens.Add(token);Check(backend.ConsumedTokens==1536+step,"Long KV length");if(step<2)backend.Step(token);}
         Check(longTokens.ToObject<int[]>().SequenceEqual(new[]{105043,74994,3837}),"Long reference prefix");round["longErrors"]=longErrors;round["longTokens"]=longTokens;round["longThreeStepSeconds"]=watch.Elapsed.TotalSeconds;
         backend.ResetCache();Check(backend.ConsumedTokens==0,"Long reset");
        }
        round["nativeCalls"]=backend.NativeCalls;
       }
       round["disposedOnOwner"]=Thread.CurrentThread.ManagedThreadId;
      }
     }
     report["passed"]=true;
    }
    catch(Exception error){report["passed"]=false;report["error"]=error.ToString();}
    finally{report["releasedOnOwner"]=Thread.CurrentThread.ManagedThreadId;File.WriteAllText(path,report.ToString());}
   }){IsBackground=true,Name="Echo-A03-Contract-Owner"};
   worker.Start();var timer=System.Diagnostics.Stopwatch.StartNew();while(worker.IsAlive&&timer.Elapsed.TotalSeconds<100)yield return null;
   Assert.That(worker.IsAlive,Is.False,"Native contract probe did not release");var result=JObject.Parse(File.ReadAllText(path));Assert.That((bool)result["passed"],Is.True,result["error"]?.ToString());Assert.That(result["rounds"].Count(),Is.EqualTo(5));
  }
 }
}
