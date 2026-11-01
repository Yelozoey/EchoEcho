using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Persistence;
using EchoStation.Player;
using EchoStation.Puzzles;
using EchoStation.Station;
using EchoStation.Story;
using EchoStation.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
namespace EchoStation.Tests.PlayerFixtures
{
 // Diagnostic input operator. It queries a route but only keyboard/mouse/E/UI can change gameplay.
 public sealed class StationRouteDriver:IDisposable
 {
  [Serializable]public sealed class Result
  {public bool passed,wrongCodeRejected,wrongCircuitRejected,deviceRefused,unknownCodeProtected,manualSlotRetained,reloaded,inputRestored;public string ending,phase,error;public double wallSeconds;public int reads,commands,walkSegments,mutedPhysicalDevices,catalogueClues;public string[] completed,journal,scans;}
  private readonly StationSession station;private readonly Keyboard keyboard;private readonly Mouse mouse;private readonly Func<IEnumerator> nextFrame;private readonly string evidence;private readonly PlayerInputReader input;
  private readonly InputDevice[] muted;private bool disposed;
  [Serializable]private sealed class PathDiagnostic{public Vector3 player,target,nearest,goal,chosen;public string nearestStatus;public bool alternateStart;public int retries;}
  public Result Report{get;}=new Result();
  public StationSaveData SavedData{get;private set;}
  public Func<string,IEnumerator> Checkpoint{get;set;}
  public StationRouteDriver(StationSession session,Keyboard keys,Mouse pointer,Func<IEnumerator> frame,string directory)
  {station=session;keyboard=keys;mouse=pointer;nextFrame=frame;evidence=Path.GetFullPath(directory);Directory.CreateDirectory(evidence);input=session.GetComponent<PlayerInputReader>();muted=InputSystem.devices.Where(d=>(d is Keyboard||d is Mouse)&&d!=keys&&d!=pointer&&d.enabled).ToArray();Report.mutedPhysicalDevices=muted.Length;foreach(var d in muted)InputSystem.DisableDevice(d);}
  public void Dispose(){if(disposed)return;foreach(var d in muted)if(d.added)InputSystem.EnableDevice(d);Report.inputRestored=muted.All(d=>!d.added||d.enabled);disposed=true;}
  private static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
  private IEnumerator Frame(){input.SetApplicationFocus(true);yield return nextFrame();}
  private void Phase(string name){Report.phase=name;File.WriteAllText(Path.Combine(evidence,"Progress.json"),JsonUtility.ToJson(Report,true));}
  private IEnumerator Until(Func<bool> ready,double seconds,string message){var timer=System.Diagnostics.Stopwatch.StartNew();while(!ready()){if(timer.Elapsed.TotalSeconds>seconds)throw new TimeoutException(message+" phase="+Report.phase+" player="+station.transform.position+" echo="+station.Echo.transform.position);yield return Frame();}}
  public IEnumerator KeyPress(Key key){InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));yield return Frame();InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return Frame();yield return Frame();}
  private IEnumerator Type(string value){foreach(var c in value)InputSystem.QueueTextEvent(keyboard,c);yield return Frame();yield return Frame();}
  private IEnumerator ClickPoint(Vector2 point){InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return Frame();InputSystem.QueueStateEvent(mouse,new MouseState{position=point,buttons=1});yield return Frame();InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return Frame();yield return Frame();}
  public IEnumerator Click(Button button)
  {
   Check(button&&button.gameObject.activeInHierarchy&&button.interactable,"Inactive UI button");Canvas.ForceUpdateCanvases();yield return Frame();var rect=button.GetComponent<RectTransform>();var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));var witnessed=false;UnityEngine.Events.UnityAction witness=()=>witnessed=true;button.onClick.AddListener(witness);try{yield return ClickPoint(point);}finally{if(button)button.onClick.RemoveListener(witness);}Check(witnessed,"Actual pointer click missed "+button.name+" at "+point+" screen="+Screen.width+"x"+Screen.height);
  }
  private IEnumerator Capture(){if(!input.CanPlay){Check(!input.UiBlocked,"Cannot capture through a modal");yield return ClickPoint(new Vector2(Screen.width*.5f,Screen.height*.5f));}Check(input.CanPlay,"Pointer capture failed");}
  private WorldTarget Target(string id){Check(WorldTarget.Active.TryGet(id,out var t)&&t.gameObject.scene==station.gameObject.scene,"Missing actual target "+id);return t;}
  private void Aim(Vector3 target){var d=target-station.Player.View.position;station.Player.SetLookAngles(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg);}
  private IEnumerator Segment(Vector3 target)
  {
   var timer=System.Diagnostics.Stopwatch.StartNew();InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
   try{while(true){var d=target-station.transform.position;d.y=0;if(d.magnitude<.18f)break;Check(input.CanPlay,"Walk lost focus/capture or modal changed");if(timer.Elapsed.TotalSeconds>25)throw new TimeoutException("W walk blocked at "+station.transform.position+" towards "+target+" move="+input.Move+" key="+keyboard.wKey.isPressed);station.Player.SetLookAngles(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,0);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));yield return Frame();}}
   finally{InputSystem.QueueStateEvent(keyboard,new KeyboardState());}Report.walkSegments++;yield return Frame();yield return Frame();
  }
  public IEnumerator Walk(Vector3 target)
  {
   yield return Capture();if(station.Echo.CurrentMode!=IntentKind.Follow)yield return KeyPress(Key.Digit1);
   var agent=station.Echo.GetComponent<EchoNavigation>().Agent;var filter=new NavMeshQueryFilter{agentTypeID=agent.agentTypeID,areaMask=agent.areaMask};var d=target-station.transform.position;d.y=0;if(d.magnitude<.18f)yield break;
   if(d.magnitude<2.2f){yield return Segment(target);yield break;}
   var path=new NavMeshPath();var deadline=Time.realtimeSinceStartup+2;var found=false;var alternate=false;var start=new NavMeshHit();PathDiagnostic diagnostic=null;var retries=0;
   while(!found&&Time.realtimeSinceStartup<deadline){retries++;Check(NavMesh.SamplePosition(target,out var end,.8f,filter),"No operator route goal "+target);var nearest=NavMesh.SamplePosition(station.transform.position,out start,1.5f,filter);
    if(nearest&&NavMesh.CalculatePath(start.position,end.position,filter,path)&&path.status==NavMeshPathStatus.PathComplete){found=true;break;}
    if(diagnostic==null)diagnostic=new PathDiagnostic{player=station.transform.position,target=target,nearest=start.position,goal=end.position,nearestStatus=path.status.ToString()};
    foreach(var angle in new[]{0,45,-45,90,-90,135,-135,180}){var point=station.transform.position+(Quaternion.AngleAxis(angle,Vector3.up)*d.normalized)*1.2f;if(NavMesh.SamplePosition(point,out var candidate,.45f,filter)&&NavMesh.CalculatePath(candidate.position,end.position,filter,path)&&path.status==NavMeshPathStatus.PathComplete){start=candidate;found=true;alternate=true;break;}}
    if(!found)yield return Frame();
   }
   if(diagnostic!=null){diagnostic.alternateStart=alternate;diagnostic.chosen=start.position;diagnostic.retries=retries;File.WriteAllText(Path.Combine(evidence,"PathDiagnostic_"+Report.walkSegments+".json"),JsonUtility.ToJson(diagnostic,true));}
   Check(found,"Operator route blocked before actual walk to "+target);if(alternate)yield return Segment(start.position);foreach(var corner in path.corners.Skip(1))yield return Segment(corner);yield return Segment(target);
  }
  private IEnumerator Near(){yield return Until(()=>{var d=station.Echo.transform.position-station.transform.position;d.y=0;return d.magnitude<2.8f;},25,"Echo did not catch up");}
  private Vector3 Stand(WorldTarget target)=>target.GetComponent<EchoTargetPermissions>().ApproachPoint.position-new Vector3(0,0,.4f);
  private IEnumerator Interact(string id){var target=Target(id);yield return Walk(Stand(target));Aim(target.transform.position);yield return Frame();yield return KeyPress(Key.E);}
  private IEnumerator Read(string id)
  {
   var anchor=station.transform.position;var witnessed=false;Action<ClueRecord> witness=c=>{if(c.Id==id)witnessed=true;};station.GetComponent<PlayerInteraction>().ClueRead+=witness;try{yield return Interact(id);}finally{station.GetComponent<PlayerInteraction>().ClueRead-=witness;}Check(witnessed&&station.Journal.Store.TryGet(id,out _),"Actual E did not read "+id);Report.reads++;yield return Walk(anchor);
  }
  private IEnumerator Command(string text)
  {
   yield return Capture();yield return KeyPress(Key.T);Check(station.Dialogue.IsOpen,"T did not open dialogue");yield return Until(()=>station.Dialogue.InputField.isFocused,5,"Dialogue input focus");yield return Type(text);yield return KeyPress(Key.Enter);Report.commands++;yield return KeyPress(Key.Escape);Check(!station.Dialogue.IsOpen,"Dialogue did not close");yield return Capture();
  }
  private IEnumerator Investigate(string id,string text,Vector3 stand)
  {yield return Walk(stand);yield return Near();yield return Command(text);yield return Until(()=>station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered(id)&&station.Echo.CurrentMode==IntentKind.Wait,40,"Actual investigation did not finish "+id);Check(station.Journal.Store.TryGet(id,out _),"Scan did not archive "+id);}
  private string KnownPassword(string clue){Check(station.Memories.Store.TryGetClue(clue,out var record)&&record.Password!=null,"Code source not actually known: "+clue);return record.Password;}
  private IEnumerator Terminal(string device,string knownSource,bool wrongFirst=false)
  {
   yield return Interact(device);var panel=station.GetComponent<StationTerminalPanel>();Check(panel.IsOpen,"E did not open terminal "+device);yield return Until(()=>panel.CodeInput.isFocused,5,"Terminal focus");if(wrongFirst){yield return Type("0000");yield return KeyPress(Key.Enter);Check(!station.Progress.Has("pz02"),"Wrong code unlocked cabin");Report.wrongCodeRejected=true;}yield return Type(KnownPassword(knownSource));yield return KeyPress(Key.Enter);Check(station.Progress.Has(device=="cabin_terminal_01"?"pz02":"pz05"),"Actual typed terminal code not accepted");yield return KeyPress(Key.Escape);Check(!panel.IsOpen,"Terminal modal remained open");yield return Capture();
  }
  private IEnumerator Door(string id){yield return Interact(id);Check(Target(id).GetComponent<StationGate>().IsOpen,"Player E did not open authorized door "+id);}
  private IEnumerator Evidence(string id)
  {yield return Capture();yield return KeyPress(Key.J);Check(station.GetComponent<JournalPanel>().IsOpen,"J did not open journal");var panel=station.GetComponent<StationEvidencePanel>();yield return Click(id=="pz01"?panel.TimelineButton:panel.CauseButton);Check(station.Progress.Has(id),"Player evidence confirmation failed "+id);yield return KeyPress(Key.Escape);yield return Capture();}
  private IEnumerator CaptureCheckpoint(string name){Phase(name);if(Checkpoint!=null)yield return Checkpoint(name);}
  public IEnumerator RunSeal()=>Run("seal_exit");
  public IEnumerator Run(string endingId)
  {
   var clock=System.Diagnostics.Stopwatch.StartNew();var spec=StationScript.Endings.Single(e=>e.Id==endingId);var full=endingId=="archive_exit";var cause=endingId!="seal_exit";Check(station.Progress.Capture().Length==0&&station.Journal.Store.Entries.Count==0,"Route must start from an empty new session");
   if(full){Phase("valley_timeline");yield return Read("note_entry_inspection_01");yield return Read("note_evacuation_01");yield return Evidence("pz01");}
   Phase("cabin_records");yield return Walk(new Vector3(0,0,-12));yield return Walk(new Vector3(0,0,-2));yield return Near();yield return Read("note_maintenance_01");
   yield return Command("帮我开门");Report.deviceRefused=station.Dialogue.Session.Messages.Last().IsRefusal&&!Target("cabin_door_01").GetComponent<StationGate>().IsOpen&&!station.Progress.Has("pz02");Check(Report.deviceRefused,"Device delegation was not refused");
   yield return Command("通道终端的密码是多少");Report.unknownCodeProtected=station.Dialogue.Session.Messages.Last().Text=="我目前没有相关记录。";Check(Report.unknownCodeProtected,"Unknown vent code used unrelated known password");
   Phase("first_real_scan");if(full){yield return Walk(new Vector3(7.7f,0,1));yield return Near();yield return Read("note_roster_01");}yield return Investigate("scan_mark_01","调查储藏室可疑点",new Vector3(7.7f,0,1));Aim(Target("scan_mark_01").transform.position);yield return CaptureCheckpoint("cabin_arrow");yield return Walk(new Vector3(0,0,-2));
   Phase("cabin_terminal");yield return Terminal("cabin_terminal_01","note_maintenance_01",true);yield return Door("cabin_door_01");
   Phase("lab_records");yield return Walk(new Vector3(0,-4,24));yield return Near();yield return Read("note_lab_circuit_01");if(cause)yield return Read("data_lab_maintenance_01");if(full)yield return Read("note_lab_sensor_01");
   Phase("second_real_scan");yield return Investigate("scan_lab_calibration_01","调查校准架",new Vector3(1,-4,30));Check(station.Progress.Has("pz04"),"Two real scans did not complete credential");
   Phase("power_ui");yield return Interact("power_panel_01");var power=station.GetComponent<StationPowerPanel>();Check(power.IsOpen,"Power E modal absent");yield return Click(power.SubmitButton);Report.wrongCircuitRejected=!station.Progress.Has("pz03")&&!power.Cycle.Powered;Check(Report.wrongCircuitRejected,"Wrong circuit energized system");foreach(var i in new[]{0,1,2})yield return Click(power.ToggleButton(i));yield return Click(power.SubmitButton);Check(station.Progress.Has("pz03"),"Player circuit submission failed");yield return Until(()=>power.Cycle.PassageReady,6,"Local ventilation did not finish");yield return KeyPress(Key.Escape);yield return Capture();
   Phase("vent_terminal");yield return Walk(new Vector3(0,-4,42));yield return Near();if(full)yield return Read("note_vent_route_01");yield return Terminal("vent_terminal_01","scan_lab_calibration_01");yield return Door("core_door_01");yield return Walk(new Vector3(0,-4,64));yield return Near();
   if(cause){Phase("core_evidence");yield return Read("data_core_sensor_01");if(full)yield return Read("note_core_shutdown_01");yield return Evidence("pz06");}
   Phase("prechoice_manual_save");yield return KeyPress(Key.Digit2);yield return KeyPress(Key.F5);var persistence=station.GetComponent<StationPersistence>();Check(persistence.LastSucceeded,persistence.LastMessage);var saved=File.ReadAllBytes(persistence.Files.PrimaryPath);SavedData=persistence.Files.Read().Data;
   Phase("core_choice");yield return Interact("core_terminal_01");var ending=station.GetComponent<StationEndingPanel>();Check(ending.IsOpen,"Core E did not open choice panel");for(var i=0;i<3;i++)Check(ending.ChoiceButton(i).interactable==StationScript.Endings[i].Required.All(station.Progress.Has),"Ending availability did not match evidence");var index=StationScript.Endings.ToList().FindIndex(e=>e.Id==endingId);yield return Click(ending.ChoiceButton(index));Check(!ending.HasEnded,"Selection skipped confirmation");yield return KeyPress(Key.Escape);yield return Capture();yield return Interact("core_terminal_01");yield return Click(ending.ChoiceButton(index));yield return Click(ending.ConfirmButton);Check(ending.Choice.EndingId==endingId&&station.Echo.CurrentMode==(spec.EchoStays?IntentKind.Wait:IntentKind.Follow),"Ending outcome/Echo agreement mismatch");
   Report.manualSlotRetained=File.ReadAllBytes(persistence.Files.PrimaryPath).SequenceEqual(saved);Check(Report.manualSlotRetained,"Ending overwrote manual slot");Report.ending=ending.Choice.EndingId;Report.completed=station.Progress.Capture();Report.journal=station.Journal.Store.Entries.Select(e=>e.Id).ToArray();Report.scans=station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.GetDiscoveredIds();Report.catalogueClues=StationClues.All.Count(c=>Report.journal.Contains(c.Id));if(full){Check(Report.catalogueClues==14,"Full investigation missed a catalogue source");Check(ending.ArchivedEvidenceIds.Count==Report.journal.Length,"Game archive omitted collected evidence");}yield return CaptureCheckpoint("ending_"+endingId);
   if(cause){var canvas=station.gameObject.scene.GetRootGameObjects().Single(r=>r.name=="ES_Ending");yield return Click(canvas.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Load"));yield return Until(()=>!persistence.Busy&&!station.GetComponent<StationLifecycle>().IsChangingSession,8,"Ending load did not finish");Check(persistence.LastSucceeded&&!ending.HasEnded,persistence.LastMessage);Check(station.Memories.CaptureJson()==SavedData.MemoryJson,"Ending load replayed or changed memory");Check(station.Progress.Capture().SequenceEqual(SavedData.Progress.Completed),"Ending load changed progress");Check(station.Journal.Store.Entries.Select(e=>e.Id).SequenceEqual(SavedData.Progress.Journal),"Ending load changed journal");Check(ProgressAudit.Validate(station.CaptureProgressState())==null,"Restored route state invalid");Check(File.ReadAllBytes(persistence.Files.PrimaryPath).SequenceEqual(saved),"Ending load overwrote slot");Report.reloaded=true;yield return CaptureCheckpoint("restored_prechoice");}
   Report.wallSeconds=clock.Elapsed.TotalSeconds;Report.passed=true;Phase("complete");
  }
 }
}
