using System;
using System.IO;
using System.Linq;
using System.Text;
using EchoStation.AI.Models;
using EchoStation.AI.Tokenization;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3BpeTokenizerTests
 {
  private QwenByteBpeTokenizer tokenizer;private JObject golden;
  [OneTimeSetUp]public void Setup()
  {
   tokenizer=new QwenByteBpeTokenizer(TokenizerAssets.ParseQwen3(File.ReadAllText("Assets/StreamingAssets/EchoLLM/"+Qwen3Catalog.DirectoryName+"/tokenizer.json")));
   golden=JObject.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-Q3-A02b/TokenizerGoldens.json"));
   Assert.That(golden["sourceTokenizerSha256"].Value<string>(),Is.EqualTo(Qwen3Catalog.Current.Files["tokenizer.json"].Sha256));
  }
  [Test]public void All400OfficialIdSequencesAndDecodesMatch()
  {
   var cases=(JArray)golden["cases"];Assert.That(cases.Count,Is.EqualTo(400));var clock=System.Diagnostics.Stopwatch.StartNew();
   for(var i=0;i<cases.Count;i++)
   {
    var sample=cases[i];var ids=tokenizer.EncodePlain(sample["text"].Value<string>());
    CollectionAssert.AreEqual(sample["ids"].Values<int>().ToArray(),ids,"IDs case"+i);
    Assert.That(tokenizer.Decode(ids),Is.EqualTo(sample["decoded"].Value<string>()),"decode case"+i);
    Assert.That(tokenizer.Decode(ids,strictUtf8:true),Is.EqualTo(sample["normalized"].Value<string>()),"NFC roundtrip case"+i);
   }
   File.WriteAllText("Docs/Tests/Stage3/Evidence/S3-Q3-A02b/Ids_"+DateTime.UtcNow.Ticks+".json",new JObject{{"cases",400},{"idsMatched",true},{"decodedMatched",true},{"definedTokens",tokenizer.DefinedTokenCount},{"seconds",clock.Elapsed.TotalSeconds}}.ToString());
  }
  [Test]public void All256ByteDecodesMatchOfficialTokenizer()
  {foreach(var sample in golden["byteDecodes"])Assert.That(tokenizer.Decode(new[]{sample["id"].Value<int>()}),Is.EqualTo(sample["decoded"].Value<string>()),"byte"+sample["byte"]);}
  [Test]public void All26AddedTokensDecodeAndSkipFlagsMatch()
  {
   var sample=golden["addedDecode"];var ids=sample["ids"].Values<int>().ToArray();Assert.That(tokenizer.DefinedTokenCount,Is.EqualTo(151669));
   Assert.That(tokenizer.Decode(ids),Is.EqualTo(sample["all"].Value<string>()));Assert.That(tokenizer.Decode(ids,true),Is.EqualTo(sample["skipSpecial"].Value<string>()));
  }
  [Test]public void LiteralDataCannotPromoteAnySpecialToken()
  {var text=golden["addedDecode"]["all"].Value<string>();var ids=tokenizer.EncodePlain(text);Assert.That(ids.Any(tokenizer.IsSpecial),Is.False);Assert.That(tokenizer.Decode(ids),Is.EqualTo(text));}
  [Test]public void FourNewOrdinaryIdsRemainAvailableWithoutToolPrivilege()
  {
   var markers=new[]{"<tool_response>","</tool_response>","<think>","</think>"};
   for(var i=0;i<4;i++){CollectionAssert.AreEqual(new[]{151665+i},tokenizer.EncodePlain(markers[i]));Assert.That(tokenizer.IsSpecial(151665+i),Is.False);Assert.That(tokenizer.Decode(new[]{151665+i}),Is.EqualTo(markers[i]));}
  }
  [Test]public void UnknownIdsBadUtf8AndBadUtf16FailExplicitly()
  {
   Assert.Throws<InvalidDataException>(()=>tokenizer.Decode(new[]{-1}));Assert.Throws<InvalidDataException>(()=>tokenizer.Decode(new[]{151669}));Assert.Throws<InvalidDataException>(()=>tokenizer.Decode(new[]{151935}));
   var id=golden["byteDecodes"].Single(t=>t["byte"].Value<int>()==0xe4)["id"].Value<int>();Assert.Throws<DecoderFallbackException>(()=>tokenizer.Decode(new[]{id},strictUtf8:true));
   Assert.Throws<ArgumentException>(()=>tokenizer.EncodePlain("\ud800"));Assert.Throws<ArgumentException>(()=>tokenizer.EncodePlain("\udc00"));Assert.That(tokenizer.EncodePlain(""),Is.Empty);
  }
  [Test]public void ActualTokenCountAndLargePiecesRemainBounded()
  {
   Assert.That(tokenizer.EncodePlain(new string('1',1536)).Length,Is.EqualTo(1536));var text=new string('x',8192);Assert.That(tokenizer.Decode(tokenizer.EncodePlain(text)),Is.EqualTo(text));
   Assert.Throws<ArgumentException>(()=>tokenizer.EncodePlain(new string('x',QwenPreTokenizer.MaximumCharacters+1)));
  }
  [Test]public void AdjacentTrustedTextNormalizesTogether()
  {CollectionAssert.AreEqual(tokenizer.EncodePlain("é"),new TrustedTokenSequence(tokenizer).Text("e").Text("\u0301").ToArray());Assert.Throws<ArgumentException>(()=>new TrustedTokenSequence(tokenizer).Separator((ChatSeparator)1));}
 }
}
