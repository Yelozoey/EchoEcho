using System;
using System.Collections.Generic;
using System.IO;

namespace EchoStation.AI.Inference
{
 public static class Qwen3TensorContract
 {
  public const int Layers=28,QueryHeads=16,KvHeads=8,HeadSize=128,Vocabulary=151936,MaximumInput=1536,MaximumContext=2048;
  public static string Past(int layer,bool value)=>"past_key_values."+layer+(value?".value":".key");
  public static string Present(int layer,bool value)=>"present."+layer+(value?".value":".key");
  public static void ValidateMetadata(IReadOnlyDictionary<string,TensorDescription> inputs,IReadOnlyDictionary<string,TensorDescription> outputs)
  {
   if(inputs==null||outputs==null||inputs.Count!=59||outputs.Count!=57)throw new InvalidDataException("Qwen3 tensor inventory differs");
   foreach(var name in new[]{"input_ids","attention_mask","position_ids"})Check(inputs,name,TensorScalar.Int64,1,-1);
   Check(outputs,"logits",TensorScalar.Float32,1,1,Vocabulary);
   for(var layer=0;layer<Layers;layer++)foreach(var value in new[]{false,true})
   {Check(inputs,Past(layer,value),TensorScalar.Float32,1,KvHeads,-1,HeadSize);Check(outputs,Present(layer,value),TensorScalar.Float32,1,KvHeads,-1,HeadSize);}
  }
  private static void Check(IReadOnlyDictionary<string,TensorDescription> set,string name,TensorScalar scalar,params long[] dimensions)
  {
   if(!set.TryGetValue(name,out var info)||info.Scalar!=scalar||info.Dimensions.Count!=dimensions.Length)throw new InvalidDataException("Qwen3 tensor name/type/rank differs: "+name);
   for(var i=0;i<dimensions.Length;i++)if(info.Dimensions[i]!=dimensions[i]&&!(i==0&&info.Dimensions[i]==-1))throw new InvalidDataException("Qwen3 metadata dimension differs: "+name);
  }
  public static void ValidateInput(IReadOnlyList<int> ids)
  {if(ids==null||ids.Count<1||ids.Count>MaximumInput)throw new ArgumentException("Qwen3 prefill must be1..1536");for(var i=0;i<ids.Count;i++)ValidateToken(ids[i]);}
  public static void ValidateToken(int id){if(id<0||id>=Vocabulary)throw new ArgumentOutOfRangeException(nameof(id));}
  public static void ValidateLogits(ReadOnlySpan<float> values,IReadOnlyList<long> dimensions)
  {Shape(dimensions,1,1,Vocabulary);if(values.Length!=Vocabulary)throw new InvalidDataException("Qwen3 logits size differs");FiniteTensorValidator.RequireFinite(values);}
  public static void ValidateKv(ReadOnlySpan<float> values,IReadOnlyList<long> dimensions,int length)
  {if(length<0||length>MaximumContext)throw new InvalidDataException("Qwen3 KV context exceeds limit");Shape(dimensions,1,KvHeads,length,HeadSize);if(values.Length!=KvHeads*length*HeadSize)throw new InvalidDataException("Qwen3 KV size differs");FiniteTensorValidator.RequireFinite(values);}
  private static void Shape(IReadOnlyList<long> actual,params long[] expected)
  {if(actual==null||actual.Count!=expected.Length)throw new InvalidDataException("Qwen3 runtime tensor rank differs");for(var i=0;i<expected.Length;i++)if(actual[i]!=expected[i])throw new InvalidDataException("Qwen3 runtime tensor shape differs");}
 }
}
