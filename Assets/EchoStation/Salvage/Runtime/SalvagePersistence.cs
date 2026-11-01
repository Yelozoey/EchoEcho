using System;
using System.IO;
using System.Linq;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using EchoStation.Dialogue;
using EchoStation.Echo;

namespace EchoStation.Salvage
{
    [RequireComponent(typeof(SalvageMission))]
    public sealed class SalvagePersistence:MonoBehaviour
    {
        SalvageCarry carry;SalvageMotor motor;SalvageMission mission;SalvageEcho echo;PlayerFlashlight torch;SalvageDelivery delivery;
        SalvageCommandHub hub;SalvageEchoCoordinator brain;
        public SalvageSaveStore Store{get;private set;}
        void Cache(){if(carry)return;carry=GetComponent<SalvageCarry>();motor=GetComponent<SalvageMotor>();mission=GetComponent<SalvageMission>();torch=GetComponent<PlayerFlashlight>();delivery=GetComponent<SalvageDelivery>();echo=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();hub=GetComponent<SalvageCommandHub>();brain=echo.GetComponent<SalvageEchoCoordinator>();Store=new SalvageSaveStore(Path.Combine(Application.persistentDataPath,"Salvage"));}
        void Update(){var k=Keyboard.current;if(k==null)return;if(k.f5Key.wasPressedThisFrame)Save();else if(k.f9Key.wasPressedThisFrame)Load();}
        public void ConfigureTestStore(string path){Cache();Store=new SalvageSaveStore(path);}
        public SalvageSnapshot Capture()
        {
            Cache();if(delivery.Pending||mission.EndPending||mission.RestartPending)throw new InvalidOperationException("请先确认或取消当前操作。");
            return new SalvageSnapshot{Player=SalvagePose.From(transform,Mathf.DeltaAngle(0,motor.View.localEulerAngles.x)),Echo=SalvagePose.From(echo.transform),Flashlight=torch.IsOn,Warned=echo.Battery.LowWarningCount>0,Charge=echo.Battery.Charge,Elapsed=mission.Elapsed,Completed=mission.Completed,Cargo=carry.Registry.Records.OrderBy(r=>r.Definition.Id).Select(r=>new SavedCargo{Id=r.Definition.Id,State=(int)r.State,X=r.Position.x,Y=r.Position.y,Z=r.Position.z}).ToArray(),Collaboration=hub?new SavedCollaboration{BaseMode=(int)brain.BaseMode,Discovered=hub.Known.Snapshot(),Messages=hub.History.CaptureRecent().Select(SavedDialogue.From).ToArray()}:new SavedCollaboration()};
        }
        public bool Save(){Cache();try{var ok=Store.TryWrite(Capture(),out var error);mission.Notice=ok?"已保存到独立回收存档。":error;return ok;}catch(Exception e)when(e is ArgumentException||e is InvalidOperationException){mission.Notice=e.Message;return false;}}
        public bool Load(){Cache();if(!Store.TryRead(out var data,out var error)){mission.Notice=error;return false;}bool legacy=Store.LastReadVersion==1;if(!TryRestore(data,out var why)){mission.Notice=why;return false;}if(legacy){mission.Notice=Store.TryWrite(data,out var migration)?"已载入R1存档，原档已备份并升级协作数据。":"已载入R1存档；迁移未写入："+migration;}else mission.Notice=error==""?"已恢复回收存档。":error;return true;}
        public bool TryRestore(SalvageSnapshot data,out string error)
        {
            Cache();error="";try{SalvageSnapshot.Validate(data);ValidateGeometry(data);}catch(Exception e)when(e is ArgumentException||e is InvalidOperationException){error=e.Message;return false;}
            SalvageSnapshot before;try{before=Capture();}catch(InvalidOperationException e){error=e.Message;return false;}
            var originalHistory=hub?hub.History:null;
            try{Apply(data);return true;}catch(Exception e){try{Apply(before);if(hub&&originalHistory!=null)hub.ReplaceHistory(originalHistory);}catch(Exception rollback){error="恢复与回滚失败："+rollback.Message;return false;}error="恢复失败，已回滚："+e.Message;return false;}
        }
        void ValidateGeometry(SalvageSnapshot data)
        {
            if(carry.Registry.Count!=7)throw new InvalidOperationException("Scene cargo mismatch.");
            foreach(var c in data.Cargo.Where(c=>c.State==0))
            {
                var actor=carry.Actor(c.Id);var box=actor.Body;foreach(var hit in Physics.OverlapBox(c.Position()+box.center,box.size*.48f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                    if(!Ignore(hit))throw new ArgumentException("Saved cargo overlaps level geometry.");
            }
            var world=data.Cargo.Where(c=>c.State==0).ToArray();
            for(int i=0;i<world.Length;i++)for(int j=i+1;j<world.Length;j++)if(CargoBounds(world[i]).Intersects(CargoBounds(world[j])))throw new ArgumentException("Saved cargo overlap each other.");
            var p=data.Player.Position();foreach(var hit in Physics.OverlapCapsule(p+Vector3.up*.36f,p+Vector3.up*1.4f,.27f,~0,QueryTriggerInteraction.Ignore))if(!Ignore(hit))throw new ArgumentException("Saved player overlaps level geometry.");
            var playerBounds=new Bounds(p+Vector3.up*.875f,new Vector3(.54f,1.65f,.54f));if(world.Any(c=>playerBounds.Intersects(CargoBounds(c))))throw new ArgumentException("Saved player overlaps cargo.");
            var echoBox=echo.GetComponent<BoxCollider>();var echoBounds=new Bounds(data.Echo.Position()+echoBox.center,echoBox.size*.96f);
            if(playerBounds.Intersects(echoBounds)||world.Any(c=>echoBounds.Intersects(CargoBounds(c))))throw new ArgumentException("Saved Echo overlaps player or cargo.");
            foreach(var hit in Physics.OverlapBox(echoBounds.center,echoBounds.extents,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))if(!Ignore(hit))throw new ArgumentException("Saved Echo overlaps level geometry.");
            var filter=new NavMeshQueryFilter{agentTypeID=echo.Navigation.Agent.agentTypeID,areaMask=NavMesh.AllAreas};if(!NavMesh.SamplePosition(data.Echo.Position(),out var sample,.3f,filter))throw new ArgumentException("Saved Echo not on navigation surface.");
        }
        Bounds CargoBounds(SavedCargo saved){var b=carry.Actor(saved.Id).Body;return new Bounds(saved.Position()+b.center,b.size*.96f);}
        bool Ignore(Collider hit)=>hit.GetComponentInParent<SalvageDropSurface>()||hit.GetComponentInParent<CargoActor>()||hit.transform.IsChildOf(transform)||hit.transform.IsChildOf(echo.transform);
        void Apply(SalvageSnapshot data)
        {
            DialogueSession history=hub?data.Collaboration.Session():null;
            if(brain){if(Application.isPlaying&&brain.Ready)brain.ResetSession((IntentKind)data.Collaboration.BaseMode);else brain.ResetBase((IntentKind)data.Collaboration.BaseMode);}
            if(hub){hub.GetComponent<SalvageCommandPanel>()?.Close();hub.Known.RestoreValidatedIds(data.Collaboration.Discovered);hub.ReplaceHistory(history);}
            delivery.Cancel();carry.RestoreRecords(data.Cargo);motor.RestorePose(data.Player.Position(),data.Player.Yaw,data.Player.Pitch);torch.SetOn(data.Flashlight);
            if(Application.isPlaying&&echo.Initialized){echo.Navigation.Cancel();if(!echo.Navigation.TryBind(data.Echo.Position()))throw new InvalidOperationException("Echo restore binding failed.");}else echo.transform.position=data.Echo.Position();
            echo.transform.rotation=Quaternion.Euler(0,data.Echo.Yaw,0);echo.Battery.Restore(data.Charge,data.Warned);mission.RestoreProgress(data.Elapsed,data.Completed);Physics.SyncTransforms();if(hub){hub.GetComponent<SalvageDiscoveryMarkers>()?.Refresh();hub.NotifyChanged();}
        }
    }
}
