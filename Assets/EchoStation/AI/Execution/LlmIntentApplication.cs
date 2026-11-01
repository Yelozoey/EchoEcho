using System;
using System.Collections.Generic;
using EchoStation.AI.Authorization;
using EchoStation.AI.Context;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Echo;

namespace EchoStation.AI.Execution
{
 public enum ApplicationStatus { Applied, ExecutorRejected, ParseFallback, Stale, InvalidCompletion }
 public sealed class IntentApplicationResult
 {
  public ApplicationStatus Status {get;}public bool Accepted {get;}public IntentKind? DeliveredKind {get;}public GuardDisposition? Guard {get;}public string Reason {get;}
  public bool IsFault=>Status==ApplicationStatus.InvalidCompletion||Status==ApplicationStatus.ParseFallback;
  internal IntentApplicationResult(ApplicationStatus status,bool accepted=false,IntentKind? kind=null,GuardDisposition? guard=null,string reason=null)
  {Status=status;Accepted=accepted;DeliveredKind=kind;Guard=guard;Reason=reason;}
 }
 public static class LlmIntentApplication
 {
  public static IntentApplicationResult Apply(InferenceSessionHost host,StampedCompletion completion,InferenceSnapshot submitted,IReadOnlyList<AllowedTarget> current,IEnumerable<TrustedTargetIdentity> catalog,ScriptKnowledgeResponder knowledge,ulong conversationVersion,Func<IntentRequest,bool> execute)
  {
   if(host==null||execute==null)throw new ArgumentNullException("Application dependencies");
   if(completion==null||submitted==null)return Invalid("missing_completion_or_snapshot");
   if(!host.IsCurrent(completion.Stamp))return new IntentApplicationResult(ApplicationStatus.Stale);
   var result=completion.Completion;var generation=result.Generation;
   if(result.Outcome!=RequestOutcome.Succeeded||generation==null||generation.Prepared==null||!ReferenceEquals(generation.Prepared.Snapshot,submitted))return Invalid("untrusted_or_mismatched_completion");
   var parsed=generation.Complete?IntentJsonParser.Parse(generation.Text):null;GuardedIntent decision=null;IntentRequest request;
   var parseFault=parsed==null||!parsed.Success;
   if(parseFault)request=ScriptCommands.Answer(IntentJsonParser.FallbackLine);
   else
   {
    decision=IntentActionGuard.Evaluate(parsed.Intent,generation.Prepared,current,catalog);
    request=GuardedIntentMapper.Create(decision,submitted.Input,knowledge,conversationVersion);
   }
   // No yield/await between the final claim and traditional execution; a taken result is not permission.
   if(!host.TryClaim(completion.Stamp))return new IntentApplicationResult(ApplicationStatus.Stale);
   var accepted=execute(request);
   return new IntentApplicationResult(parseFault?ApplicationStatus.ParseFallback:accepted?ApplicationStatus.Applied:ApplicationStatus.ExecutorRejected,accepted,request.Kind,decision?.Disposition,parseFault?"strict_json_or_eos":decision?.Reason);
  }
  private static IntentApplicationResult Invalid(string reason)=>new IntentApplicationResult(ApplicationStatus.InvalidCompletion,reason:reason);
 }
}
