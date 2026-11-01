using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EchoStation.AI.Probe
{
 // G01 spike only: no Unity objects, game state or production request routing.
 public static class OrtCompatibilityProbe
 {
  public static Thread Start(string model,string fixture,string referenceFolder,string output,int mainThread,int repeats=5,int length=0)
  {
   if(File.Exists(output))throw new IOException("Fresh probe evidence required");
   var thread=new Thread(()=>Execute(model,fixture,referenceFolder,output,mainThread,repeats,length)){IsBackground=true,Name="Echo-G01-ORT-Owner"};
   thread.Start();return thread;
  }
  private static SessionOptions Options()
  {
   var value=new SessionOptions{IntraOpNumThreads=2,InterOpNumThreads=1,ExecutionMode=ExecutionMode.ORT_SEQUENTIAL};
   value.AddSessionConfigEntry("session.intra_op.allow_spinning","0");return value;
  }
  private static void Execute(string model,string fixture,string referenceFolder,string output,int mainThread,int repeats,int length)
  {
   var record=new JObject{{"mainThread",mainThread},{"ownerThread",Thread.CurrentThread.ManagedThreadId},{"repeats",repeats}};
   var rounds=new JArray();record["rounds"]=rounds;
   try
   {
    if(mainThread==Thread.CurrentThread.ManagedThreadId)throw new InvalidOperationException("Inference must be background");
    using(var options=Options())using(var tiny=new InferenceSession(Path.Combine(referenceFolder,"TinyAdd.onnx"),options))
    using(var values=tiny.Run(new[]{NamedOnnxValue.CreateFromTensor("x",new DenseTensor<float>(new[]{1f,2f},new[]{2}))}))
    {
     var actual=values.First().AsTensor<float>().ToArray();record["tinyAdd"]=new JArray(actual);
     if(actual.Length!=2||actual[0]!=3||actual[1]!=5)throw new InvalidDataException("Tiny arithmetic mismatch");
    }
    var source=JObject.Parse(File.ReadAllText(fixture))["inputIds"].ToObject<long[]>();
    var prompt=length>0?Enumerable.Range(0,length).Select(i=>source[i%source.Length]).ToArray():source;
    record["inputTokens"]=prompt.Length;
    for(var round=0;round<repeats;round++)
    {
     var timer=Stopwatch.StartNew();
     using(var options=Options())
     using(var session=new InferenceSession(model,options))
     {
      var item=new JObject{{"loadSeconds",timer.Elapsed.TotalSeconds},{"ownerThread",Thread.CurrentThread.ManagedThreadId}};rounds.Add(item);
      var steps=new JArray();item["steps"]=steps;
      var past=new Dictionary<string,DenseTensor<float>>();
      for(var layer=0;layer<24;layer++)foreach(var kind in new[]{"key","value"})past[$"past_key_values.{layer}.{kind}"]=new DenseTensor<float>(new[]{1,2,0,64});
      var ids=prompt;
      for(var step=0;step<3;step++)
      {
       var count=prompt.Length+step;
       var inputs=new List<NamedOnnxValue>{NamedOnnxValue.CreateFromTensor("input_ids",new DenseTensor<long>(ids,new[]{1,ids.Length})),NamedOnnxValue.CreateFromTensor("attention_mask",new DenseTensor<long>(Enumerable.Repeat(1L,count).ToArray(),new[]{1,count})),NamedOnnxValue.CreateFromTensor("position_ids",new DenseTensor<long>(Enumerable.Range(count-ids.Length,ids.Length).Select(v=>(long)v).ToArray(),new[]{1,ids.Length}))};
       foreach(var pair in past)inputs.Add(NamedOnnxValue.CreateFromTensor(pair.Key,pair.Value));
       timer.Restart();
       using(var results=session.Run(inputs))
       {
        var seconds=timer.Elapsed.TotalSeconds;
        var tensor=results.First(r=>r.Name=="logits").AsTensor<float>();var dense=tensor as DenseTensor<float>;if(dense==null)throw new InvalidDataException("Dense logits required");var values=dense.Buffer.Span;var start=values.Length-151936;var best=0;
        for(var index=0;index<151936;index++){var f=values[start+index];if(float.IsNaN(f)||float.IsInfinity(f))throw new InvalidDataException("Nonfinite logits");if(f>values[start+best])best=index;}
        var data=new JObject{{"token",best},{"seconds",seconds},{"logitsShape",new JArray(tensor.Dimensions.ToArray())},{"ownerThread",Thread.CurrentThread.ManagedThreadId}};steps.Add(data);
        if(length==0)
        {
         var reference=ReadFloats(Path.Combine(referenceFolder,$"PythonStep{step}Logits.bin"));if(reference.Length!=151936)throw new InvalidDataException("Reference length");
         double max=0;for(var index=0;index<151936;index++)max=Math.Max(max,Math.Abs(values[start+index]-reference[index]));data["referenceMaxAbsoluteError"]=max;
         var raw=values.Slice(start,151936).ToArray();WriteFloats(output+$".r{round}.s{step}.bin",raw);
        }
        past.Clear();
        foreach(var result in results.Where(r=>r.Name.StartsWith("present.",StringComparison.Ordinal)))
        {
         var t=result.AsTensor<float>();past.Add(result.Name.Replace("present.","past_key_values."),new DenseTensor<float>(t.ToArray(),t.Dimensions.ToArray()));
        }
        if(past.Count!=48)throw new InvalidDataException("KV output count");
        data["kvShape"]=new JArray(past.First().Value.Dimensions.ToArray());ids=new[]{(long)best};
       }
      }
     }
    }
    if(length==0)
    {
     var expected=JObject.Parse(File.ReadAllText(Path.Combine(referenceFolder,"PythonOrtReference.json")))["generated"].ToObject<int[]>();
     foreach(JObject round in rounds)for(var step=0;step<3;step++)
     {
      var data=round["steps"][step];if((int)data["token"]!=expected[step]||(double)data["referenceMaxAbsoluteError"]>.002)throw new InvalidDataException("Native/reference mismatch");
     }
    }
    record["passed"]=true;
   }
   catch(Exception error){record["passed"]=false;record["error"]=error.ToString();}
   finally{record["releasedOnThread"]=Thread.CurrentThread.ManagedThreadId;Directory.CreateDirectory(Path.GetDirectoryName(output));File.WriteAllText(output,record.ToString(Formatting.Indented));}
  }
  private static float[] ReadFloats(string path){var bytes=File.ReadAllBytes(path);var values=new float[bytes.Length/4];Buffer.BlockCopy(bytes,0,values,0,bytes.Length);return values;}
  private static void WriteFloats(string path,float[] values){var bytes=new byte[values.Length*4];Buffer.BlockCopy(values,0,bytes,0,bytes.Length);File.WriteAllBytes(path,bytes);}
 }
}
