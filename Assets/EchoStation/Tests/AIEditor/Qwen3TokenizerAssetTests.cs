using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3TokenizerAssetTests
 {
  private string json;private TokenizerAssets valid;
  [OneTimeSetUp]public void LoadPinnedJsonOnly()
  {
   var bytes=File.ReadAllBytes("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json");
   using(var sha=SHA256.Create())Assert.That(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(),Is.EqualTo(Qwen3Catalog.Current.Files["tokenizer.json"].Sha256));
   json=new UTF8Encoding(false,true).GetString(bytes);valid=TokenizerAssets.ParseQwen3(json);
  }
  [Test]public void FullOfficialInventoryHasContinuousNewProfileIds()
  {
   Assert.That(valid.Vocabulary.Count,Is.EqualTo(151643));Assert.That(valid.MergeRanks.Count,Is.EqualTo(151387));Assert.That(valid.ById.Count,Is.EqualTo(151669));Assert.That(valid.AddedTokens.Count,Is.EqualTo(26));
   Assert.That(valid.AddedTokens.Count(t=>t.IsSpecial),Is.EqualTo(14));Assert.That(valid.AddedTokens.Count(t=>!t.IsSpecial),Is.EqualTo(12));Assert.That(Enumerable.Range(0,151669).All(valid.ById.ContainsKey),Is.True);
  }
  [Test]public void FourNewMarkersRemainExactOrdinaryTokens()
  {
   var expected=new[]{"<tool_response>","</tool_response>","<think>","</think>"};
   for(var i=0;i<4;i++){Assert.That(valid.ById[151665+i],Is.EqualTo(expected[i]));Assert.That(valid.AddedTokens.Single(t=>t.Id==151665+i).IsSpecial,Is.False);}
  }
  [Test]public void ProfilesCannotSilentlyAcceptEachOthersMergeStorage()
  {
   Assert.Throws<InvalidDataException>(()=>TokenizerAssets.Parse(json));
   // Read only the historical byte vocabulary; never verify/load its model weights.
   var bytes=File.ReadAllBytes("Assets/StreamingAssets/EchoLLM/"+ModelCatalog.DirectoryName+"/tokenizer.json");
   using(var sha=SHA256.Create())Assert.That(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(),Is.EqualTo(ModelCatalog.Current.Files["tokenizer.json"].Sha256));
   var legacy=new UTF8Encoding(false,true).GetString(bytes);Assert.That(TokenizerAssets.Parse(legacy).ById.Count,Is.EqualTo(151665));Assert.Throws<InvalidDataException>(()=>TokenizerAssets.ParseQwen3(legacy));
  }
  [TestCase("string")][TestCase("short")][TestCase("nonstring")]public void MalformedPairArrayRejected(string kind)
  {
   var root=JObject.Parse(json);var merges=(JArray)root["model"]["merges"];
   if(kind=="string")merges[0]="Ġ Ġ";else if(kind=="short")merges[0]=new JArray("Ġ");else merges[0]=new JArray("Ġ",123);
   Assert.Throws<InvalidDataException>(()=>TokenizerAssets.ParseQwen3(root.ToString(Formatting.None)));
  }
  [Test]public void UnknownNewMarkerCannotReuseAuthorizedId()
  {
   var root=JObject.Parse(json);root["added_tokens"].Single(t=>t["id"].Value<int>()==151667)["content"]="<thinking>";
   Assert.Throws<InvalidDataException>(()=>TokenizerAssets.ParseQwen3(root.ToString(Formatting.None)));
  }
  [Test]public void UnsupportedNormalizerAndAssetBudgetRejected()
  {
   var root=JObject.Parse(json);root["normalizer"]["type"]="NFD";Assert.Throws<InvalidDataException>(()=>TokenizerAssets.ParseQwen3(root.ToString(Formatting.None)));
   Assert.Throws<InvalidDataException>(()=>TokenizerAssets.ParseQwen3(null));Assert.Throws<InvalidDataException>(()=>TokenizerAssets.ParseQwen3(new string(' ',16*1024*1024+1)));Assert.Throws<ArgumentNullException>(()=>TokenizerAssets.LoadQwen3(null));
  }
 }
}
