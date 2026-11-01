using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.World;
using UnityEngine;
namespace EchoStation.Salvage
{
    [DefaultExecutionOrder(40),RequireComponent(typeof(SalvageEcho))]
    public sealed class SalvageEchoCoordinator:MonoBehaviour,IIntentSink
    {
        sealed class FollowLeaf:IBehaviorNode
        {readonly SalvageEcho echo;MovementLease lease;public FollowLeaf(SalvageEcho value)=>echo=value;public void Enter(BehaviorContext c)=>lease=c.Movement;public NodeStatus Tick(float dt){if(!lease.IsValid)return NodeStatus.Failure;echo.AdvanceFollow(dt);return NodeStatus.Running;}public void Exit()=>lease?.Stop();}
        SalvageEcho echo;SalvageCommandHub hub;PlayerInputReader input;EchoPresentation presentation;IntentBehaviorTree tree;IntentDispatcher dispatcher;DangerPerception perception;
        readonly HashSet<string> warned=new HashSet<string>(StringComparer.Ordinal);string suspended;bool automaticWarning,playerDispatch;float senseTimer;SalvageMission mission;
        public bool AutomaticWarnings{get;set;}=true;public int WarningCount{get;private set;}
        public IntentKind BaseMode{get;private set;}=IntentKind.Follow;
        public IntentKind? CurrentMode=>tree?.CurrentMode;public string CurrentTargetId{get;private set;}public string LastArrivedId{get;private set;}public string LastFailure{get;private set;}
        public bool Ready=>tree!=null;public ulong Generation{get;private set;}
        public Func<WorldTarget,NodeStatus> SensorStep{private get;set;}
        public Action SensorCancel{private get;set;}
        public Action<float,bool> SensorAdvance{private get;set;}
        public Func<bool> SensorBusy{private get;set;}
        public Func<string> SensorFailure{private get;set;}
        void Start()=>Initialize();
        public bool Initialize()
        {
            if(tree!=null)return true;echo=GetComponent<SalvageEcho>();if(!echo.Initialized&&!echo.Initialize())return false;
            hub=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageCommandHub>()).Single();input=hub.GetComponent<PlayerInputReader>();presentation=GetComponent<EchoPresentation>();perception=new DangerPerception(GetComponent<EchoRig>(),gameObject.scene.GetPhysicsScene());
            var registry=new TargetRegistry();foreach(var target in hub.Targets)if(!registry.TryRegister(target.World,out _))throw new InvalidOperationException("Owned target registration failed.");
            tree=new IntentBehaviorTree(echo.Navigation,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Follow,()=>new FollowLeaf(echo)},{IntentKind.Wait,()=>new WaitNode()},{IntentKind.Investigate,()=>new SalvageInvestigationNode(Arrive)},{IntentKind.Warn,()=>new WarnNode(t=>perception.CanPerceive(t,out _))}});
            dispatcher=new IntentDispatcher(registry,this);echo.ExternalControl=true;hub.Execute=Command;hub.Cancel=CancelCurrent;mission=hub.GetComponent<SalvageMission>();mission.Restarting+=NewRun;BeginBase();return true;
        }
        public bool Command(IntentRequest request)
        {
            if(!Ready||request==null)return false;
            if(request.Kind==IntentKind.Investigate||request.Kind==IntentKind.Warn)
            {var id=request.Kind==IntentKind.Investigate?request.TargetName:request.DangerTarget;if(!hub.Context().CanSelect(id))return false;var view=hub.Targets.SingleOrDefault(t=>t.Id==id);if(!view||!view.Allows(request.Kind))return false;if(request.Kind==IntentKind.Investigate&&!hub.Known.Contains(id)&&!echo.Battery.CanScan)return false;if(request.Kind==IntentKind.Warn&&!perception.CanPerceive(view.World,out _))return false;}
            playerDispatch=true;try{return dispatcher.TryDispatch(request,out _);}finally{playerDispatch=false;}
        }
        public bool TryBeginBehavior(BehaviorIntent intent)
        {
            if(intent.Target){var view=intent.Target.GetComponent<SalvageTargetView>();if(!view||view.gameObject.scene!=gameObject.scene||!view.Allows(intent.Kind))return false;}
            if(playerDispatch){automaticWarning=false;suspended=null;}
            SensorCancel?.Invoke();Generation++;if(intent.Kind==IntentKind.Investigate){LastFailure=null;LastArrivedId=null;}
            if(intent.Kind==IntentKind.Follow||intent.Kind==IntentKind.Wait)BaseMode=intent.Kind;
            CurrentTargetId=intent.Target?intent.Target.Id:null;return tree.TryBeginBehavior(intent);
        }
        public void Speak(string content,bool refusal){} // The command hub owns the single visible acknowledgement.
        NodeStatus Arrive(WorldTarget target){LastArrivedId=target.Id;if(SensorStep!=null)return SensorStep(target);hub.Say("已到达调查位置。");return NodeStatus.Success;}
        public bool CancelCurrent(){if(!Ready)return false;automaticWarning=false;suspended=null;SensorCancel?.Invoke();Generation++;tree.Cancel();CurrentTargetId=null;BeginBase();return true;}
        void BeginBase(){dispatcher.TryDispatch(BaseMode==IntentKind.Wait?ScriptCommands.Wait():ScriptCommands.Follow(),out _);}
        void Update(){if(!Ready||!echo.enabled)return;Advance(Mathf.Min(Time.deltaTime,.1f),input.UiBlocked||!input.HasApplicationFocus||Time.timeScale==0);}
        public void Advance(float dt,bool paused)
        {
            if(!Ready)return;echo.AdvanceResources(dt,paused);if(paused){SensorAdvance?.Invoke(dt,true);echo.Navigation.Cancel();return;}SenseDanger(dt);SensorAdvance?.Invoke(dt,false);
            var mode=tree.CurrentMode;tree.Tick(dt);
            if(mode.HasValue&&!tree.CurrentMode.HasValue){if(tree.Status==BehaviorTreeStatus.Failure){LastFailure=SensorFailure?.Invoke()??"目标不可达或状态已变化。";SensorCancel?.Invoke();hub.Say(LastFailure,true);}CurrentTargetId=null;if(mode==IntentKind.Warn&&automaticWarning)ResumeAfterWarning();else BeginBase();}
            var speed=echo.Navigation.IsBound?Vector3.Dot(echo.Navigation.Agent.velocity,transform.forward):0;presentation.ApplyFrame(speed,CurrentMode==IntentKind.Warn?EchoMood.Alert:CurrentMode==IntentKind.Investigate?EchoMood.Thinking:echo.Battery.Charge<25?EchoMood.Alert:EchoMood.Calm,dt,Time.unscaledTime,SensorBusy?.Invoke()??false);
        }
        public void ResetBase(IntentKind mode){if(mode!=IntentKind.Follow&&mode!=IntentKind.Wait)throw new ArgumentException("Invalid base mode.");BaseMode=mode;CancelCurrent();}
        void SenseDanger(float dt)
        {
            if(!AutomaticWarnings||CurrentMode==IntentKind.Warn)return;senseTimer-=dt;if(senseTimer>0)return;senseTimer=.25f;
            if(!perception.TryFindNearest(out var danger,t=>!warned.Contains(t.Id)&&t.gameObject.scene==gameObject.scene&&t.GetComponent<SalvageTargetView>()&&t.GetComponent<SalvageTargetView>().Allows(IntentKind.Warn)))return;
            suspended=CurrentMode==IntentKind.Investigate?CurrentTargetId:null;automaticWarning=true;
            if(dispatcher.TryDispatch(ScriptCommands.Warn(danger.Id),out _)){warned.Add(danger.Id);WarningCount++;hub.Say("警告："+SalvageTargetCatalog.Items.Single(t=>t.Id==danger.Id).Summary);}else{automaticWarning=false;suspended=null;}
        }
        void ResumeAfterWarning()
        {
            var id=suspended;suspended=null;automaticWarning=false;
            var target=id==null?null:hub.Targets.SingleOrDefault(t=>t.Id==id);
            if(target&&target.Allows(IntentKind.Investigate)&&(hub.Known.Contains(id)||echo.Battery.CanScan)&&dispatcher.TryDispatch(ScriptCommands.Investigate(id),out _))return;
            BeginBase();
        }
        public void ResetSession(IntentKind mode){warned.Clear();WarningCount=0;senseTimer=0;ResetBase(mode);GetComponent<SalvageScanner>()?.ResetSensor();hub.GetComponent<SalvageCommandPanel>()?.Close();}
        void NewRun(){ResetSession(IntentKind.Follow);hub.Known.RestoreValidatedIds(Array.Empty<string>());hub.ReplaceHistory(new EchoStation.Dialogue.DialogueSession());hub.NotifyChanged();}
        void OnDisable(){if(mission)mission.Restarting-=NewRun;SensorCancel?.Invoke();tree?.Dispose();tree=null;if(echo)echo.ExternalControl=false;if(hub){hub.Execute=null;hub.Cancel=null;}Generation++;}
    }
}
