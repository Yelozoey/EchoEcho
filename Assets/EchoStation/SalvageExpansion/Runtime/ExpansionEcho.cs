using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Rendering;
using EchoStation.Salvage;
using EchoStation.World;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace EchoStation.SalvageExpansion
{
    /// <summary>Script intent adapter. Reports actual read-only state; it has no device mutation port.</summary>
    [DefaultExecutionOrder(50)]
    public sealed class ExpansionEcho:MonoBehaviour,IIntentSink,IExpansionSaveSection
    {
        sealed class ScanNode:IBehaviorNode
        {
            readonly ExpansionEcho owner;MovementLease lease;WorldTarget target;float timer;
            public ScanNode(ExpansionEcho value){owner=value;}
            public void Enter(BehaviorContext context){lease=context.Movement;target=context.Intent.Target;timer=0;var p=target.transform.position;p.y=owner.Robot.position.y;var side=(owner.Robot.position-p);side.y=0;if(side.sqrMagnitude<.01f)side=Vector3.left;lease.TryMove(p+side.normalized*1.1f);}
            public NodeStatus Tick(float seconds)
            {
                if(!lease.IsValid||!target||!target.isActiveAndEnabled||!owner.Owns(target))return NodeStatus.Failure;
                var delta=target.transform.position-owner.Robot.position;delta.y=0;if(delta.magnitude>1.7f)return lease.State==NavigationState.Unreachable?NodeStatus.Failure:NodeStatus.Running;
                lease.Stop();timer+=seconds;if(timer<1)return NodeStatus.Running;return owner.CommitScan(target)?NodeStatus.Success:NodeStatus.Failure;
            }
            public void Exit(){lease?.Stop();}
        }
        [SerializeField]EchoNavigation navigation;[SerializeField]WorldTarget[] targets;
        ExpansionWorld world;FacilityController facility;EnvironmentHazards hazards;ExpansionPersistence persistence;EchoPresentation presentation;IntentBehaviorTree tree;IntentDispatcher dispatcher;IDisposable menuLease;Text panel;RectTransform buttons;GameObject menu;ScanWaveEmitter wave;
        readonly HashSet<string> discovered=new HashSet<string>(StringComparer.Ordinal);readonly Dictionary<string,string> reports=new Dictionary<string,string>(StringComparer.Ordinal),stamps=new Dictionary<string,string>(StringComparer.Ordinal);readonly List<string> history=new List<string>();readonly Dictionary<string,TextMesh> markers=new Dictionary<string,TextMesh>(StringComparer.Ordinal);
        IntentKind baseMode=IntentKind.Follow;float senseAt,warningAt;bool initialized,recapture;string targetId;
        readonly RaycastHit[] sightHits=new RaycastHit[64];
        public string Key=>"echo";public Transform Robot=>navigation.transform;public EchoNavigation Navigation=>navigation;public EchoBattery Battery{get;}=new EchoBattery();public ulong Generation{get;private set;}public string CurrentTarget=>targetId;public IntentKind Mode=>tree?.CurrentMode??baseMode;public int DiscoveredCount=>discovered.Count;public bool MenuOpen=>menu&&menu.activeSelf;public string LastSpeech{get;private set;}="脚本协作在线。R 查看Echo。";
        public void Configure(EchoNavigation robot,WorldTarget[] owned){navigation=robot;targets=owned;}
        public event Action TextCommunicationRequested;
        public WorldTarget[] KnownTargetsSnapshot()=>targets==null?Array.Empty<WorldTarget>():targets.Where(t=>t&&discovered.Contains(t.Id)).ToArray();
        public bool TryReadKnownReport(string id,out string summary,out bool stale)
        {stale=false;if(!reports.TryGetValue(id??"",out summary))return false;stale=stamps[id]!=Stamp(Find(id));return true;}
        void Start()=>Initialize();
        public void Initialize()
        {
            if(initialized)return;world=GetComponent<ExpansionWorld>();facility=GetComponent<FacilityController>();hazards=GetComponent<EnvironmentHazards>();persistence=GetComponent<ExpansionPersistence>();if(!world||!facility||!hazards||!navigation||targets==null||targets.Any(t=>!t)||targets.Select(t=>t.Id).Distinct().Count()!=targets.Length)throw new InvalidOperationException("Echo bindings required.");world.Initialize();facility.Initialize();hazards.Initialize();if(!navigation.TryBind(navigation.transform.position))throw new InvalidOperationException("Echo navigation not bound.");presentation=navigation.GetComponent<EchoPresentation>();if(presentation)presentation.enabled=false;wave=navigation.GetComponent<ScanWaveEmitter>();
            tree=new IntentBehaviorTree(navigation,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Follow,()=>new ExpansionFollowNode(world)},{IntentKind.Wait,()=>new WaitNode()},{IntentKind.Investigate,()=>new ScanNode(this)},{IntentKind.Warn,()=>new WaitNode()}},Speak);dispatcher=new IntentDispatcher(WorldTarget.Active,this);persistence.Register(this);persistence.Restored+=Restored;world.Pause.Configure(world.Input,new Behaviour[]{world.Motor},new[]{navigation.Agent});GameRuntimeOptions.UseLLM=false;initialized=true;Base();
        }
        bool Owns(WorldTarget t)=>t&&Array.IndexOf(targets,t)>=0;
        WorldTarget Find(string id)=>targets.FirstOrDefault(t=>t&&t.Id==id);
        public bool Discover(WorldTarget target)
        {
            if(!Owns(target)||!target.isActiveAndEnabled)return false;var d=target.transform.position-world.Motor.View.position;if(d.magnitude>12||Vector3.Angle(world.Motor.View.forward,d)>65)return false;
            int count=Physics.RaycastNonAlloc(world.Motor.View.position,d.normalized,sightHits,d.magnitude,~0,QueryTriggerInteraction.Ignore);if(count==sightHits.Length)return false;Collider nearest=null;float distance=float.PositiveInfinity;for(int i=0;i<count;i++){var hit=sightHits[i];if(hit.collider.transform.IsChildOf(world.transform)||hit.collider.transform.IsChildOf(Robot))continue;if(hit.distance<distance){nearest=hit.collider;distance=hit.distance;}}if(nearest&&!nearest.transform.IsChildOf(target.transform)&&!target.transform.IsChildOf(nearest.transform))return false;return discovered.Add(target.Id);
        }
        public bool Command(IntentRequest request)
        {
            if(!initialized||!world.ActionsAllowed||world.Pause.Blocked||!world.Input.HasApplicationFocus){world.Say("Echo命令被当前输入或任务状态阻断。");return false;}
            if(request?.Kind==IntentKind.Investigate){var t=Find(request.TargetName);if(!t||!discovered.Contains(t.Id)||!t.isActiveAndEnabled){Speak("该目标尚未发现，或已不在原处。",true);return false;}}
            if(request?.Kind==IntentKind.Answer||request?.Kind==IntentKind.Refuse)return false;bool accepted=dispatcher.TryDispatch(request,out _);if(!accepted)world.Say("Echo：当前无法执行此指令。");return accepted;
        }
        public bool TryBeginBehavior(BehaviorIntent intent)
        {
            if(intent.Kind==IntentKind.Investigate&&(!Owns(intent.Target)||!discovered.Contains(intent.Target.Id)))return false;
            if(intent.Kind==IntentKind.Follow||intent.Kind==IntentKind.Wait)baseMode=intent.Kind;Generation++;targetId=intent.Target?intent.Target.Id:null;return tree.TryBeginBehavior(intent);
        }
        void Base(){targetId=null;dispatcher.TryDispatch(baseMode==IntentKind.Follow?ScriptCommands.Follow():ScriptCommands.Wait(),out _);}
        public void CancelTask(){Generation++;tree?.Cancel();navigation.Cancel();if(initialized)Base();}
        public string Report(string id)
        {if(!reports.TryGetValue(id??"",out var text))return "尚无扫描信息。";return text+(stamps[id]==Stamp(Find(id))?"":"（已过时，请重新扫描）");}
        string Stamp(WorldTarget t)
        {
            if(!t)return "missing";var d=t.GetComponent<FacilityDevice>();if(d){switch(d.Kind){case FacilityDeviceKind.FanSocket:return facility.FanPowered+":"+facility.BatterySlot;case FacilityDeviceKind.DataSocket:return facility.DataReady+":"+facility.DataPowered;case FacilityDeviceKind.Valve:return facility.ValveClosed.ToString();case FacilityDeviceKind.Isolator:return facility.Isolated.ToString();default:return facility.Isolated+":"+facility.Clamps;}}
            var z=t.GetComponent<HazardZone>();if(z)return hazards.Signal(z).ToString();var item=t.GetComponent<ExpansionItemActor>();return item?world.Inventory.Get(item.Id).Location.ToString():"fixed";
        }
        string Summary(WorldTarget t)
        {
            var d=t.GetComponent<FacilityDevice>();if(d){switch(d.Kind){case FacilityDeviceKind.FanSocket:return facility.FanPowered?"通风已供电，蒸汽受到抑制。":"通风未供电。便携电池可暂时抑制蒸汽。";case FacilityDeviceKind.DataSocket:return facility.DataReady?"数据校验完成。取核心可保留完整归档评价。":"接入便携电池可校验数据。校验是可选目标。";case FacilityDeviceKind.Valve:return facility.ValveClosed?"阀门已关闭，蒸汽停止。":"扳手可关闭阀门；操作期间危险仍运行。";case FacilityDeviceKind.Isolator:return facility.Isolated?"主供电已隔离，漏电区安全。":"主供电尚未隔离。先装备绝缘工具处理此接口。";default:return facility.Isolated?"断电已确认。用扳手逐个解除三个夹具。":"设备仍带电，先隔离供电，再处理夹具。";}}
            var z=t.GetComponent<HazardZone>();if(z)return z.Kind==EnvironmentHazardKind.Steam?"蒸汽按预警、喷发、安全周期运行；可绕行、供风或关闭阀门。":"漏电区域会快速增加防护压力；隔离器在危险区外。";var item=t.GetComponent<ExpansionItemActor>();return item?world.Inventory.Definition(item.Id).Name+"，占格 "+world.Inventory.Definition(item.Id).Footprint.Cells.Count+"。收纳前预留空间。":"没有可用扫描数据。";
        }
        bool CommitScan(WorldTarget t)
        {
            if(!Owns(t)||!discovered.Contains(t.Id)||!world.ActionsAllowed)return false;bool first=!reports.ContainsKey(t.Id);if(first&&!Battery.SpendSuccessfulScan()){Speak("扫描电量不足。可返回泊位充电。",false);return false;}reports[t.Id]=Summary(t);stamps[t.Id]=Stamp(t);Speak(reports[t.Id],false);if(wave)wave.State.Begin(navigation.transform.position);Mark(t);return true;
        }
        void Mark(WorldTarget t){if(!t||markers.ContainsKey(t.Id))return;var go=new GameObject("EchoKnownMarker");go.transform.SetParent(t.transform,false);go.transform.localPosition=Vector3.up*1.2f;var text=go.AddComponent<TextMesh>();text.font=EchoStation.Player.RuntimeUiFont.Get();text.GetComponent<Renderer>().sharedMaterial=text.font.material;text.characterSize=.025f;text.fontSize=32;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.3f,.85f,1);text.text="◇ "+t.DisplayName;markers.Add(t.Id,text);}
        public void Speak(string content,bool refusal){LastSpeech=content??"";history.Add(LastSpeech);if(history.Count>50)history.RemoveAt(0);world.Say("Echo："+LastSpeech);}
        public void Advance(float seconds,bool paused)
        {
            if(!initialized||paused||!world.ActionsAllowed)return;Battery.Step(seconds,navigation.IsBound&&navigation.Agent.velocity.sqrMagnitude>.01f,Vector3.Distance(Robot.position,ExpansionWorld.Dock)<2.5f,false);navigation.Agent.speed=Battery.FollowSpeed;tree.Tick(seconds);if(tree.CurrentMode==null){if(tree.Status==BehaviorTreeStatus.Failure)Speak("目标目前无法完成扫描。",false);Base();}
            if(Time.unscaledTime>=senseAt){senseAt=Time.unscaledTime+.25f;foreach(var t in targets)if(t&&t.isActiveAndEnabled)Discover(t);}
            if(hazards.RateAt(world.transform.position)>0&&Time.unscaledTime>=warningAt){warningAt=Time.unscaledTime+5;Speak("警告：正在暴露于危险，请退出红色区域。",false);}
            if(presentation)presentation.ApplyFrame(navigation.IsBound?Vector3.Dot(navigation.Agent.velocity,Robot.forward):0,hazards.RateAt(world.transform.position)>0?EchoMood.Alert:Mode==IntentKind.Investigate?EchoMood.Thinking:EchoMood.Calm,seconds,Time.unscaledTime,false);
            foreach(var text in markers.Values)if(text)text.transform.rotation=world.Motor.View.rotation;
        }
        public bool OpenMenu()
        {
            if(!initialized||world.Pause.Blocked||!world.ActionsAllowed||!world.Input.HasApplicationFocus)return false;menuLease=world.Pause.Acquire();world.Input.SetPointerCaptured(false);
            if(!panel){panel=SalvageUi.Panel(transform,"ExpansionEchoPanel",1120,900,121);menu=SalvageUi.CanvasOf(panel);menu.AddComponent<GraphicRaycaster>();panel.gameObject.SetActive(false);buttons=(RectTransform)panel.transform.parent;}
            foreach(Transform child in buttons)if(child!=panel.transform)Destroy(child.gameObject);
            SalvageWidgets.Label(SalvageWidgets.Rect("Title",buttons,28,20,1060,70),"Echo · 脚本信息协作 · 电量 "+Mathf.RoundToInt(Battery.Charge)+"%\n仅提供信息，设备由玩家操作。",24);
            Add("跟随",28,104,()=>Command(ScriptCommands.Follow()));Add("待命",285,104,()=>Command(ScriptCommands.Wait()));Add("取消调查",542,104,()=>{CancelTask();return true;});Add("关闭 [R/Esc]",799,104,()=>true);
            if(TextCommunicationRequested!=null)Add("文字通信 [T]",799,34,()=>{TextCommunicationRequested?.Invoke();return true;});
            int row=0;foreach(var t in targets.Where(t=>t&&t.isActiveAndEnabled&&discovered.Contains(t.Id))){var target=t;float y=166+row++*49;SalvageWidgets.Button(buttons,"Scan_"+t.Id,"调查 "+t.DisplayName,28,y,230,()=>{CloseMenu();Command(ScriptCommands.Investigate(target.Id));});SalvageWidgets.Label(SalvageWidgets.Rect("Report_"+t.Id,buttons,276,y+5,810,44),Report(t.Id),18);}
            if(row==0)SalvageWidgets.Label(SalvageWidgets.Rect("Empty",buttons,28,190,1050,90),"尚未发现目标。靠近设备并看向它，再打开R面板。",24);menu.SetActive(true);return true;
        }
        void Add(string text,float x,float y,Func<bool> command)=>SalvageWidgets.Button(buttons,text,text,x,y,235,()=>{CloseMenu();command();});
        public void CloseMenu(){if(menu)menu.SetActive(false);menuLease?.Dispose();menuLease=null;recapture=true;}
        void Restored(){CloseMenu();CancelTask();foreach(var marker in markers.Values)if(marker)Destroy(marker.gameObject);markers.Clear();foreach(var id in reports.Keys)Mark(Find(id));}
        void Update(){if(!initialized)return;if(MenuOpen){if(!world.Input.HasApplicationFocus||Keyboard.current!=null&&(Keyboard.current.rKey.wasPressedThisFrame||Keyboard.current.escapeKey.wasPressedThisFrame))CloseMenu();}else if(Keyboard.current!=null&&Keyboard.current.rKey.wasPressedThisFrame&&world.Input.CanPlay)OpenMenu();Advance(Mathf.Min(Time.deltaTime,.1f),world.Pause.Paused||!world.Input.HasApplicationFocus);}
        void LateUpdate(){if(recapture){recapture=false;if(world)world.Pause.RestorePointer();}}
        public JToken Capture()=>new JObject{{"base",baseMode==IntentKind.Follow?"follow":"wait"},{"charge",Battery.Charge},{"warned",Battery.LowWarningCount>0},{"position",new JArray(Robot.position.x,Robot.position.y,Robot.position.z)},{"discovered",new JArray(discovered.OrderBy(s=>s,StringComparer.Ordinal))},{"reports",new JObject(reports.Select(p=>new JProperty(p.Key,new JObject{{"text",p.Value},{"stamp",stamps[p.Key]}})))},{"history",new JArray(history)}};
        public bool TryPrepare(ExpansionSnapshot snapshot,JToken value,out Action apply,out Action rollback,out string error)
        {
            apply=rollback=null;error="";try{var v=SaveSectionJson.Fields(value,"base","charge","warned","position","discovered","reports","history");if(v["base"].Type!=JTokenType.String||((string)v["base"]!="follow"&&(string)v["base"]!="wait"))throw new ArgumentException("Invalid Echo base.");float charge=SaveSectionJson.Number(v["charge"],0,100);bool warned=SaveSectionJson.Boolean(v["warned"]);if(charge<25&&!warned)throw new ArgumentException("Battery warning inconsistent.");if(!(v["position"] is JArray pos)||pos.Count!=3)throw new ArgumentException("Robot position required.");var point=new Vector3(SaveSectionJson.Number(pos[0],0,36),SaveSectionJson.Number(pos[1],-.1f,3),SaveSectionJson.Number(pos[2],0,24));if(!UnityEngine.AI.NavMesh.SamplePosition(point,out _,.3f,navigation.Agent.areaMask))throw new ArgumentException("Robot position outside navigation.");if(!(v["discovered"] is JArray ds)||ds.Count>targets.Length||ds.Any(t=>t.Type!=JTokenType.String||!Find((string)t))||ds.Select(t=>(string)t).Distinct().Count()!=ds.Count)throw new ArgumentException("Invalid discoveries.");if(!(v["reports"] is JObject rs)||rs.Properties().Any(p=>!ds.Any(d=>(string)d==p.Name)))throw new ArgumentException("Invalid reports.");foreach(var p in rs.Properties()){var report=SaveSectionJson.Fields(p.Value,"text","stamp");if(report.Properties().Any(f=>f.Value.Type!=JTokenType.String||((string)f.Value).Length>280))throw new ArgumentException("Report too long.");}if(!(v["history"] is JArray hs)||hs.Count>50||hs.Any(t=>t.Type!=JTokenType.String||((string)t).Length>280))throw new ArgumentException("Invalid history.");var before=(JObject)Capture();var frozen=(JObject)v.DeepClone();apply=()=>Apply(frozen);rollback=()=>Apply(before);return true;}catch(Exception e)when(e is ArgumentException||e is OverflowException){error=e.Message;return false;}
        }
        void Apply(JObject v){baseMode=(string)v["base"]=="follow"?IntentKind.Follow:IntentKind.Wait;Battery.Restore((float)v["charge"],(bool)v["warned"]);var p0=v["position"];if(!navigation.TryBind(new Vector3((float)p0[0],(float)p0[1],(float)p0[2])))throw new InvalidOperationException("Robot restore failed.");discovered.Clear();foreach(var t in (JArray)v["discovered"])discovered.Add((string)t);reports.Clear();stamps.Clear();foreach(var p in ((JObject)v["reports"]).Properties()){reports.Add(p.Name,(string)p.Value["text"]);stamps.Add(p.Name,(string)p.Value["stamp"]);}history.Clear();history.AddRange(((JArray)v["history"]).Select(t=>(string)t));LastSpeech=history.LastOrDefault()??"脚本协作在线。";}
        void OnDisable(){CloseMenu();tree?.Dispose();if(navigation)navigation.Cancel();}
        void OnDestroy(){if(persistence)persistence.Restored-=Restored;}
    }
}
