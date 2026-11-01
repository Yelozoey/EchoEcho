using System;
using EchoStation.Dialogue.Commands;
using EchoStation.Dialogue.Knowledge;

namespace EchoStation.AI.Authorization
{
 public static class TrustedReplyPolicy
 {
  public const string Persona="我是Echo，观测站助理机器人。我可以跟随、扫描、警戒，并整理你已发现的记录。设备和机关需要由你操作。";
  public static string Resolve(GuardedIntent decision,string originalInput,ScriptKnowledgeResponder knowledge,ulong conversationVersion=1)
  {
   if(decision==null||decision.Disposition==GuardDisposition.Action)throw new ArgumentException("A non-action authorization decision is required");
   if(decision.Disposition==GuardDisposition.Refuse||decision.Disposition==GuardDisposition.Clarify)return decision.SafeLine;
   if(knowledge==null)return "记忆同步尚未完成，请稍后再问。";
   string text;
   try{text=TargetNameMatcher.Normalize(originalInput,200,400).TrimEnd('。','.','！','!','？','?');}catch(ArgumentException){return "请先明确需要查询的信息。";}
   if(text=="你好"||text=="你好echo"||text=="echo你好"||text=="您好"||text=="hello"||text=="hi")return "你好。我已就绪，可以协助扫描与整理线索。";
   if(text=="谢谢"||text=="谢谢你")return "收到。我会继续协助你。";
   if(text=="你是谁"||text=="自我介绍"||text=="你叫什么"||text=="你能做什么"||text=="你会做什么")return Persona;
   var kind=QuestionKind(text);
   var result=knowledge.AnswerKnown(originalInput,kind,conversationVersion);
   return result==null||string.IsNullOrWhiteSpace(result.Content)||result.Content.Length>280?ScriptKnowledgeResponder.NoRecords:result.Content;
  }
  private static ScriptQuestionKind QuestionKind(string text)
  {
   var password=Has(text,"密码","服务码","维护码");
   var negative=Has(text,"不要","不想","别","不必","不需要","不用","不能","禁止","不告诉");
   var explicitCode=Has(text,"是多少","是什么","复述","再说","提醒我","给我密码","查询密码","显示密码","查看密码","告诉我密码","告诉我维护码","告诉我服务码")||text=="密码"||text=="维护码"||text=="服务码";
   if(password&&explicitCode&&!negative)return ScriptQuestionKind.Password;
   if(Has(text,"提示","下一步","没头绪","卡住","怎么继续"))return ScriptQuestionKind.Hint;
   if(Has(text,"整理线索","总结线索","总结记录","知道什么","知道了什么","汇总线索"))return ScriptQuestionKind.Summary;
   return ScriptQuestionKind.Fact;
  }
  private static bool Has(string text,params string[] terms){foreach(var term in terms)if(text.Contains(term))return true;return false;}
 }
}
