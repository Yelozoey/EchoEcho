using System;
using System.Collections.Generic;
using EchoStation.Echo;
using EchoStation.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace EchoStation.Player
{
    [RequireComponent(typeof(FirstPersonMotor),typeof(PlayerJournal),typeof(PlayerFlashlight)),DisallowMultipleComponent]
    public sealed class SaveGameAdapter:MonoBehaviour
    {
        [SerializeField]private EchoBrain echo;
        private readonly Collider[] overlaps=new Collider[32];private readonly RaycastHit[] hits=new RaycastHit[16];
        private sealed class Plan{public SavePayload data;public JournalEntry[] entries;public WorldTarget[] scans,allTargets;}
        public EchoBrain Echo=>echo;
        public void Configure(EchoBrain brain){if(!brain||brain.gameObject.scene!=gameObject.scene)throw new ArgumentException("Save actors must belong to one loaded cabin scene.");echo=brain;GetComponent<PlayerJournal>().BindEcho(echo.GetComponent<EchoScanController>());}
        public bool CanRestore(SavePayload data,out string error)=>Prepare(data,out _,out error);
        public SavePayload Capture()
        {
            if(!echo||!echo.IsInitialized)throw new InvalidOperationException("Echo save binding not ready.");var motor=GetComponent<FirstPersonMotor>();var entries=GetComponent<PlayerJournal>().Store.Entries;var ids=new string[entries.Count];for(var i=0;i<ids.Length;i++)ids[i]=entries[i].Id;var scans=echo.GetComponent<EchoScanController>().Scanner.Ledger.GetDiscoveredIds();Array.Sort(scans,StringComparer.Ordinal);return new SavePayload{sceneId="cabin_s1",playerPosition=transform.position,playerYaw=motor.Yaw,playerPitch=motor.Pitch,flashlightOn=GetComponent<PlayerFlashlight>().IsOn,echoPosition=echo.transform.position,echoYaw=echo.transform.eulerAngles.y,echoMode=echo.CurrentMode==IntentKind.Follow?"follow":"wait",journalIds=ids,scanIds=scans};
        }
        public bool TrySave(LocalSaveStore store,out string error)
        {var data=Capture();return Prepare(data,out _,out error)&&store.TryWrite(data,out error);}
        public bool TryLoad(LocalSaveStore store,out string message)
        {var read=store.Read();message=read.Detail;if(!read.Success)return false;if(!TryRestore(read.Data,out var error)){message=error;return false;}if(read.Status==SaveStatus.Loaded)message="存档已恢复。";return true;}
        public bool TryRestore(SavePayload data,out string error)
        {
            if(!Prepare(data,out var plan,out error))return false;var navigation=echo.GetComponent<EchoNavigation>();var oldPosition=echo.transform.position;if(!navigation.TryBind(plan.data.echoPosition)){navigation.TryBind(oldPosition);error="Echo导航恢复失败。";return false;}
            echo.Shutdown();var journal=GetComponent<PlayerJournal>();var subscribed=journal.enabled;journal.enabled=false;
            try
            {
                EchoPersistence.Restore(echo.GetComponent<EchoScanController>(),plan.allTargets,plan.scans);journal.Store.Replace(plan.entries);var controller=GetComponent<CharacterController>();var enabled=controller.enabled;controller.enabled=false;transform.position=plan.data.playerPosition;var motor=GetComponent<FirstPersonMotor>();motor.SetLookAngles(plan.data.playerYaw,plan.data.playerPitch);motor.ResetMotion();controller.enabled=enabled;GetComponent<PlayerFlashlight>().SetOn(plan.data.flashlightOn);echo.transform.rotation=Quaternion.Euler(0,plan.data.echoYaw,0);if(!echo.Initialize(transform,plan.data.echoMode=="follow"?IntentKind.Follow:IntentKind.Wait))throw new InvalidOperationException("Validated Echo initialization failed.");var panel=GetComponent<JournalPanel>();if(panel&&panel.IsOpen)panel.SetOpen(false);GetComponent<PlayerInputReader>().SetPointerCaptured(false);Physics.SyncTransforms();return true;
            }
            finally{journal.enabled=subscribed;}
        }
        private bool Prepare(SavePayload data,out Plan plan,out string error)
        {
            plan=null;error="";
            try
            {
                if(!echo||!echo.IsInitialized)throw new ArgumentException("Echo persistence binding not ready.");if(data==null)throw new ArgumentException("Save data missing.");data=SaveCodec.Decode(SaveCodec.Encode(data)).Data;
                var scene=gameObject.scene;var journalSet=new HashSet<string>(data.journalIds,StringComparer.Ordinal);var scanned=new HashSet<string>(data.scanIds,StringComparer.Ordinal);var scans=new List<WorldTarget>();var entries=new List<JournalEntry>();var all=new List<WorldTarget>();foreach(var root in scene.GetRootGameObjects())all.AddRange(root.GetComponentsInChildren<WorldTarget>());
                WorldTarget Target(string id){if(!WorldTarget.Active.TryGet(id,out var target)||target.gameObject.scene!=scene||!target.isActiveAndEnabled)throw new ArgumentException("Unknown or unloaded saved target: "+id);return target;}
                foreach(var id in data.scanIds){var target=Target(id);var permissions=target.GetComponent<EchoTargetPermissions>();if(!journalSet.Contains(id)||!permissions||!permissions.CanInvestigate)throw new ArgumentException("Saved scan has no journal or permission: "+id);scans.Add(target);}
                foreach(var id in data.journalIds)
                {
                    var target=Target(id);if(target.GetComponent<ClueSource>()){if(!EchoPersistence.TryReadCanonical(target,scanned.Contains(id),out var record))throw new ArgumentException("Hidden clue lacks saved scan authorization: "+id);entries.Add(JournalEntry.FromClue(record));}
                    else{var permissions=target.GetComponent<EchoTargetPermissions>();if(!scanned.Contains(id)||!permissions||!permissions.CanInvestigate)throw new ArgumentException("Equipment journal lacks saved scan.");var summary=permissions.ScanFact;entries.Add(new JournalEntry(id,target.DisplayName+" · 扫描记录","Echo已完成对"+target.DisplayName+"的扫描。\n"+summary,summary,ClueKind.Scan));}
                }
                var separation=data.playerPosition-data.echoPosition;separation.y=0;if(separation.sqrMagnitude<.9f*.9f)throw new ArgumentException("Saved actors overlap.");
                var physics=scene.GetPhysicsScene();if(!PlayerSpace(physics,data.playerPosition))throw new ArgumentException("Saved player position blocked or without reachable ground.");var navigation=echo.GetComponent<EchoNavigation>();var filter=new NavMeshQueryFilter{agentTypeID=navigation.Agent.agentTypeID,areaMask=navigation.Agent.areaMask};if(!NavMesh.SamplePosition(data.echoPosition,out var nav,.1f,filter)||Vector3.Distance(data.echoPosition,nav.position)>.1f||Blocked(physics.OverlapBox(data.echoPosition+new Vector3(0,.435f,0),new Vector3(.37f,.425f,.37f),overlaps,Quaternion.Euler(0,data.echoYaw,0),~0,QueryTriggerInteraction.Ignore)))throw new ArgumentException("Saved Echo position has no safe navigation surface.");plan=new Plan{data=data,entries=entries.ToArray(),scans=scans.ToArray(),allTargets=all.ToArray()};return true;
            }
            catch(ArgumentException exception){error=exception.Message;return false;}
        }
        private bool Own(Collider collider)=>collider&&(collider.transform.IsChildOf(transform)||collider.transform.IsChildOf(echo.transform));
        private bool Blocked(int count){if(count==overlaps.Length)return true;for(var i=0;i<count;i++)if(overlaps[i]&&!Own(overlaps[i]))return true;return false;}
        private bool PlayerSpace(PhysicsScene physics,Vector3 position)
        {
            if(Blocked(physics.OverlapCapsule(position+Vector3.up*.3f,position+Vector3.up*1.45f,.29f,overlaps,~0,QueryTriggerInteraction.Ignore)))return false;var count=physics.Raycast(position+Vector3.up*.1f,Vector3.down,hits,2,~0,QueryTriggerInteraction.Ignore);if(count==hits.Length)return false;for(var i=0;i<count;i++)if(!Own(hits[i].collider)&&hits[i].normal.y>.5f&&hits[i].point.y<=position.y+.05f)return true;return false;
        }
    }
}
