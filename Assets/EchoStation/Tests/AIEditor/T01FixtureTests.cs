using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.AI.IntentJson;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class T01FixtureTests
 {
  [Test]public void FrozenNew100HasExactlyThePredeclaredSixIntentDistributionAndReadOnlyRows()
  {var cases=T01QualityFixture.Load();Assert.That(cases.Count,Is.EqualTo(100));var expected=new[]{15,20,15,10,25,15};var kinds=new[]{LlmIntentKind.Follow,LlmIntentKind.Investigate,LlmIntentKind.Wait,LlmIntentKind.Warn,LlmIntentKind.Answer,LlmIntentKind.Refuse};for(var i=0;i<kinds.Length;i++)Assert.That(cases.Count(c=>c.Expected==kinds[i]),Is.EqualTo(expected[i]));Assert.Throws<NotSupportedException>(()=>((IList<T01Case>)cases)[0]=cases[1]);}
  [Test]public void ClearFixtureInputsDoNotReusePriorHeldOutB100()
  {
   var old=JToken.Parse(File.ReadAllText("Tools/Stage3/Fixtures/Q3_Validation100_B.json"));var rows=old as JArray??(JArray)(old["cases"]??old["rows"]);var seen=new HashSet<string>(rows.Select(r=>Normalize(r.Value<string>("input"))),StringComparer.Ordinal);Assert.That(T01QualityFixture.Load().Where(c=>seen.Contains(Normalize(c.Input))).Select(c=>c.Id),Is.Empty);
  }
  private static string Normalize(string text)=>text.Trim().TrimEnd('。','.','!','！','?','？');
  [TestCase("expected","DoorOpen")][TestCase("setup","unknown_world")][TestCase("target","unknown_target")][TestCase("id","wrong_id")]
  public void InvalidLabelsContextsAndOrderAreRejected(string field,string value)
  {var rows=JArray.Parse(File.ReadAllText(T01QualityFixture.Path));rows[0][field]=value;Assert.Throws<ArgumentException>(()=>T01QualityFixture.Parse(rows.ToString()));}
  [Test]public void OversizedInputExtraFieldWrongCountAndMissingTargetAreRejected()
  {
   var source=File.ReadAllText(T01QualityFixture.Path);var rows=JArray.Parse(source);rows[0]["input"]=new string('中',201);Assert.Throws<ArgumentException>(()=>T01QualityFixture.Parse(rows.ToString()));rows=JArray.Parse(source);rows[0]["answer_leak"]="Follow";Assert.Throws<ArgumentException>(()=>T01QualityFixture.Parse(rows.ToString()));rows=JArray.Parse(source);rows.RemoveAt(0);Assert.Throws<ArgumentException>(()=>T01QualityFixture.Parse(rows.ToString()));rows=JArray.Parse(source);rows[30]["target"]=null;Assert.Throws<ArgumentException>(()=>T01QualityFixture.Parse(rows.ToString()));
  }
 }
}
