using System.Collections.Generic;
using System.IO;
using EchoStation.AI.IntentJson;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class IntentJsonParserTests
 {
  public static IEnumerable<TestCaseData> Cases()
  {foreach(var row in JArray.Parse(File.ReadAllText("Docs/Tests/Stage3/Evidence/S3-J01/ParserCases.json"))){var units=row["utf16Units"].ToObject<ushort[]>();var chars=new char[units.Length];for(var i=0;i<chars.Length;i++)chars[i]=(char)units[i];yield return new TestCaseData(new string(chars),row["valid"].Value<bool>()).SetName("StrictJSON_"+row["name"].Value<string>());}}
  [TestCaseSource(nameof(Cases))]public void FixedCrossLanguageBoundaryCases(string raw,bool valid)
  {var result=IntentJsonParser.Parse(raw);Assert.That(result.Success,Is.EqualTo(valid),result.Failure.ToString());Assert.That(result.Intent!=null,Is.EqualTo(valid));}
  [Test]public void ParsedFieldsAreDecodedWithoutGameObjectsOrStateWrites()
  {var r=IntentJsonParser.Parse("{\"content\":\"我前去扫描。\",\"targetName\":\"power_panel_01\",\"intent\":\"investigate\"}");Assert.That(r.Success,Is.True);Assert.That(r.Intent.Kind,Is.EqualTo(LlmIntentKind.Investigate));Assert.That(r.Intent.TargetName,Is.EqualTo("power_panel_01"));Assert.That(r.Intent.DangerTarget,Is.Null);Assert.That(r.Intent.Content,Is.EqualTo("我前去扫描。"));}
  [Test]public void ParseFailureLineIsExactlyApprovedText()
  {Assert.That(IntentJsonParser.FallbackLine,Is.EqualTo("抱歉，我没能理解你的指令，请换一种说法。"));Assert.That(IntentJsonParser.Parse(null).Success,Is.False);}
  [Test]public void SchemaPublishesSixClosedObjectBranches()
  {var schema=JObject.Parse(File.ReadAllText("Docs/Stage3/intent.schema.json"));Assert.That(((JArray)schema["oneOf"]).Count,Is.EqualTo(6));foreach(var branch in schema["oneOf"]){Assert.That(branch["type"].Value<string>(),Is.EqualTo("object"));Assert.That(branch["additionalProperties"].Value<bool>(),Is.False);}}
 }
}
