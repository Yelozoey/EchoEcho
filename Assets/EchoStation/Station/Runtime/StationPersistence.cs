using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Dialogue;
using EchoStation.Echo;
using EchoStation.Persistence;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.AI;
namespace EchoStation.Station
{
 [DisallowMultipleComponent]
 public sealed class StationPersistence:MonoBehaviour
 {
  private StationSession station;private StationFileStore files;
  public StationFileStore Files=>files??(files=new StationFileStore(Path.Combine(Application.persistentDataPath,"Saves","S2")));
  public bool Busy{get;private set;}public bool LastSucceeded{get;private set;}public string LastMessage{get;private set;}="";
  public string StageLabel{get;private set;}="S2";
#if UNITY_EDITOR
  internal Action BeforeCommitForTest;
#endif
  private void Awake()=>station=GetComponent<StationSession>();
  public void ConfigureDirectory(string directory,string stageLabel="S2"){if(Busy)throw new InvalidOperationException("Persistence busy.");if(stageLabel!="S2"&&stageLabel!="S3")throw new ArgumentException("Unknown persistence stage label.");var candidate=new StationFileStore(directory);files=candidate;StageLabel=stageLabel;}
  public StationSaveData Capture()
  {
   if(!station.Ready||Busy)throw new InvalidOperationException("观测站或存档操作尚未就绪。");station.RefreshZone();return CaptureRaw();
  }
  private StationSaveData CaptureRaw()
  {
   var a=station.transform.position;var b=station.Echo.transform.position;return new StationSaveData{Player=new SavedPose(a.x,a.y,a.z,station.Player.Yaw,station.Player.Pitch),Echo=new SavedPose(b.x,b.y,b.z,station.Echo.transform.eulerAngles.y),EchoMode=station.Echo.CurrentMode==IntentKind.Follow?"follow":"wait",Flashlight=GetComponent<PlayerFlashlight>().IsOn,Elapsed=station.Elapsed,Progress=station.CaptureProgressState(),MemoryJson=station.Memories.CaptureJson(),Dialogue=station.Dialogue.Session.CaptureRecent().ToArray()};
  }
  public bool TrySave()
  {
   LastSucceeded=false;if(Busy){LastMessage="正在恢复状态，请稍后保存。";return false;}if(GetComponent<StationEndingPanel>().HasEnded){LastMessage="复查已结束，保留选择前的手动档。";station.SetStatus(LastMessage);return false;}
   try{var data=Capture();if(!StationRestorePlan.TryBuild(station,data,out _,out var why)){LastMessage=why;station.SetStatus(why);return false;}LastSucceeded=Files.TryWrite(data,out var error);LastMessage=LastSucceeded?StageLabel+"手动存档已完整保存。":error;}catch(ArgumentException e){LastMessage=e.Message;}station.SetStatus(LastMessage);return LastSucceeded;
  }
  public IEnumerator Load()
  {
   if(Busy)yield break;var read=Files.Read();if(!read.Success){LastSucceeded=false;LastMessage=read.Detail;station.SetStatus(LastMessage);yield break;}yield return RestoreData(read.Data);if(LastSucceeded&&read.Status==StationReadStatus.Backup){LastMessage="主档不可用，已整体恢复上次有效备份。";station.SetStatus(LastMessage);}
  }
  public bool CanRestore(StationSaveData data,out string error)=>StationRestorePlan.TryBuild(station,data,out _,out error);
  public IEnumerator RestoreData(StationSaveData data)
  {
   if(Busy)yield break;LastSucceeded=false;if(!StationRestorePlan.TryBuild(station,data,out var plan,out var error)){LastMessage=error;station.SetStatus(error);yield break;}
   var old=CaptureRaw();if(!StationRestorePlan.TryBuild(station,old,out var rollback,out error,true)){LastMessage="无法准备恢复前快照："+error;station.SetStatus(LastMessage);yield break;}
   var input=GetComponent<PlayerInputReader>();var journal=station.Journal;var scanner=station.Echo.GetComponent<EchoScanController>();var scanEnabled=scanner.enabled;var journalEnabled=journal.enabled;var motorEnabled=station.Player.enabled;var oldBlocked=input.UiBlocked;var visible=station.Dialogue.Session.Messages.ToArray();var ending=GetComponent<StationEndingPanel>();var oldEnding=ending.Choice.EndingId;var oldArchive=ending.ArchivedEvidenceIds.ToArray();var endingVisible=ending.IsOpen;
   Busy=true;station.Restoring=true;CloseModals();input.SetUiBlocked(true);station.Player.enabled=false;journal.enabled=false;scanner.enabled=false;station.Echo.Shutdown();
   var rollbackSucceeded=false;
   try{
    error=null;try{BeginWorld(plan);}catch(Exception e){error=e.Message;}
    if(error==null){var bound=false;yield return Bind(plan,ok=>bound=ok);if(!bound)error="Echo导航无法恢复。";}
    if(error==null)try{
#if UNITY_EDITOR
     var injection=BeforeCommitForTest;BeforeCommitForTest=null;injection?.Invoke();
#endif
     Commit(plan,false);LastSucceeded=true;}catch(Exception e){error=e.Message;}
    if(!LastSucceeded){var original=error;try{BeginWorld(rollback);}catch(Exception e){error="恢复旧世界失败："+e.Message;}
     var rebound=false;yield return Bind(rollback,ok=>rebound=ok);try{if(!rebound)throw new InvalidOperationException("旧Echo导航恢复失败。");Commit(rollback,true);if(!station.Dialogue.Session.TryRestoreVisibleForRollback(visible))throw new InvalidOperationException("旧对话恢复失败。");ending.RestoreOutcome(oldEnding,oldArchive,endingVisible);error=original;rollbackSucceeded=true;}catch(Exception e){error=original+"；回滚异常："+e.Message;Debug.LogError(error);}
    }
    LastMessage=LastSucceeded?StageLabel+"进度、日志、扫描、记忆与对话已整体恢复。":(rollbackSucceeded?"读取未完成，已恢复原状态：":"读取和回滚未完成，请保留现场：")+error;
   }finally{scanner.enabled=scanEnabled;journal.enabled=journalEnabled;station.Player.enabled=motorEnabled;station.Restoring=false;Busy=false;input.SetUiBlocked(LastSucceeded?false:oldBlocked);input.SetPointerCaptured(false);Physics.SyncTransforms();}
   station.SetStatus(LastMessage);
  }
  private void CloseModals(){station.Dialogue.SetOpen(false);GetComponent<JournalPanel>().SetOpen(false);GetComponent<StationControls>().SetOpen(false);GetComponent<StationTerminalPanel>().Close();GetComponent<StationPowerPanel>().Close();GetComponent<StationEndingPanel>().ResetForRestore();}
  private void BeginWorld(StationRestorePlan p)
  {
   var controller=GetComponent<CharacterController>();var enabled=controller.enabled;controller.enabled=false;transform.position=StationRestorePlan.Point(p.Data.Player);station.Player.SetLookAngles(p.Data.Player.Yaw,p.Data.Player.Pitch);station.Player.ResetMotion();controller.enabled=enabled;p.Cabin.Apply(p.Data.Progress.CabinOpen);p.Core.Apply(p.Data.Progress.CoreOpen);p.CabinData.SetActive(p.Data.Progress.Completed.Contains("pz02"));p.VentData.SetActive(p.Data.Progress.Completed.Contains("pz05"));if(!GetComponent<StationPowerPanel>().RestoreCycle(p.Data.Progress.Powered,p.Data.Progress.VentElapsed))throw new InvalidOperationException("Validated ventilation rejected.");Physics.SyncTransforms();
  }
  private IEnumerator Bind(StationRestorePlan p,Action<bool> result)
  {
   // Let carving observe the candidate gates/player before binding. No input or game-state producer runs during this transaction.
   yield return null;yield return null;var navigation=station.Echo.GetComponent<EchoNavigation>();var filter=new NavMeshQueryFilter{agentTypeID=navigation.Agent.agentTypeID,areaMask=navigation.Agent.areaMask};var point=StationRestorePlan.Point(p.Data.Echo);var end=Time.realtimeSinceStartup+2;
   while(Time.realtimeSinceStartup<end){if(NavMesh.SamplePosition(point,out var hit,.12f,filter)&&Vector3.Distance(point,hit.position)<.12f&&navigation.TryBind(point)){station.Echo.transform.rotation=Quaternion.Euler(0,p.Data.Echo.Yaw,0);result(true);yield break;}yield return null;}result(false);
  }
  private void Commit(StationRestorePlan p,bool rollback)
  {
   EchoPersistence.Restore(station.Echo.GetComponent<EchoScanController>(),p.Targets,p.Scanned);station.Journal.Store.Replace(p.Entries);if(!station.Progress.TryRestore(p.Data.Progress.Completed))throw new InvalidOperationException("Validated progress rejected.");if(!station.Memories.TryAdopt(p.Memory,out var error))throw new InvalidOperationException(error);if(!station.Knowledge.Synchronize(station.Memories.Store))throw new InvalidOperationException(station.Knowledge.LastError);if(!station.Dialogue.Session.TryRestoreRecent(p.Data.Dialogue))throw new InvalidOperationException("Validated dialogue rejected.");station.RestoreClockAndZone(p.Data.Elapsed,p.Data.Progress.Zone);GetComponent<PlayerFlashlight>().SetOn(p.Data.Flashlight);if(!station.Echo.Initialize(transform,p.Data.EchoMode=="follow"?IntentKind.Follow:IntentKind.Wait))throw new InvalidOperationException("Validated Echo initialization rejected.");if(!rollback){station.Dialogue.InputField.text="";if(GetComponent<StationScanProgress>().Synchronize(false)==EchoStation.Puzzles.PuzzleResult.Completed)throw new InvalidOperationException("Restore must not manufacture a completion.");}
  }
 }
}
