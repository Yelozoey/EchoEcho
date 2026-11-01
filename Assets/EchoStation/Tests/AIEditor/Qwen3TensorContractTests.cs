using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.AI.Inference;
using EchoStation.AI.Models;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3TensorContractTests
 {
  private static Dictionary<string,TensorDescription> Read(string kind)
  {
   var root=JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-Q3-A03r/ActualModelMetadata.json"));
   return root[kind].ToDictionary(node=>node["name"].Value<string>(),node=>new TensorDescription(node["type"].Value<string>()=="tensor(int64)"?TensorScalar.Int64:node["type"].Value<string>()=="tensor(float)"?TensorScalar.Float32:TensorScalar.Unsupported,node["dimensions"].Values<long>()));
  }
  [Test]public void ActualInstalledModelMetadataMatchesNewContract()=>Qwen3TensorContract.ValidateMetadata(Read("inputs"),Read("outputs"));
  [Test]public void CompiledGeometryMatchesPinnedOfficialConfig()
  {
   var config=JObject.Parse(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/config.json"));
   Assert.That(config["num_hidden_layers"].Value<int>(),Is.EqualTo(Qwen3TensorContract.Layers));Assert.That(config["num_attention_heads"].Value<int>(),Is.EqualTo(Qwen3TensorContract.QueryHeads));
   Assert.That(config["num_key_value_heads"].Value<int>(),Is.EqualTo(Qwen3TensorContract.KvHeads));Assert.That(config["head_dim"].Value<int>(),Is.EqualTo(Qwen3TensorContract.HeadSize));Assert.That(config["vocab_size"].Value<int>(),Is.EqualTo(Qwen3TensorContract.Vocabulary));
  }
  [TestCase("name")][TestCase("dtype")][TestCase("rank")][TestCase("old_geometry")][TestCase("logits")][TestCase("batch")]
  public void IncompatibleMetadataRejected(string fault)
  {
   var input=Read("inputs");var output=Read("outputs");
   switch(fault)
   {
    case "name":input.Remove("position_ids");input.Add("Position_Ids",new TensorDescription(TensorScalar.Int64,new long[]{1,-1}));break;
    case "dtype":input["input_ids"]=new TensorDescription(TensorScalar.Float32,new long[]{1,-1});break;
    case "rank":input["input_ids"]=new TensorDescription(TensorScalar.Int64,new long[]{1});break;
    case "old_geometry":input[Qwen3TensorContract.Past(0,false)]=new TensorDescription(TensorScalar.Float32,new long[]{1,2,-1,64});break;
    case "logits":output["logits"]=new TensorDescription(TensorScalar.Float32,new long[]{1,-1,151936});break;
    case "batch":input["input_ids"]=new TensorDescription(TensorScalar.Int64,new long[]{2,-1});break;
   }
   Assert.Throws<InvalidDataException>(()=>Qwen3TensorContract.ValidateMetadata(input,output));
  }
  [Test]public void InputLengthsAndIdsStayBounded()
  {
   Qwen3TensorContract.ValidateInput(new[]{0});Qwen3TensorContract.ValidateInput(new int[1536]);
   Assert.Throws<ArgumentException>(()=>Qwen3TensorContract.ValidateInput(null));Assert.Throws<ArgumentException>(()=>Qwen3TensorContract.ValidateInput(Array.Empty<int>()));Assert.Throws<ArgumentException>(()=>Qwen3TensorContract.ValidateInput(new int[1537]));
   Assert.Throws<ArgumentOutOfRangeException>(()=>Qwen3TensorContract.ValidateInput(new[]{151936}));Assert.Throws<ArgumentOutOfRangeException>(()=>Qwen3TensorContract.ValidateToken(-1));
  }
  [Test]public void EveryRuntimeFloatIncludingPaddingLogitsIsChecked()
  {
   var logits=new float[151936];var kv=new float[8*2*128];
   Qwen3TensorContract.ValidateLogits(logits,new long[]{1,1,151936});Qwen3TensorContract.ValidateKv(kv,new long[]{1,8,2,128},2);
   foreach(var bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
   {
    logits[logits.Length-1]=bad;Assert.Throws<InvalidDataException>(()=>Qwen3TensorContract.ValidateLogits(logits,new long[]{1,1,151936}));logits[logits.Length-1]=0;
    kv[kv.Length-1]=bad;Assert.Throws<InvalidDataException>(()=>Qwen3TensorContract.ValidateKv(kv,new long[]{1,8,2,128},2));kv[kv.Length-1]=0;
   }
  }
  [Test]public void EmptyPastMaximumContextAndWrongRuntimeShapesChecked()
  {
   Qwen3TensorContract.ValidateKv(Array.Empty<float>(),new long[]{1,8,0,128},0);Qwen3TensorContract.ValidateKv(new float[8*2048*128],new long[]{1,8,2048,128},2048);
   Assert.Throws<InvalidDataException>(()=>Qwen3TensorContract.ValidateKv(Array.Empty<float>(),new long[]{1,8,0,128},2049));
   Assert.Throws<InvalidDataException>(()=>Qwen3TensorContract.ValidateKv(new float[8*2*128],new long[]{1,8,3,128},2));Assert.Throws<InvalidDataException>(()=>Qwen3TensorContract.ValidateLogits(new float[10],new long[]{1,1,10}));
  }
 }
}
