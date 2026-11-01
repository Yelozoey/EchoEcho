using System;
using System.Linq;
using EchoStation.Dialogue;
using EchoStation.Echo;
using EchoStation.Salvage;
using UnityEngine;
namespace EchoStation.SalvageNext
{
    [RequireComponent(typeof(NextSession),typeof(SalvageCommandHub))]
    public sealed class NextAdvice:MonoBehaviour
    {
        NextSession session;SalvageCommandHub hub;RecoveryKnowledgeCapture capture;long pendingEpoch;RecoveryPreference pendingPreference;
        public string PendingTargetId{get;private set;}
        public string LastAdvice{get;private set;}="先调查可见物品，再请求回收建议。";
        void Cache(){if(session)return;session=GetComponent<NextSession>();hub=GetComponent<SalvageCommandHub>();capture=new RecoveryKnowledgeCapture();}
        void OnEnable(){Cache();session.Restored+=Clear;session.SettingsChanged+=Clear;}
        void OnDisable(){if(session){session.Restored-=Clear;session.SettingsChanged-=Clear;}Clear();}
        public static string PreferenceName(RecoveryPreference value)=>value==RecoveryPreference.Nearby?"就近":value==RecoveryPreference.HighScore?"高分":"轻量";
        public void Clear(){PendingTargetId=null;LastAdvice="建议已清除。可按当前偏好重新请求。";}
        public bool Offer(RecoveryPreference preference)
        {
            Cache();var knowledge=capture.Capture(hub,session.Epoch,preference);var first=knowledge.Suggestions(preference).FirstOrDefault();PendingTargetId=first?.Id;pendingEpoch=session.Epoch;pendingPreference=preference;
            LastAdvice=first==null?"没有已扫描、可搬运且可达的待回收物。可先指向物品让我调查。":"按"+PreferenceName(preference)+"建议："+first.Name+"，"+first.Kilograms+"kg／"+first.Points+"分，路径约"+first.PathMeters.Value.ToString("F1")+"米。确认后我前往该位置，搬运和交付由你完成。";hub.Say(LastAdvice);return first!=null;
        }
        public bool Confirm()
        {
            Cache();var id=PendingTargetId;PendingTargetId=null;
            if(id==null||pendingEpoch!=session.Epoch||!capture.Capture(hub,session.Epoch,pendingPreference).Suggestions(pendingPreference).Any(f=>f.Id==id)){LastAdvice="该建议已失效，请重新请求。";hub.Say(LastAdvice,true);return false;}
            return hub.Select(IntentKind.Investigate,id);
        }
        public bool SubmitText(string text)
        {
            Cache();if(string.IsNullOrWhiteSpace(text)||text.Length>200)return hub.Submit(text);
            var parsed=SalvageScriptCommands.Parse(text,hub.Context());var normalized=string.Concat(text.Where(c=>!char.IsWhiteSpace(c)));
            bool recommendation=normalized.Contains("建议")||normalized.Contains("推荐")||normalized.Contains("该拿什么")||normalized.Trim('。','？','?')=="任务";
            // Preserve the existing refusal-first permission check, including mixed requests.
            bool forbidden=new[]{"开门","打开门","搬运","交付","接线","修复","设置电量","修改","传送","gameobject","执行代码","替我拿"}.Any(word=>normalized.ToLowerInvariant().Contains(word));
            if(!recommendation||forbidden||parsed.Kind==ScriptReplyKind.Command||parsed.Kind==ScriptReplyKind.Cancel){Clear();return hub.Submit(text);}
            if(hub.History.TryAdd(DialogueSpeaker.Player,text,false,out _)!=DialogueWriteResult.Added)return false;
            bool nearby=normalized.Contains("就近")||normalized.Contains("最近"),score=normalized.Contains("高分"),light=normalized.Contains("轻量")||normalized.Contains("轻的");
            if((nearby?1:0)+(score?1:0)+(light?1:0)>1){Clear();LastAdvice="请只选一种偏好：就近、高分或轻量。";hub.Say(LastAdvice);return true;}
            Offer(score?RecoveryPreference.HighScore:light?RecoveryPreference.Lightweight:nearby?RecoveryPreference.Nearby:session.Preference);return true;
        }
    }
}
