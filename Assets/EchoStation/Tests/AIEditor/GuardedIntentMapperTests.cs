using System;
using System.Linq;
using EchoStation.AI.Authorization;
using EchoStation.AI.Context;
using EchoStation.AI.Execution;
using EchoStation.AI.IntentJson;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Echo;
using EchoStation.Memory;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class GuardedIntentMapperTests
 {
  private static GuardedIntent Decision(string input,string kind,string id=null)
  {
   var field=kind=="investigate"?",\"targetName\":\""+id+"\"":kind=="warn"?",\"dangerTarget\":\""+id+"\"":"";
   var content=kind=="follow"||kind=="wait"?"":",\"content\":\"机关已激活，密码9999。\"";var parsed=IntentJsonParser.Parse("{\"intent\":\""+kind+"\""+field+content+"}");Assert.That(parsed.Success,Is.True);
   var targets=new[]{new TargetCandidate("panel_01","供电面板","lab",true,true,false,true,1),new TargetCandidate("wire_01","裸露电缆","lab",true,false,true,true,2)};
   var scope=new InferenceSnapshot(new MemoryStore(),input,0,"lab",targets).Targets;
   return IntentActionGuard.Evaluate(parsed.Intent,input,scope,scope,new[]{new TrustedTargetIdentity("panel_01","供电面板"),new TrustedTargetIdentity("wire_01","裸露电缆")});
  }
  [TestCase("跟随我","follow",null,IntentKind.Follow)][TestCase("原地等待","wait",null,IntentKind.Wait)][TestCase("调查供电面板","investigate","panel_01",IntentKind.Investigate)][TestCase("警告裸露电缆","warn","wire_01",IntentKind.Warn)][TestCase("你是谁","answer",null,IntentKind.Answer)][TestCase("替我关闭实验室的门","refuse",null,IntentKind.Refuse)]
  public void AllSixAuthorizedKindsMapToExistingTraditionalContract(string input,string kind,string id,IntentKind expected)
  {
   var memory=new MemorySession();var knowledge=new ScriptKnowledgeResponder(memory,new MemorySearch());var request=GuardedIntentMapper.Create(Decision(input,kind,id),input,knowledge);
   Assert.That(request.Kind,Is.EqualTo(expected));
   if(expected==IntentKind.Investigate)Assert.That(request.TargetName,Is.EqualTo(id));else Assert.That(request.TargetName,Is.Null);
   if(expected==IntentKind.Warn)Assert.That(request.DangerTarget,Is.EqualTo(id));else Assert.That(request.DangerTarget,Is.Null);
   if(request.Content!=null)Assert.That(request.Content.Length,Is.InRange(1,280));Assert.That(request.Content??"",Does.Not.Contain("9999").And.Not.Contain("机关已激活"));
  }
  [Test]public void ClarificationCannotBecomeMovement()
  {var request=GuardedIntentMapper.Create(Decision("调查不存在的摄像头","investigate","panel_01"),"调查不存在的摄像头",null);Assert.That(request.Kind,Is.EqualTo(IntentKind.Answer));Assert.That(request.TargetName,Is.Null);}
  [Test]public void MappingCannotWriteKnownStateAndHasNoEngineReference()
  {var memory=new MemorySession();var before=memory.CaptureJson();GuardedIntentMapper.Create(Decision("你好","answer"),"你好",new ScriptKnowledgeResponder(memory,new MemorySearch()));Assert.That(memory.CaptureJson(),Is.EqualTo(before));Assert.That(typeof(GuardedIntentMapper).Assembly.GetReferencedAssemblies().Any(a=>a.Name.StartsWith("UnityEngine",StringComparison.Ordinal)),Is.False);}
  [Test]public void MissingAuthorizationNeverMaps()
  {Assert.Throws<ArgumentNullException>(()=>GuardedIntentMapper.Create(null,"跟随我",null));}
 }
}
