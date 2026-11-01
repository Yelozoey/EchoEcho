using System;
using System.Linq;
using EchoStation.Dialogue;
using EchoStation.Echo;
using UnityEngine;

namespace EchoStation.Salvage
{
    [RequireComponent(typeof(SalvageCarry))]
    public sealed class SalvageCommandHub:MonoBehaviour
    {
        readonly RaycastHit[] hits=new RaycastHit[32];
        SalvageCarry carry;SalvageMotor motor;SalvageEcho echo;SalvageDelivery delivery;
        public SalvageDiscoveryIndex Known{get;}=new SalvageDiscoveryIndex();
        public DialogueSession History{get;private set;}=new DialogueSession();
        public SalvageScriptReply LastReply{get;private set;}
        public event Action Changed;
        public Func<IntentRequest,bool> Execute{private get;set;}
        public Func<bool> Cancel{private get;set;}
        void Cache(){if(carry)return;carry=GetComponent<SalvageCarry>();motor=GetComponent<SalvageMotor>();delivery=GetComponent<SalvageDelivery>();echo=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();}
        public SalvageTargetView[] Targets=>gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageTargetView>(true)).ToArray();
        public string PointedId()
        {
            Cache();int count=Physics.RaycastNonAlloc(motor.View.position,motor.View.forward,hits,12,~0,QueryTriggerInteraction.Ignore);if(count==hits.Length)return null;
            Collider nearest=null;float distance=float.PositiveInfinity;
            for(int i=0;i<count;i++){var hit=hits[i];if(hit.collider.transform.IsChildOf(transform))continue;if(hit.distance<distance){distance=hit.distance;nearest=hit.collider;}}
            var target=nearest?nearest.GetComponentInParent<SalvageTargetView>():null;return target&&target.gameObject.scene==gameObject.scene&&target.Allows(IntentKind.Investigate)?target.Id:null;
        }
        public SalvageScriptContext Context(string pointed=null)
        {Cache();return new SalvageScriptContext(Known,pointed??PointedId(),Targets.Where(t=>t.Allows(IntentKind.Investigate)).Select(t=>t.Id),echo.Battery.Charge,delivery.Count,delivery.Points);}
        public bool Submit(string text)
        {
            if(History.TryAdd(DialogueSpeaker.Player,text,false,out _)!=DialogueWriteResult.Added)return false;
            Apply(SalvageScriptCommands.Parse(text,Context()));return true;
        }
        public bool Select(IntentKind kind,string id=null)
        {
            if(kind==IntentKind.Investigate||kind==IntentKind.Warn)
            {
                var context=Context();if(id==null)id=context.Pointed;
                if(id==null||!context.CanSelect(id))return Apply(new SalvageScriptReply(ScriptReplyKind.Refuse,"请先指向目标，或选择已发现的目标。"));
                return Apply(SalvageScriptCommands.Parse((kind==IntentKind.Warn?"警告":"调查")+id,context));
            }
            if(kind==IntentKind.Follow||kind==IntentKind.Wait)return Apply(SalvageScriptCommands.Parse(kind==IntentKind.Follow?"跟随我":"待命",Context()));
            return false;
        }
        public bool RequestCancel()=>Apply(new SalvageScriptReply(ScriptReplyKind.Cancel,"已请求取消当前任务。"));
        bool Apply(SalvageScriptReply reply)
        {
            bool accepted=true;
            if(reply.Kind==ScriptReplyKind.Command)accepted=Execute?.Invoke(reply.Request)??false;
            else if(reply.Kind==ScriptReplyKind.Cancel)accepted=Cancel?.Invoke()??false;
            if(!accepted)reply=new SalvageScriptReply(ScriptReplyKind.Refuse,"当前无法执行该任务，请检查目标和机器人状态。");
            LastReply=reply;Say(reply.Speech,reply.Kind==ScriptReplyKind.Refuse);return accepted;
        }
        public void Say(string text,bool refusal=false){History.TryAdd(DialogueSpeaker.Echo,text,refusal,out _);Changed?.Invoke();}
        public void NotifyChanged()=>Changed?.Invoke();
        internal void ReplaceHistory(DialogueSession value){History=value??throw new ArgumentNullException(nameof(value));Changed?.Invoke();}
    }
}
