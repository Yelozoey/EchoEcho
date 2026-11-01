using System;
using EchoStation.AI.Authorization;
using EchoStation.AI.IntentJson;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Echo;

namespace EchoStation.AI.Execution
{
 public static class GuardedIntentMapper
 {
  public static IntentRequest Create(GuardedIntent decision,string originalInput,ScriptKnowledgeResponder knowledge,ulong conversationVersion=1)
  {
   if(decision==null)throw new ArgumentNullException(nameof(decision));
   if(decision.Disposition!=GuardDisposition.Action)
   {
    var text=TrustedReplyPolicy.Resolve(decision,originalInput,knowledge,conversationVersion);
    return decision.Disposition==GuardDisposition.Refuse?ScriptCommands.Refuse(text):ScriptCommands.Answer(text);
   }
   switch(decision.Kind)
   {
    case LlmIntentKind.Follow:return ScriptCommands.Follow();
    case LlmIntentKind.Wait:return ScriptCommands.Wait();
    case LlmIntentKind.Investigate:return new IntentRequest(IntentKind.Investigate,targetName:decision.TargetId,content:decision.SafeLine);
    case LlmIntentKind.Warn:return new IntentRequest(IntentKind.Warn,dangerTarget:decision.TargetId,content:decision.SafeLine);
    default:throw new ArgumentException("Authorization did not produce an executable behavior");
   }
  }
 }
}
