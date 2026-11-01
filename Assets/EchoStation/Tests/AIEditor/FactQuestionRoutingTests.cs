using EchoStation.AI.Authorization;
using EchoStation.AI.Context;
using EchoStation.AI.IntentJson;
using EchoStation.Memory;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class FactQuestionRoutingTests
 {
  [TestCase("调查入口巡检记录")][TestCase("扫描入口巡检记录")][TestCase("检查入口巡检记录")][TestCase("去勘查入口巡检记录")]
  public void RecordWordInsideExplicitTargetNameDoesNotTurnScanningIntoAQuestion(string input)
  {Assert.That(Evaluate(input).Disposition,Is.EqualTo(GuardDisposition.Action));}
  [TestCase("入口巡检记录是什么")][TestCase("说说入口巡检记录")][TestCase("查看入口巡检记录的资料")][TestCase("入口巡检记录有哪些信息")][TestCase("调查入口巡检记录的背景")]
  public void ActualFactQuestionsStillNeverDispatchScanning(string input)
  {Assert.That(Evaluate(input).Disposition,Is.EqualTo(GuardDisposition.GroundedReply));}
  private static GuardedIntent Evaluate(string input)
  {
   var parsed=IntentJsonParser.Parse("{\"intent\":\"investigate\",\"targetName\":\"note_entry_inspection_01\",\"content\":\"我前去扫描。\"}");
   var candidate=new TargetCandidate("note_entry_inspection_01","入口巡检记录","valley",true,true,false,true,2);var snapshot=new InferenceSnapshot(new MemoryStore(),input,0,"valley",new[]{candidate});
   return IntentActionGuard.Evaluate(parsed.Intent,input,snapshot.Targets,snapshot.Targets,new[]{new TrustedTargetIdentity(candidate.Id,candidate.Name)});
  }
 }
}
