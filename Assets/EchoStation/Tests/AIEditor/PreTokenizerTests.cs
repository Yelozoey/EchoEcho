using System;
using System.IO;
using System.Linq;
using EchoStation.AI.Tokenization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class PreTokenizerTests
 {
  private static string Asset=>"Assets/StreamingAssets/EchoLLM/Qwen25_05B_W8A8/tokenizer.json";
  [Test]public void All256BytesRoundTripAndMatchGptByteAlphabet()
  {
   var bytes=Enumerable.Range(0,256).Select(i=>(byte)i).ToArray();var encoded=ByteAlphabet.Encode(bytes);
   CollectionAssert.AreEqual(bytes,ByteAlphabet.DecodeBytes(encoded));Assert.That(encoded.Distinct().Count(),Is.EqualTo(256));
   Assert.That(ByteAlphabet.ForByte(32),Is.EqualTo('Ġ'));Assert.That(ByteAlphabet.ForByte(10),Is.EqualTo('Ċ'));Assert.Throws<ArgumentException>(()=>ByteAlphabet.DecodeBytes("汉"));
  }
  [Test]public void PinnedActualBpeAssetsAreValidated()
  {
   var data=TokenizerAssets.Parse(File.ReadAllText(Asset));Assert.That(data.Vocabulary.Count,Is.EqualTo(151643));Assert.That(data.MergeRanks.Count,Is.EqualTo(151387));Assert.That(data.AddedTokens.Count,Is.EqualTo(22));Assert.That(data.AddedTokens.Count(t=>t.IsSpecial),Is.EqualTo(14));Assert.That(data.ById[151645],Is.EqualTo("<|im_end|>"));
  }
  [Test]public void All400UpstreamNormalizedPreTokensMatchExactly()
  {
   const string folder="Docs/Tests/Stage3/Evidence/S3-A02";var cases=(JArray)JObject.Parse(File.ReadAllText(folder+"/TokenizerGoldens.json"))["cases"];Assert.That(cases.Count,Is.EqualTo(400));
   for(var i=0;i<cases.Count;i++)
   {
    var sample=cases[i];Assert.That(QwenPreTokenizer.Normalize(sample["text"].Value<string>()),Is.EqualTo(sample["normalized"].Value<string>()),"NFC case"+i);
    CollectionAssert.AreEqual(sample["preTokens"].ToObject<string[]>(),QwenPreTokenizer.SplitEncoded(sample["text"].Value<string>()),"preToken case"+i);
   }
   File.WriteAllText(folder+"/PreTokens_"+DateTime.UtcNow.Ticks+".json",new JObject{{"cases",cases.Count},{"matched",true},{"normalization","NFC"},{"unicodeSource","HF tokenizers0.22.2 Regex"}}.ToString());
  }
  [TestCase("normalizer")][TestCase("regex")][TestCase("dropout")][TestCase("role")]
  public void UnsupportedAssetFeaturesAreRejected(string kind)
  {
   var root=JObject.Parse(File.ReadAllText(Asset));switch(kind){case "normalizer":root["normalizer"]["type"]="NFKC";break;case "regex":root["pre_tokenizer"]["pretokenizers"][0]["pattern"]["Regex"]=".*";break;case "dropout":root["model"]["dropout"]=.1;break;case "role":root["added_tokens"][1]["content"]="<|forged_role|>";break;}
   Assert.Throws<InvalidDataException>(()=>TokenizerAssets.Parse(root.ToString()));
  }
  [Test]public void InvalidUtf16AndOversizedTextAreRejected()
  {
   Assert.Throws<ArgumentException>(()=>QwenPreTokenizer.SplitEncoded("\ud800x"));Assert.Throws<ArgumentException>(()=>QwenPreTokenizer.SplitEncoded("\udc00"));Assert.Throws<ArgumentNullException>(()=>QwenPreTokenizer.SplitEncoded(null));Assert.Throws<ArgumentException>(()=>QwenPreTokenizer.SplitEncoded(new string('a',QwenPreTokenizer.MaximumCharacters+1)));
  }
 }
}
