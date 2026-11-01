using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3TuningContextTests
 {
  private static JArray Cases()=> (JArray)JObject.Parse(File.ReadAllText("Tools/Stage3/Fixtures/Q3_Validation100_B.json"))["cases"];
  [Test] public void EveryHeldOutCaseRetainsAllFivePermissions()
  {
   foreach(var row in Cases())
   {
    var snapshot=Qwen3TuningCaseContext.Create(row,true,0);Assert.That(snapshot.Targets.Count,Is.EqualTo(5));
    foreach(var target in snapshot.Targets)
    {var raw=row["targets"].First(t=>t["id"].Value<string>()==target.Id);Assert.That(target.CanInvestigate,Is.EqualTo(raw["actions"].Values<string>().Contains("investigate")));Assert.That(target.IsDanger,Is.EqualTo(raw["actions"].Values<string>().Contains("warn")));}
   }
  }
  [Test] public void ExpectedLabelAndTargetCannotNarrowGrammar()
  {
   var row=Cases()[30];var changed=row.DeepClone();changed["expected"]="Refuse";changed["target"]="answer_only_sentinel";
   var before=Qwen3TuningCaseContext.Create(row,true,0);var after=Qwen3TuningCaseContext.Create(changed,true,0);
   Assert.That(after.Targets.Select(t=>t.Id),Is.EqualTo(before.Targets.Select(t=>t.Id)));Assert.That(after.Targets.Count,Is.EqualTo(5));
   Assert.That(after.Targets.Count(t=>t.CanInvestigate),Is.EqualTo(3));Assert.That(after.Targets.Count(t=>t.IsDanger),Is.EqualTo(2));
  }
  [Test] public void LegacySmokeTargetInventoryUnchanged()
  {var snapshot=Qwen3TuningCaseContext.Create(new JObject{{"input","请去调查供电面板。"}},false,0);Assert.That(snapshot.Zone,Is.EqualTo("cabin"));Assert.That(snapshot.Targets.Select(t=>t.Id),Is.EquivalentTo(new[]{"power_panel_01","hazard_01"}));}
  [Test] public void DuplicateAndOversizedTargetListsFailClosed()
  {
   var row=Cases()[0].DeepClone();((JArray)row["targets"]).Add(row["targets"][0].DeepClone());Assert.Throws<InvalidDataException>(()=>Qwen3TuningCaseContext.Create(row,true,0));
   while(((JArray)row["targets"]).Count<9)((JArray)row["targets"]).Add(row["targets"][0].DeepClone());Assert.Throws<InvalidDataException>(()=>Qwen3TuningCaseContext.Create(row,true,0));
  }
  [Test] public void ExtraOperationPermissionRejected()
  {var row=Cases()[0].DeepClone();row["targets"][0]["actions"]=new JArray("open_door");Assert.Throws<InvalidDataException>(()=>Qwen3TuningCaseContext.Create(row,true,0));}
  [Test] public void MissingScopeCannotSilentlyReuseSmokeTargets()
  {Assert.Throws<InvalidDataException>(()=>Qwen3TuningCaseContext.Create(new JObject{{"input","扫描设备"}},true,0));}
 }
}
