using System;
using System.Linq;
using EchoStation.Echo;
using EchoStation.World;
using UnityEngine;
namespace EchoStation.Salvage
{
    [DefaultExecutionOrder(60),RequireComponent(typeof(SalvageEchoCoordinator))]
    public sealed class SalvageScanner:MonoBehaviour
    {
        SalvageEcho echo;SalvageEchoCoordinator brain;SalvageCommandHub hub;EchoScanEngine engine;WorldTarget active;ulong requestGeneration,sensorGeneration;bool committed,failed,begun;
        public EchoScanEngine Engine=>engine;public bool Busy=>engine!=null&&engine.IsBusy;public int PaidDiscoveries{get;private set;}public string LastError{get;private set;}
        public event Action<ScanPhase> PhaseChanged;public event Action<string> Discovered;
        void Start()=>Initialize();
        public void Initialize()
        {
            if(engine!=null)return;echo=GetComponent<SalvageEcho>();brain=GetComponent<SalvageEchoCoordinator>();hub=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageCommandHub>()).Single();NewEngine();brain.SensorStep=Step;brain.SensorCancel=Cancel;brain.SensorAdvance=Advance;brain.SensorBusy=()=>Busy;brain.SensorFailure=()=>LastError;
        }
        void NewEngine(){engine=new EchoScanEngine(GetComponent<EchoRig>(),gameObject.scene.GetPhysicsScene());engine.PhaseChanged+=p=>PhaseChanged?.Invoke(p);}
        bool ValidActive()=>active&&active.gameObject.scene==gameObject.scene&&active.GetComponent<SalvageTargetView>()&&active.GetComponent<SalvageTargetView>().Allows(IntentKind.Investigate)&&brain.Generation==requestGeneration&&brain.CurrentTargetId==active.Id;
        NodeStatus Step(WorldTarget target)
        {
            if(hub.Known.Contains(target.Id)){hub.Say(hub.Known.Summary(target.Id));return NodeStatus.Success;}
            if(failed&&active==target)return NodeStatus.Failure;
            if(active!=target)
            {
                Cancel();active=target;requestGeneration=brain.Generation;committed=failed=false;LastError=null;
                if(!ValidActive()||!echo.Battery.CanScan)return Fail("目标状态已变化或电量不足。");
            }
            if(committed)return NodeStatus.Success;
            if(engine.IsBusy)return NodeStatus.Running;
            if(engine.CooldownRemaining>0)return NodeStatus.Running;
            if(!engine.TryBegin(target))return Fail(Reason(engine.LastRequestFailure));
            begun=true;sensorGeneration=engine.Generation;
            return NodeStatus.Running;
        }
        public void Advance(float dt,bool paused)
        {
            if(engine==null||paused)return;
            if(engine.IsBusy&&(!ValidActive()||!echo.Battery.CanScan)){engine.Cancel();failed=true;LastError="扫描被取消：目标状态或电量已变化。";return;}
            engine.Tick(dt);
            if(begun&&engine.Generation==sensorGeneration&&engine.Phase==ScanPhase.Failed){failed=true;LastError=Reason(engine.Failure);}
            if(begun&&engine.Generation==sensorGeneration&&!committed&&!failed&&active&&engine.Phase==ScanPhase.Completed)
            {
                var receipt=engine.LastReceipt;
                if(!ValidActive()||receipt==null||receipt.TargetId!=active.Id||!SalvageTargetCatalog.TryGet(receipt.TargetId,out var definition)||receipt.Summary!=definition.Summary){Fail("扫描结果验证失败。");return;}
                if(!hub.Known.Contains(receipt.TargetId))
                {
                    if(!echo.Battery.CanScan){Fail("电量不足，扫描记录未提交。");return;}
                    // All validation precedes this main-thread transaction. No callbacks run between charge and index commit.
                    if(!echo.Battery.SpendSuccessfulScan())throw new InvalidOperationException("Validated scan charge changed.");
                    if(!hub.Known.CommitValidatedSensor(receipt.TargetId))throw new InvalidOperationException("Validated discovery commit changed.");
                    PaidDiscoveries++;committed=true;hub.Say(definition.Name+"："+receipt.Summary);Discovered?.Invoke(receipt.TargetId);hub.NotifyChanged();
                }
                else committed=true;
            }
        }
        NodeStatus Fail(string message){failed=true;LastError=message;return NodeStatus.Failure;}
        static string Reason(ScanFailure reason)=>reason==ScanFailure.Obstructed?"扫描视线被遮挡，请让开目标附近位置。":reason==ScanFailure.OutOfRange?"目标超出扫描范围。":reason==ScanFailure.OutOfView?"目标不在扫描视角内。":"扫描条件不满足，请重新指向目标。";
        public void Cancel(){engine?.Cancel();active=null;committed=failed=begun=false;requestGeneration=sensorGeneration=0;LastError=null;}
        public void ResetSensor(){Cancel();NewEngine();PaidDiscoveries=0;LastError=null;}
        void OnDisable(){Cancel();if(brain){brain.SensorStep=null;brain.SensorCancel=null;brain.SensorAdvance=null;brain.SensorBusy=null;brain.SensorFailure=null;}}
    }
}
