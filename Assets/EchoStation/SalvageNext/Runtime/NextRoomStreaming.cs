using System;
using System.Collections.Generic;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage;
using UnityEngine;
using UnityEngine.UI;
namespace EchoStation.SalvageNext
{
    [DefaultExecutionOrder(-60),DisallowMultipleComponent]
    public sealed class NextRoomStreaming:MonoBehaviour
    {
        [SerializeField]SalvageEcho echo;
        [SerializeField]string addressPrefix="es-next-room-";
        readonly bool[] retained=new bool[3];readonly Dictionary<string,SalvageTargetView> targets=new Dictionary<string,SalvageTargetView>(StringComparer.Ordinal);
        readonly string[] loading={"正在载入回收泊位…","正在载入储藏区…","正在载入检修间…"};
        NextSession session;SalvageMotor motor;PlayerInputReader input;SalvageEchoCoordinator brain;RoomDemandPlanner policy;RoomSceneOwner owner;RoomPresenter presenter;StreamInputLease inputLease;Text status;GameObject loadingCanvas;float nextEvaluation;
        public RoomSceneOwner Owner=>owner;public RoomDemandPlanner Policy=>policy;
        public bool Blocking=>inputLease!=null;public int PlayerRoom=>Mathf.Clamp(Mathf.FloorToInt(transform.position.x/12),0,2);
        public RoomPackage Room(int index)=>presenter?.Room(index);
        public void Configure(SalvageEcho robot,string prefix="es-next-room-"){if(!robot||robot.gameObject.scene!=gameObject.scene||string.IsNullOrWhiteSpace(prefix))throw new ArgumentException("Owned Echo and address prefix required.");echo=robot;addressPrefix=prefix;}
        void OnEnable()
        {
            if(!Application.isPlaying)return;if(!echo)throw new InvalidOperationException("Streaming Echo binding missing.");session=GetComponent<NextSession>();motor=GetComponent<SalvageMotor>();input=GetComponent<PlayerInputReader>();brain=echo.GetComponent<SalvageEchoCoordinator>();var hub=GetComponent<SalvageCommandHub>();if(!session||!motor||!input||!brain||!hub)throw new InvalidOperationException("Streaming player services missing.");
            owner=null;Array.Clear(retained,0,retained.Length);targets.Clear();foreach(var target in hub.Targets)targets.Add(target.Id,target);policy=new RoomDemandPlanner();presenter=new RoomPresenter(motor.View.GetComponent<Camera>());RefreshDemand();owner=RoomSceneOwner.Create(gameObject,policy,presenter.Show,presenter.Hide,addressPrefix);
            status=SalvageUi.Panel(transform,"NextRoomLoading",740,160,95);status.alignment=TextAnchor.MiddleCenter;loadingCanvas=SalvageUi.CanvasOf(status);session.Restored+=Restored;Gate();
        }
        static StreamPoint Point(Vector3 p)=>new StreamPoint(p.x,p.y,p.z);
        void RefreshDemand()
        {
            if(owner!=null)for(int i=0;i<3;i++)retained[i]=owner.Retained(i);ulong world=checked((ulong)session.Epoch),generation=brain.Generation;StreamTaskLease? task=null;
            var mode=brain.CurrentMode;string id=brain.CurrentTargetId;if(id!=null&&mode.HasValue&&(mode==IntentKind.Investigate||mode==IntentKind.Warn)&&targets.TryGetValue(id,out var target)&&target&&target.Allows(mode.Value))task=new StreamTaskLease(world,generation,Point(target.transform.position));
            policy.Refresh(world,generation,Point(transform.position),Point(echo.transform.position),task,retained);nextEvaluation=Time.unscaledTime+.1f;
        }
        public void EvaluateNow(){if(owner==null||owner.IsDisposed)return;RefreshDemand();Gate();}
        void Restored()=>EvaluateNow();
        void Gate()
        {
            bool missing=!owner.IsReady(PlayerRoom);if(missing){if(inputLease==null)inputLease=new StreamInputLease(input);if(status)status.text=owner.LastError==null?loading[PlayerRoom]:"场景资源读取失败，正在重试…";}
            else if(inputLease!=null){inputLease.Dispose();inputLease=null;}
            if(loadingCanvas&&loadingCanvas.activeSelf!=missing)loadingCanvas.SetActive(missing);
        }
        void Update(){if(owner==null||owner.IsDisposed)return;if(Time.unscaledTime>=nextEvaluation)RefreshDemand();Gate();}
        void OnDisable()
        {
            if(session)session.Restored-=Restored;owner?.Dispose();presenter?.Dispose();presenter=null;inputLease?.Dispose();inputLease=null;if(loadingCanvas)Destroy(loadingCanvas);loadingCanvas=null;status=null;targets.Clear();
        }
    }
}
