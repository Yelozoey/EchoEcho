using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EchoStation.AI.Inference
{
 public enum TensorScalar{Int64,Float32,Unsupported}
 public sealed class TensorDescription
 {
  public TensorScalar Scalar {get;}public IReadOnlyList<long> Dimensions {get;}
  public TensorDescription(TensorScalar scalar,IEnumerable<long> shape){Scalar=scalar;Dimensions=Array.AsReadOnly((shape??throw new ArgumentNullException(nameof(shape))).ToArray());}
 }
 public static class QwenTensorContract
 {
  public const int Layers=24,KvHeads=2,HeadSize=64,Vocabulary=151936,MaximumInput=1536,MaximumContext=2048;
  public static string Past(int layer,bool value)=>"past_key_values."+layer+(value?".value":".key");
  public static string Present(int layer,bool value)=>"present."+layer+(value?".value":".key");
  public static void ValidateMetadata(IReadOnlyDictionary<string,TensorDescription> inputs,IReadOnlyDictionary<string,TensorDescription> outputs)
  {
   if(inputs==null||outputs==null||inputs.Count!=51||outputs.Count!=49)throw new InvalidDataException("ONNX tensor inventory differs");
   foreach(var name in new[]{"input_ids","attention_mask","position_ids"})Check(inputs,name,TensorScalar.Int64,new long[]{1,-1},true);
   Check(outputs,"logits",TensorScalar.Float32,new long[]{1,1,Vocabulary},true);
   for(var layer=0;layer<Layers;layer++)foreach(var value in new[]{false,true})
   {Check(inputs,Past(layer,value),TensorScalar.Float32,new long[]{1,KvHeads,-1,HeadSize},true);Check(outputs,Present(layer,value),TensorScalar.Float32,new long[]{1,KvHeads,-1,HeadSize},true);}
  }
  private static void Check(IReadOnlyDictionary<string,TensorDescription> set,string name,TensorScalar scalar,long[] dimensions,bool dynamicBatch)
  {
   if(!set.TryGetValue(name,out var info)||info.Scalar!=scalar||info.Dimensions.Count!=dimensions.Length)throw new InvalidDataException("ONNX tensor name/type/rank differs: "+name);
   for(var d=0;d<dimensions.Length;d++)if(info.Dimensions[d]!=dimensions[d]&&!(d==0&&dynamicBatch&&info.Dimensions[d]==-1))throw new InvalidDataException("ONNX metadata dimension differs: "+name);
  }
  public static void ValidateInput(IReadOnlyList<int> ids)
  {if(ids==null||ids.Count<1||ids.Count>MaximumInput)throw new ArgumentException("Prefill length must be1..1536");for(var i=0;i<ids.Count;i++)ValidateToken(ids[i]);}
  public static void ValidateToken(int id){if(id<0||id>=Vocabulary)throw new ArgumentOutOfRangeException(nameof(id),"Token outside model vocabulary");}
  public static void ValidateLogits(ReadOnlySpan<float> values,IReadOnlyList<long> dimensions)
  {Shape(dimensions,1,1,Vocabulary);if(values.Length!=Vocabulary)throw new InvalidDataException("Logits size differs");Finite(values);}
  public static void ValidateKv(ReadOnlySpan<float> values,IReadOnlyList<long> dimensions,int length)
  {if(length<0||length>MaximumContext)throw new InvalidDataException("KV context exceeds limit");Shape(dimensions,1,KvHeads,length,HeadSize);if(values.Length!=KvHeads*length*HeadSize)throw new InvalidDataException("KV size differs");Finite(values);}
  private static void Shape(IReadOnlyList<long> actual,params long[] expected){if(actual==null||actual.Count!=expected.Length)throw new InvalidDataException("Tensor rank differs");for(var i=0;i<expected.Length;i++)if(actual[i]!=expected[i])throw new InvalidDataException("Tensor runtime shape differs");}
  private static void Finite(ReadOnlySpan<float> values){for(var i=0;i<values.Length;i++)if(float.IsNaN(values[i])||float.IsInfinity(values[i]))throw new InvalidDataException("Nonfinite model tensor");}
 }
}
