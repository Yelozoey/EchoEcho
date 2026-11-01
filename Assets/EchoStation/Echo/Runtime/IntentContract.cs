using System;
using EchoStation.World;
using UnityEngine;

namespace EchoStation.Echo
{
    public static class GameRuntimeOptions
    {
        public static bool UseLLM=false;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()=>UseLLM=false;
    }
    public enum IntentKind{Follow,Investigate,Wait,Warn,Answer,Refuse}
    public enum IntentRejection{None,MissingRequest,UnknownIntent,InvalidFields,UnknownTarget,ForbiddenTarget,ExecutorRejected}
    public sealed class IntentRequest
    {
        public IntentKind Kind{get;}public string TargetName{get;}public string DangerTarget{get;}public string Content{get;}
        public IntentRequest(IntentKind kind,string targetName=null,string dangerTarget=null,string content=null){Kind=kind;TargetName=targetName;DangerTarget=dangerTarget;Content=content;}
    }
    public readonly struct BehaviorIntent
    {
        public IntentKind Kind{get;}public WorldTarget Target{get;}
        internal BehaviorIntent(IntentKind kind,WorldTarget target){Kind=kind;Target=target;}
    }
    public interface IIntentSink
    {
        bool TryBeginBehavior(BehaviorIntent intent);
        void Speak(string content,bool refusal);
    }
    public sealed class IntentDispatcher
    {
        private readonly TargetRegistry registry;private readonly IIntentSink sink;
        public IntentDispatcher(TargetRegistry targets,IIntentSink executor){registry=targets??throw new ArgumentNullException(nameof(targets));sink=executor??throw new ArgumentNullException(nameof(executor));}
        public bool TryDispatch(IntentRequest request,out IntentRejection rejection)
        {
            rejection=IntentRejection.None;if(request==null)return Reject(IntentRejection.MissingRequest,out rejection);
            if(!Enum.IsDefined(typeof(IntentKind),request.Kind))return Reject(IntentRejection.UnknownIntent,out rejection);
            var targetName=request.TargetName;var danger=request.DangerTarget;var content=request.Content;WorldTarget target=null;
            switch(request.Kind)
            {
                case IntentKind.Follow:case IntentKind.Wait:
                    if(!string.IsNullOrEmpty(targetName)||!string.IsNullOrEmpty(danger)||!string.IsNullOrEmpty(content))return Reject(IntentRejection.InvalidFields,out rejection);break;
                case IntentKind.Answer:case IntentKind.Refuse:
                    if(!string.IsNullOrEmpty(targetName)||!string.IsNullOrEmpty(danger)||!ValidSpeech(content))return Reject(IntentRejection.InvalidFields,out rejection);
                    sink.Speak(content,request.Kind==IntentKind.Refuse);return true;
                case IntentKind.Investigate:case IntentKind.Warn:
                    var investigate=request.Kind==IntentKind.Investigate;var id=investigate?targetName:danger;
                    if(!TargetRegistry.IsValidId(id)||!string.IsNullOrEmpty(investigate?danger:targetName)||!ValidSpeech(content))return Reject(IntentRejection.InvalidFields,out rejection);
                    if(!registry.TryGet(id,out target))return Reject(IntentRejection.UnknownTarget,out rejection);
                    var permissions=target.GetComponent<EchoTargetPermissions>();
                    if(!permissions||(investigate?!permissions.CanInvestigate:!permissions.IsDanger))return Reject(IntentRejection.ForbiddenTarget,out rejection);break;
            }
            if(!sink.TryBeginBehavior(new BehaviorIntent(request.Kind,target)))return Reject(IntentRejection.ExecutorRejected,out rejection);
            if(!string.IsNullOrEmpty(content))sink.Speak(content,false);return true;
        }
        private static bool ValidSpeech(string content)=>!string.IsNullOrWhiteSpace(content)&&content.Length<=280;
        private static bool Reject(IntentRejection value,out IntentRejection output){output=value;return false;}
    }
    public static class ScriptCommands
    {
        public static IntentRequest Follow()=>new IntentRequest(IntentKind.Follow);
        public static IntentRequest Wait()=>new IntentRequest(IntentKind.Wait);
        public static IntentRequest Investigate(string id)=>new IntentRequest(IntentKind.Investigate,targetName:id,content:"我前往指定位置扫描。");
        public static IntentRequest Warn(string id)=>new IntentRequest(IntentKind.Warn,dangerTarget:id,content:"该设备存在风险，请保持距离。");
        public static IntentRequest Answer(string content)=>new IntentRequest(IntentKind.Answer,content:content);
        public static IntentRequest Refuse(string content)=>new IntentRequest(IntentKind.Refuse,content:content);
    }
}
