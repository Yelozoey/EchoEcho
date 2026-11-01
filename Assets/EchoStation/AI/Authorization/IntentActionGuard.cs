using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.AI.IntentJson;

namespace EchoStation.AI.Authorization
{
 public enum GuardDisposition { Action, GroundedReply, Refuse, Clarify }
 public sealed class GuardedIntent
 {
  public GuardDisposition Disposition {get;}public LlmIntentKind Kind {get;}public string TargetId {get;}public string SafeLine {get;}public string Reason {get;}
  // Model prose is deliberately absent: only trusted reply generation may create visible facts.
  internal GuardedIntent(GuardDisposition disposition,LlmIntentKind kind,string target,string line,string reason)
  {Disposition=disposition;Kind=kind;TargetId=target;SafeLine=line;Reason=reason;}
 }
 public static class IntentActionGuard
 {
  public const string Refusal="我不能代替你操作门、设备或机关。我可以扫描并提供已有线索。";
  private static bool Has(string value,params string[] terms)=>terms.Any(value.Contains);
  public static GuardedIntent Evaluate(StructuredIntent intent,PreparedPrompt prepared,IReadOnlyList<AllowedTarget> current,IEnumerable<TrustedTargetIdentity> catalog)
  {
   if(prepared==null)return Deny("missing_context");
   return Evaluate(intent,prepared.Snapshot.Input,prepared.EffectiveTargets,current,catalog);
  }
  public static GuardedIntent Evaluate(StructuredIntent intent,string input,IReadOnlyList<AllowedTarget> prepared,IReadOnlyList<AllowedTarget> current,IEnumerable<TrustedTargetIdentity> catalog)
  {
   if(intent==null||!ValidScope(prepared)||!ValidScope(current))return Deny("invalid_context");
   string text;TargetNameMatch match;
   try{text=TargetNameMatcher.Normalize(input,200,400);match=TargetNameMatcher.Match(input,catalog);}catch(ArgumentException){return Deny("invalid_context");}
   if(Has(text,"<|","忽略规则","忽略以上","忽略之前","system prompt","\"intent\"","\"targetname\"","扮演开发者","执行代码"))return Clarify("instruction_boundary");
   var question=IsKnowledgeQuestion(text);
   var operationQuestion=Has(text,"如何","怎么","为什么","是什么","是多少","有哪些")&&!Has(text,"帮我","替我","代我","你来","自动","按下","按压");
   if(IsMachineOperation(text)&&!operationQuestion)return Deny("forbidden_operation");
   var stopFollowing=Has(text,"停止跟随","暂停跟随","不要跟过来","别跟过来","不要移动","别移动","别动");
   var actionText=text;
   foreach(var phrase in new[]{"停止跟随","暂停跟随","不要跟过来","别跟过来","不要移动","别移动","别动","停止等候","停止等待","别留在原处"})actionText=actionText.Replace(phrase,"");
   if(Has(actionText,"不要","别去","别跟","不能","禁止","不需要","无需","do not","don't"))return Clarify("negative_or_ambiguous_request");
   var follow=Has(actionText,"跟随","跟着","跟上","跟紧","跟在","随我","随行","follow","陪我","陪着我","与我同行","跟我","我的后面","我的后方","我身后");
   var wait=stopFollowing||Has(actionText,"等待","待命","等在","等候","停下","停在","停止前进","停止移动","保持静止","候着","留在","原地","稍等","wait");
   var investigate=Has(actionText,"调查","扫描","检查","查看","看看","勘查","侦查","inspect","investigate","scan ");
   var warn=Has(actionText,"警告","警戒","危险","风险","警报","提醒","warn");
   // Checking a dangerous target can mean warn. Other mixed action classes need clarification.
   var groups=(follow?1:0)+(wait?1:0)+(investigate?1:0)+(warn&&!investigate?1:0);
   if(groups>1&&!question)return Clarify("multiple_actions");
   if(intent.Kind==LlmIntentKind.Answer)return Reply("answer_requires_known_records");
   if(intent.Kind==LlmIntentKind.Refuse)return Deny("model_refusal");
   if(question)return Reply("question_cannot_dispatch_action");
   if(intent.Kind==LlmIntentKind.Follow)
    return follow&&!wait&&!investigate&&!warn?Action(intent.Kind,null,"我会跟随你。","explicit_follow"):Clarify("follow_not_requested");
   if(intent.Kind==LlmIntentKind.Wait)
    return wait&&!follow&&!investigate&&!warn?Action(intent.Kind,null,"我在这里待命。","explicit_wait"):Clarify("wait_not_requested");
   if(intent.Kind!=LlmIntentKind.Investigate&&intent.Kind!=LlmIntentKind.Warn)return Deny("unsupported_intent");
   if(intent.Kind==LlmIntentKind.Investigate&&!investigate||intent.Kind==LlmIntentKind.Warn&&!warn)return Clarify("action_not_requested");
   if(match.UniqueId==null)return Clarify(match.IsAmbiguous?"ambiguous_target":"unknown_or_missing_target");
   var id=intent.Kind==LlmIntentKind.Investigate?intent.TargetName:intent.DangerTarget;
   if(!string.Equals(id,match.UniqueId,StringComparison.Ordinal))return Deny("model_target_mismatch");
   var before=prepared.SingleOrDefault(t=>t.Id==id);var now=current.SingleOrDefault(t=>t.Id==id);
   if(before==null||now==null)return Deny("target_not_in_both_scopes");
   if(intent.Kind==LlmIntentKind.Investigate&&(!before.CanInvestigate||!now.CanInvestigate)||intent.Kind==LlmIntentKind.Warn&&(!before.IsDanger||!now.IsDanger))return Deny("permission_changed_or_forbidden");
   return Action(intent.Kind,id,intent.Kind==LlmIntentKind.Investigate?"我前去扫描该目标。":"我会对该目标保持警戒。","explicit_scoped_target");
  }
  private static bool ValidScope(IReadOnlyList<AllowedTarget> scope)
  {return scope!=null&&scope.Count<=8&&scope.All(t=>t!=null)&&scope.Select(t=>t.Id).Distinct(StringComparer.Ordinal).Count()==scope.Count;}
  internal static bool IsKnowledgeQuestion(string text)
  {
   if(Has(text,"有哪些","是什么","在哪里","是多少","为什么","怎么","如何","背景","总结","整理线索","提示","密码","维护码","服务码","告诉我","知道什么","意味着","解释","说说","讲讲","的资料","的记录","已知资料","什么信息","有什么信息"))return true;
   return Has(text,"资料","信息","记录")&&!Has(text,"调查","扫描","检查","勘查","侦查","investigate","scan ");
  }
  private static bool IsMachineOperation(string text)
  {
   if(Has(text,"代我解谜","替我解谜","输入密码","输入服务码","输入维护码","拨动开关","按下","按压","送电","切换供电","调整回路","操作终端","操作设备","自动解谜","solve puzzle"))return true;
   return Has(text,"打开","开启","关闭","关掉","关上","解锁","启动","激活","切换","操作","open ","close ","unlock ","activate ")&&Has(text,"门","终端","机关","按钮","回路","通风","电源","供电","设备","door","terminal","button","circuit");
  }
  private static GuardedIntent Action(LlmIntentKind kind,string id,string line,string reason)=>new GuardedIntent(GuardDisposition.Action,kind,id,line,reason);
  private static GuardedIntent Reply(string reason)=>new GuardedIntent(GuardDisposition.GroundedReply,LlmIntentKind.Answer,null,null,reason);
  private static GuardedIntent Deny(string reason)=>new GuardedIntent(GuardDisposition.Refuse,LlmIntentKind.Refuse,null,Refusal,reason);
  private static GuardedIntent Clarify(string reason)=>new GuardedIntent(GuardDisposition.Clarify,LlmIntentKind.Answer,null,"请一次明确一个行动和目标。我只会扫描、跟随、待命或警戒。",reason);
 }
}
