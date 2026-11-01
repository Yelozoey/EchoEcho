using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.AI.Inference;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class TensorContractTests
 {
  private static Dictionary<string,TensorDescription> Read(string kind)
  {var data=JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-A03/ActualModelMetadata.json"));return data[kind].ToDictionary(v=>v["name"].Value<string>(),v=>new TensorDescription(v["type"].Value<string>()=="tensor(int64)"?TensorScalar.Int64:TensorScalar.Float32,v["dimensions"].ToObject<long[]>()));}
  [Test]public void ActualFinalNativeMetadataFitsContract()=>QwenTensorContract.ValidateMetadata(Read("inputs"),Read("outputs"));
  [TestCase("name")][TestCase("dtype")][TestCase("rank")][TestCase("head")][TestCase("logits")]
  public void WrongMetadataIsRejected(string fault)
  {
   var input=Read("inputs");var output=Read("outputs");
   switch(fault){case "name":input.Remove("position_ids");input.Add("Position_Ids",new TensorDescription(TensorScalar.Int64,new long[]{-1,-1}));break;case "dtype":input["input_ids"]=new TensorDescription(TensorScalar.Float32,new long[]{-1,-1});break;case "rank":input["input_ids"]=new TensorDescription(TensorScalar.Int64,new long[]{1});break;case "head":input[QwenTensorContract.Past(0,false)]=new TensorDescription(TensorScalar.Float32,new long[]{1,14,-1,64});break;case "logits":output["logits"]=new TensorDescription(TensorScalar.Float32,new long[]{1,-1,151936});break;}
   Assert.Throws<InvalidDataException>(()=>QwenTensorContract.ValidateMetadata(input,output));
  }
  [Test]public void InputBoundariesAndIdsAreChecked()
  {
   QwenTensorContract.ValidateInput(new[]{0});QwenTensorContract.ValidateInput(new int[1536]);Assert.Throws<ArgumentException>(()=>QwenTensorContract.ValidateInput(Array.Empty<int>()));Assert.Throws<ArgumentException>(()=>QwenTensorContract.ValidateInput(new int[1537]));Assert.Throws<ArgumentOutOfRangeException>(()=>QwenTensorContract.ValidateInput(new[]{151936}));Assert.Throws<ArgumentOutOfRangeException>(()=>QwenTensorContract.ValidateToken(-1));
  }
  [Test]public void RuntimeShapeAndNaNOrInfinityAreRejected()
  {
   var logits=new float[151936];QwenTensorContract.ValidateLogits(logits,new long[]{1,1,151936});logits[100]=float.NaN;Assert.Throws<InvalidDataException>(()=>QwenTensorContract.ValidateLogits(logits,new long[]{1,1,151936}));logits[100]=float.PositiveInfinity;Assert.Throws<InvalidDataException>(()=>QwenTensorContract.ValidateLogits(logits,new long[]{1,1,151936}));
   Assert.Throws<InvalidDataException>(()=>QwenTensorContract.ValidateLogits(new float[10],new long[]{1,1,10}));
   QwenTensorContract.ValidateKv(Array.Empty<float>(),new long[]{1,2,0,64},0);QwenTensorContract.ValidateKv(new float[256],new long[]{1,2,2,64},2);
   Assert.Throws<InvalidDataException>(()=>QwenTensorContract.ValidateKv(new float[256],new long[]{1,2,3,64},2));Assert.Throws<InvalidDataException>(()=>QwenTensorContract.ValidateKv(new float[128],new long[]{1,2,1,64},2049));
  }
 }
}
