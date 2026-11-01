using System;
using System.Collections.Generic;
using EchoStation.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Echo
{
    [RequireComponent(typeof(EchoRig),typeof(EchoNavigation),typeof(EchoScanController)),DisallowMultipleComponent]
    public sealed class EchoBrain:MonoBehaviour,IIntentSink
    {
        [SerializeField]private bool automaticWarnings=true;
        private EchoNavigation navigation;private EchoScanController scans;private DangerPerception perception;private Predicate<WorldTarget> dangerFilter;
        private IntentBehaviorTree tree;private IntentDispatcher dispatcher;private Transform player;
        private readonly Dictionary<string,float> warningCooldowns=new Dictionary<string,float>(StringComparer.Ordinal);
        private BehaviorIntent? currentRequest,resumeRequest;private ulong manualVersion,resumeVersion;
        private float clock,senseTimer;private bool initialized,internalRequest,warningActive;
        public bool AutomaticWarningsEnabled{get=>automaticWarnings;set=>automaticWarnings=value;}
        public bool IsInitialized=>initialized;public IntentKind? CurrentMode=>tree?.CurrentMode;
        public int AutomaticWarningCount{get;private set;}
        public string LastWarningTargetId{get;private set;}public string LastSpeech{get;private set;}
        public event Action<string,bool> Speech;
        public bool Initialize(Transform followTarget,IntentKind startingMode=IntentKind.Follow)
        {
            if(startingMode!=IntentKind.Follow&&startingMode!=IntentKind.Wait)return false;
            navigation=GetComponent<EchoNavigation>();scans=GetComponent<EchoScanController>();if(!navigation.IsBound)return false;Shutdown();player=followTarget;
            perception=new DangerPerception(GetComponent<EchoRig>(),gameObject.scene.GetPhysicsScene());dangerFilter=CanAutomaticallyWarn;warningCooldowns.Clear();clock=senseTimer=0;manualVersion=0;resumeRequest=currentRequest=null;AutomaticWarningCount=0;LastWarningTargetId=null;
            tree=new IntentBehaviorTree(navigation,new Dictionary<IntentKind,Func<IBehaviorNode>>{
                {IntentKind.Follow,()=>new FollowNode(()=>player&&player.gameObject.activeInHierarchy?player.position:(Vector3?)null)},
                {IntentKind.Investigate,()=>new InvestigateScanNode(scans.Scanner)},
                {IntentKind.Wait,()=>new WaitNode()},
                {IntentKind.Warn,()=>new WarnNode(target=>perception.CanPerceive(target,out _))}},Speak);
            dispatcher=new IntentDispatcher(WorldTarget.Active,this);initialized=true;scans.BindAutonomousMode(()=>tree?.CurrentMode);scans.Scanner.Ledger.Discovered+=OnDiscovery;
            return TrySubmit(startingMode==IntentKind.Follow&&player?ScriptCommands.Follow():ScriptCommands.Wait(),out _);
        }
        public bool TrySubmit(IntentRequest request,out IntentRejection rejection)
        {if(!initialized){rejection=IntentRejection.ExecutorRejected;return false;}return dispatcher.TryDispatch(request,out rejection);}
        public bool TryBeginBehavior(BehaviorIntent intent)
        {
            if(!initialized||tree==null)return false;
            if(intent.Kind==IntentKind.Warn&&!perception.CanPerceive(intent.Target,out _))return false;
            if(!tree.TryBeginBehavior(intent))return false;
            if(!internalRequest){manualVersion++;resumeRequest=null;}
            if(intent.Kind==IntentKind.Warn){warningCooldowns[intent.Target.Id]=clock+8;LastWarningTargetId=intent.Target.Id;}
            currentRequest=intent;warningActive=intent.Kind==IntentKind.Warn;return true;
        }
        public void Speak(string content,bool refusal){LastSpeech=content;Speech?.Invoke(content,refusal);}
        private void OnDiscovery(EchoScanReceipt receipt)=>Speak(receipt.Summary,false);
        private void Update()
        {
            if(!initialized)return;clock+=Time.deltaTime;tree.Tick(Time.deltaTime);
            if(tree.CurrentMode==null&&(tree.Status==BehaviorTreeStatus.Success||tree.Status==BehaviorTreeStatus.Failure))CompleteBehavior();
            if(!automaticWarnings||tree.CurrentMode==IntentKind.Warn)return;senseTimer-=Time.deltaTime;if(senseTimer>0)return;senseTimer=.2f;
            if(perception.TryFindNearest(out var danger,dangerFilter))
            {
                resumeRequest=currentRequest??new BehaviorIntent(IntentKind.Wait,null);resumeVersion=manualVersion;
                if(BeginInternal(new BehaviorIntent(IntentKind.Warn,danger)))
                {AutomaticWarningCount++;Speak(ScriptCommands.Warn(danger.Id).Content,false);}
                else resumeRequest=null;
            }
        }
        private bool CanAutomaticallyWarn(WorldTarget target)
        {return WorldTarget.Active.TryGet(target.Id,out var canonical)&&canonical==target&&(!warningCooldowns.TryGetValue(target.Id,out var until)||clock>=until);}
        private bool BeginInternal(BehaviorIntent intent)
        {internalRequest=true;try{return TryBeginBehavior(intent);}finally{internalRequest=false;}}
        private void CompleteBehavior()
        {
            var wasWarning=warningActive;warningActive=false;var resume=resumeRequest;resumeRequest=null;
            if(wasWarning&&resume.HasValue&&resumeVersion==manualVersion&&CanResume(resume.Value)&&BeginInternal(resume.Value))return;
            if(tree.Status==BehaviorTreeStatus.Failure&&currentRequest.HasValue&&currentRequest.Value.Kind==IntentKind.Investigate)Speak("该位置目前无法完成扫描。",false);
            BeginInternal(new BehaviorIntent(IntentKind.Wait,null));
        }
        private bool CanResume(BehaviorIntent intent)
        {
            if(intent.Kind==IntentKind.Follow)return player&&player.gameObject.activeInHierarchy;if(intent.Kind==IntentKind.Wait)return true;
            return intent.Kind==IntentKind.Investigate&&intent.Target&&intent.Target.isActiveAndEnabled&&intent.Target.GetComponent<EchoTargetPermissions>()&&intent.Target.GetComponent<EchoTargetPermissions>().CanInvestigate;
        }
        public void Shutdown()
        {
            if(!initialized)return;initialized=false;if(scans){scans.BindAutonomousMode(null);scans.Scanner.Ledger.Discovered-=OnDiscovery;}tree?.Dispose();tree=null;dispatcher=null;dangerFilter=null;resumeRequest=currentRequest=null;warningActive=false;
        }
        private void OnDisable()=>Shutdown();
    }
}
