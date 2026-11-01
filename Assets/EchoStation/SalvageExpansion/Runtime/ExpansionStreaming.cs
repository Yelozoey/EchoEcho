using System;
using EchoStation.Salvage;
using EchoStation.SalvageNext;
using UnityEngine;
using UnityEngine.UI;
namespace EchoStation.SalvageExpansion
{
    [DefaultExecutionOrder(75)]
    public sealed class ExpansionStreaming:MonoBehaviour
    {
        ExpansionWorld world;ExpansionEcho echo;ExpansionPersistence persistence;RoomDemandPlanner policy;RoomPresenter presenter;RoomSceneOwner owner;readonly bool[] retained=new bool[3];IDisposable lease;Text loading;float next;
        public RoomSceneOwner Owner=>owner;public bool Blocking=>lease!=null;public int PlayerRoom=>Mathf.Clamp((int)(transform.position.x/12),0,2);public RoomPackage Room(int index)=>presenter?.Room(index);
        void Start(){world=GetComponent<ExpansionWorld>();echo=GetComponent<ExpansionEcho>();persistence=GetComponent<ExpansionPersistence>();echo.Initialize();policy=new RoomDemandPlanner();presenter=new RoomPresenter(world.Motor.View.GetComponent<Camera>());Refresh();owner=RoomSceneOwner.Create(gameObject,policy,presenter.Show,presenter.Hide,"es-v2-room-");loading=SalvageUi.Panel(transform,"ExpansionLoading",700,140,140);EvaluateNow();persistence.Restored+=EvaluateNow;}
        static StreamPoint Point(Vector3 p)=>new StreamPoint(p.x,p.y,p.z);
        void Refresh(){if(owner!=null)for(int i=0;i<3;i++)retained[i]=owner.Retained(i);StreamTaskLease? task=null;if(echo.CurrentTarget!=null&&EchoStation.World.WorldTarget.Active.TryGet(echo.CurrentTarget,out var t)&&t)task=new StreamTaskLease((ulong)persistence.Epoch,echo.Generation,Point(t.transform.position));policy.Refresh((ulong)persistence.Epoch,echo.Generation,Point(transform.position),Point(echo.Robot.position),task,retained);next=Time.unscaledTime+.1f;}
        public void EvaluateNow(){if(owner==null||owner.IsDisposed)return;Refresh();bool missing=!owner.IsReady(PlayerRoom);if(missing&&lease==null)lease=world.Pause.Acquire();else if(!missing&&lease!=null){lease.Dispose();lease=null;}if(loading){loading.text=owner.LastError==null?"正在载入设施区块…":"区块读取失败，正在重试…";SalvageUi.CanvasOf(loading).SetActive(missing);}}
        void Update(){if(owner!=null&&Time.unscaledTime>=next)EvaluateNow();}
        void OnDisable(){if(persistence)persistence.Restored-=EvaluateNow;owner?.Dispose();presenter?.Dispose();lease?.Dispose();lease=null;if(loading)Destroy(SalvageUi.CanvasOf(loading));}
    }
}
