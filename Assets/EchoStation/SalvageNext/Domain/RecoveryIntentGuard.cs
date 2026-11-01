using System;
using System.Linq;
using EchoStation.AI.Authorization;
using EchoStation.AI.Context;
using EchoStation.AI.IntentJson;
using EchoStation.Echo;
using EchoStation.Salvage;

namespace EchoStation.SalvageNext
{
    public sealed class RecoveryIntentDecision
    {
        public IntentRequest Action { get; }
        public string Line { get; }
        public string Reason { get; }
        public bool Refusal { get; }
        internal RecoveryIntentDecision(string line,string reason,bool refusal=false,IntentRequest action=null){Line=line;Reason=reason;Refusal=refusal;Action=action;}
    }
    public static class RecoveryIntentGuard
    {
        const string Refusal="我不能代为搬运、交付、开门或操作设备。我可以扫描并提供已知信息。";
        public static RecoveryIntentDecision Evaluate(string raw,PreparedPrompt prepared,RecoveryKnowledge requested,RecoveryKnowledge current,InferenceSnapshot currentScope)
        {
            var parsed=IntentJsonParser.Parse(raw);if(!parsed.Success)return new RecoveryIntentDecision(IntentJsonParser.FallbackLine,"invalid_json",true);
            if(prepared==null||requested==null||current==null||currentScope==null||requested.Epoch!=current.Epoch||requested.Revision!=current.Revision)
                return new RecoveryIntentDecision("该回复对应的任务已失效，请重新发出指令。","stale_context",true);
            var input=prepared.Snapshot.Input;
            bool question=Has(input,"怎么","如何","为什么","是什么","能否","可以吗")&&!Has(input,"替我","帮我","代我","你来","自动");
            if(!question&&Has(input,"搬运","搬走","拿起","拾取","交付","送到回收点","开门","关门","打开门","操作设备","接线","切换供电"))
                return new RecoveryIntentDecision(Refusal,"forbidden_player_action",true);
            var catalog=SalvageTargetCatalog.Items.Select(t=>new TrustedTargetIdentity(t.Id,t.Name,t.Kind==SalvageTargetKind.Cargo?t.Name.Substring(2):t.Name));
            var guarded=IntentActionGuard.Evaluate(parsed.Intent,input,prepared.EffectiveTargets,currentScope.Targets,catalog);
            if(guarded.Disposition!=GuardDisposition.Action)
            {
                if(guarded.Disposition==GuardDisposition.GroundedReply)return new RecoveryIntentDecision(KnownReply(current,input),guarded.Reason);
                return new RecoveryIntentDecision(guarded.SafeLine,guarded.Reason,guarded.Disposition==GuardDisposition.Refuse);
            }
            switch(guarded.Kind)
            {
                case LlmIntentKind.Follow:return new RecoveryIntentDecision("我会跟随你。",guarded.Reason,action:ScriptCommands.Follow());
                case LlmIntentKind.Wait:return new RecoveryIntentDecision("我在这里待命。",guarded.Reason,action:ScriptCommands.Wait());
                case LlmIntentKind.Investigate:return new RecoveryIntentDecision("我前去扫描该目标。",guarded.Reason,action:ScriptCommands.Investigate(guarded.TargetId));
                case LlmIntentKind.Warn:return new RecoveryIntentDecision("请与危险设备保持距离。",guarded.Reason,action:ScriptCommands.Warn(guarded.TargetId));
                default:return new RecoveryIntentDecision(Refusal,"unsupported_action",true);
            }
        }
        // As in the existing S3 policy, model prose never invents visible game facts.
        // Raw output remains in diagnostics and is graded BEFORE this trusted presentation step.
        static string KnownReply(RecoveryKnowledge known,string input)
        {
            if(Has(input,"推荐","建议","优先","轻量","高分","就近"))
            {
                var preference=Has(input,"轻量","轻的","最轻")?RecoveryPreference.Lightweight:Has(input,"高分","分数最高")?RecoveryPreference.HighScore:Has(input,"就近","最近")?RecoveryPreference.Nearby:known.Preference;
                var choice=known.Suggestions(preference).FirstOrDefault();
                return choice==null?"目前没有已发现、可搬运且可达的目标。可先指向物资让我调查。":"按"+RecoveryContext.PreferenceName(preference)+"，建议"+choice.Name+"（"+choice.Kilograms+"kg／"+choice.Points+"分）。请选择目标确认调查；搬运和交付仍由你完成。";
            }
            if(Has(input,"进度","结算","交付了"))return "已交付"+known.DeliveredCount+"件／"+known.DeliveredPoints+"分。至少3件且12分后可在任务板主动结算。";
            var snapshot=RecoveryContext.Snapshot(known,input,0);var found=RecoveryContext.Search().Search(snapshot.Known,input).Where(m=>m.Record is EchoStation.Memory.ClueMemory).Take(2).Select(m=>m.Record.Summary).ToArray();
            if(found.Length>0)return string.Join("\n",found);
            return Has(input,"你好","谢谢","你是谁","你能做什么")?"我是Echo，可以跟随、扫描、警戒与整理已知信息。搬运和交付由你完成。":"我没有与问题相关的已验证记录。请先明确目标或进行扫描。";
        }
        static bool Has(string value,params string[] terms)=>terms.Any(value.Contains);
    }
}
