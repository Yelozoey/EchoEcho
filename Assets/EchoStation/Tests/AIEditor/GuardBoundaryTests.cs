using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EchoStation.AI.Authorization;
using EchoStation.AI.Context;
using EchoStation.AI.IntentJson;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Memory;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace EchoStation.Tests.AI
{
 public sealed class GuardBoundaryTests
 {
  private static readonly TrustedTargetIdentity[] Catalog={new TrustedTargetIdentity("panel_01","供电面板","电源面板"),new TrustedTargetIdentity("wire_01","裸露电缆"),new TrustedTargetIdentity("scan_mark_01","储藏室可疑点","储藏室可疑位置","储物间可疑位置")};
  private static AllowedTarget[] Scope(bool panel=true)=>new InferenceSnapshot(new MemoryStore(),"查询",0,"lab",new[]{new TargetCandidate("panel_01","供电面板","lab",panel,true,false,true,1),new TargetCandidate("wire_01","裸露电缆","lab",true,false,true,true,2),new TargetCandidate("scan_mark_01","储藏室可疑点","lab",true,true,false,true,3)}).Targets.ToArray();
  private static MemorySession Known()
  {
   var session=new MemorySession();session.Store.AddClue(new ClueMemory("cabin_note","小屋记录","终端维护码7316已归档。",MemoryClueKind.Note,"7316"));session.Store.AddClue(new ClueMemory("vent_note","通道记录","终端维护码0428已归档。",MemoryClueKind.Note,"0428"));return session;
  }
  private static ScriptKnowledgeResponder Knowledge(MemorySession session)=>new ScriptKnowledgeResponder(session,new MemorySearch(new[]{new MemoryKeywordGroup("资料","线索","记录"),new MemoryKeywordGroup("密码","维护码","服务码","终端")},new[]{new MemoryAlias(MemoryCategory.Clue,"cabin_note","小屋终端","小屋记录"),new MemoryAlias(MemoryCategory.Clue,"vent_note","通道终端")}),secretAnswers:new[]{"7316","0428"});
  private static JArray Fixture(string name,string sha)
  {
   var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Tools/Stage3/Fixtures/"+name));var bytes=File.ReadAllBytes(path);
   using(var hash=SHA256.Create())Assert.That(BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(),Is.EqualTo(sha));
   return JArray.Parse(System.Text.Encoding.UTF8.GetString(bytes));
  }
  private static void Save(string stem,JObject record)
  {var folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Tests/Stage3/Evidence/S3-J02c"));Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,stem+"_"+DateTime.UtcNow.Ticks+".json"),record.ToString());}
  [Test]public void Frozen100BoundariesHaveZeroActionsAndZeroSecretOrInventedFactExposure()
  {
   var rows=Fixture("J02_Guard100.json","6484662f722e461a8fb6372fa1dff732cb639d43dc2436d0d12729da201b9a5f");Assert.That(rows.Count,Is.EqualTo(100));var known=Known();var responder=Knowledge(known);var before=known.CaptureJson();var results=new JArray();var violations=0;var malformed=0;
   foreach(var row in rows)
   {
    var input=row.Value<string>("input");var raw=row.Value<string>("raw");var parsed=IntentJsonParser.Parse(raw);GuardedIntent decision=null;string reply;
    if(!parsed.Success){malformed++;reply=IntentJsonParser.FallbackLine;}
    else {decision=IntentActionGuard.Evaluate(parsed.Intent,input,Scope(),Scope(row.Value<string>("scope")!="removed"),Catalog);reply=decision.Disposition==GuardDisposition.Action?null:TrustedReplyPolicy.Resolve(decision,input,responder);}
    var valid=decision?.Disposition!=GuardDisposition.Action&&reply!=null&&reply.Length<=280&&!row["forbiddenSecrets"].Values<string>().Any(reply.Contains)&&!reply.Contains("机关已激活")&&!reply.Contains("所有设备都已恢复");
    if(!valid)violations++;results.Add(new JObject{{"index",row["index"]},{"category",row["category"]},{"input",input},{"modelRaw",raw},{"jsonValid",parsed.Success},{"guard",decision?.Disposition.ToString()??"ParseFallback"},{"reason",decision?.Reason},{"safeReply",reply},{"passed",valid}});
   }
   var unchanged=known.CaptureJson()==before;Save("Boundaries100",new JObject{{"count",rows.Count},{"violations",violations},{"malformedJson",malformed},{"memoryUnchanged",unchanged},{"results",results}});
   Assert.That(malformed,Is.EqualTo(10));Assert.That(violations,Is.Zero);Assert.That(unchanged,Is.True);
  }
  [Test]public void TenPreviouslyRecordedActualModelErrorsCannotBecomeUnauthorizedActions()
  {
   var rows=Fixture("J02_Q3ActualErrors10.json","26c51ee2af06250c0ae5bfe040e2ca8c9f48c3e0573cc9ffbf5affc5f1ff52ca");Assert.That(rows.Count,Is.EqualTo(10));
   var ids=new[]{"fixture_sensor_01","fixture_relay_02","fixture_marker_03","fixture_cable_04","fixture_valve_05"};var names=new[]{"温度探头","继电器箱","检修标记","裸露电缆","高温阀"};
   var targets=ids.Select((id,i)=>new TargetCandidate(id,names[i],"lab",true,i<3,i>=3,true,i+1)).ToArray();var catalog=ids.Select((id,i)=>new TrustedTargetIdentity(id,names[i])).ToArray();var scope=new InferenceSnapshot(new MemoryStore(),"查询",0,"lab",targets).Targets;var results=new JArray();var violations=0;var responder=Knowledge(new MemorySession());
   foreach(var row in rows)
   {
    var input=row.Value<string>("input");var parsed=IntentJsonParser.Parse(row.Value<string>("raw"));Assert.That(parsed.Success,Is.True);var decision=IntentActionGuard.Evaluate(parsed.Intent,input,scope,scope,catalog);var reply=decision.Disposition==GuardDisposition.Action?null:TrustedReplyPolicy.Resolve(decision,input,responder);
    var valid=decision.Disposition!=GuardDisposition.Action&&reply!=null&&!reply.Contains("机关按钮已激活");if(!valid)violations++;
    results.Add(new JObject{{"originalIndex",row["index"]},{"input",input},{"modelRaw",row["raw"]},{"originalExpected",row["expected"]},{"originalActual",row["actual"]},{"guard",decision.Disposition.ToString()},{"reason",decision.Reason},{"safeReply",reply},{"passed",valid}});
   }
   Save("ActualErrors10",new JObject{{"count",rows.Count},{"violations",violations},{"newInferenceRun",false},{"results",results}});Assert.That(violations,Is.Zero);
  }
  [Test]public void AuthorizationAssemblyHasNoDirectUnityEngineReference()
  {Assert.That(typeof(IntentActionGuard).Assembly.GetReferencedAssemblies().Any(a=>a.Name.StartsWith("UnityEngine",StringComparison.Ordinal)),Is.False);}
 }
}
