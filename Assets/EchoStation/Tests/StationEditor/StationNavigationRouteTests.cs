using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Dialogue.Integration;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Station;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class StationNavigationRouteTests:StationPlayFixture
 {
  [Serializable]private struct Sample{public float seconds,delta;public Vector3 player,echo;public string phase;}
  [Serializable]private sealed class Trace{public bool continuous=true,passed;public float maxPlayerStep,maxEchoStep;public double wallSeconds;public string phase;public Sample[] samples;}
  private readonly List<Sample> samples=new List<Sample>();private Trace trace;private string label;private System.Diagnostics.Stopwatch timer;private bool watching;
  private void Begin(string id){samples.Clear();label=id;Directory.CreateDirectory("Docs/Tests/Stage2/Evidence/S2-F04");trace=new Trace();timer=System.Diagnostics.Stopwatch.StartNew();watching=true;station.StartCoroutine(Observe());}
  private IEnumerator Observe(){var a=station.transform.position;var b=station.Echo.transform.position;while(watching&&station){var nextA=station.transform.position;var nextB=station.Echo.transform.position;var da=Vector3.Distance(a,nextA);var db=Vector3.Distance(b,nextB);trace.maxPlayerStep=Mathf.Max(trace.maxPlayerStep,da);trace.maxEchoStep=Mathf.Max(trace.maxEchoStep,db);trace.continuous&=da<=FirstPersonMotion.Speed*Mathf.Min(Time.deltaTime,.1f)+.2f&&db<=station.Echo.GetComponent<NavMeshAgent>().speed*Time.deltaTime+.2f;samples.Add(new Sample{seconds=Time.timeSinceLevelLoad,delta=Time.deltaTime,player=nextA,echo=nextB,phase=trace.phase});a=nextA;b=nextB;yield return null;}}
  private void Phase(string phase){trace.phase=phase;File.WriteAllText("Docs/Tests/Stage2/Evidence/S2-F04/"+label+"-Progress.json",JsonUtility.ToJson(new Sample{seconds=Time.timeSinceLevelLoad,player=station.transform.position,echo=station.Echo.transform.position,phase=phase},true));}
  private void Finish(){watching=false;trace.wallSeconds=timer.Elapsed.TotalSeconds;trace.samples=samples.ToArray();trace.passed=trace.continuous;File.WriteAllText("Docs/Tests/Stage2/Evidence/S2-F04/"+label+"-Trace.json",JsonUtility.ToJson(trace,true));Assert.That(trace.continuous,Is.True,"Unexpected displacement beyond actual movement budget");}
  private IEnumerator Walk(float x,float z,double seconds=18)
  {
   var input=station.GetComponent<PlayerInputReader>();input.SetPointerCaptured(true);Assert.That(input.CanPlay,Is.True);var time=System.Diagnostics.Stopwatch.StartNew();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.W));
   try{while(true){var delta=new Vector3(x-station.transform.position.x,0,z-station.transform.position.z);if(delta.magnitude<.22f)break;
    if(time.Elapsed.TotalSeconds>seconds){var origin=station.transform.position+Vector3.up*.8f;var hitName=scene.GetPhysicsScene().Raycast(origin,station.transform.forward,out var hit,2,~0,QueryTriggerInteraction.Ignore)?hit.collider.name:"none";Assert.Fail("Real W walk blocked at "+station.transform.position+" towards "+x+","+z+" focus="+input.HasApplicationFocus+" captured="+input.PointerCaptured+" blocked="+input.UiBlocked+" W="+inputs.Keyboard.wKey.isPressed+" move="+input.Move+" motor="+station.Player.enabled+" collision="+station.Player.LastCollision+" front="+hitName+" settings="+InputSystem.settings.editorInputBehaviorInPlayMode+" currentKeyboard="+(Keyboard.current==inputs.Keyboard));}
    station.Player.SetLookAngles(Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,0);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.W));yield return R04ShadowProbe.Frame();}}
   finally{InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());}yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();
  }
  private IEnumerator Near(double seconds=18){yield return Until(()=>{var d=station.transform.position-station.Echo.transform.position;d.y=0;return d.magnitude<2.8f;},seconds,"Echo could not catch up in "+trace.phase+" at "+station.Echo.transform.position);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));}
  private IEnumerator Door(string id,float z){yield return Walk(0,z);WorldTarget.Active.TryGet(id,out var target);Look(target.transform.position);yield return R04ShadowProbe.Frame();yield return Press(Key.E);Assert.That(target.GetComponent<StationGate>().IsOpen,Is.True,id);}
  [UnityTest,Timeout(240000)]public IEnumerator F04_A01_ActualPlayerAndEchoTraverseFiveZonesAndReturnWithoutRelocation()
  {
   yield return new EnterPlayMode();yield return Boot();Begin("A-RoundTrip");Assert.That(station.Progress.TryRestore(new[]{"pz02","pz03","pz04","pz05"}),Is.True);station.GetComponent<StationPowerPanel>().RestoreCycle(true,3);yield return Press(Key.Digit1);
   Phase("valley_to_cabin");yield return Walk(0,-12);yield return Near();yield return Walk(0,-2);yield return Near();Phase("manual_cabin_door");yield return Door("cabin_door_01",3.3f);Phase("descent_to_lab");yield return Walk(0,24);yield return Near();Assert.That(station.transform.position.y,Is.InRange(-4.1f,-3.8f));Phase("lab_to_vent");yield return Walk(0,36);yield return Near();yield return Walk(0,50);yield return Near();Phase("manual_core_door");yield return Door("core_door_01",56.2f);Phase("core_arrival");yield return Walk(0,64);yield return Near();Assert.That(station.CurrentZone,Is.EqualTo("core"));Assert.That(station.Echo.transform.position.z,Is.GreaterThan(58));
   Phase("return_to_vent");yield return Walk(0,50);yield return Near();Phase("return_to_lab");yield return Walk(0,30);yield return Near();Phase("return_up_ramp");yield return Walk(0,3);yield return Near();Assert.That(station.transform.position.y,Is.InRange(-.1f,.2f));Phase("return_to_valley");yield return Walk(0,-12);yield return Near();yield return Walk(0,-34);yield return Near();Assert.That(station.CurrentZone,Is.EqualTo("valley"));Assert.That(station.Echo.transform.position.z,Is.LessThan(-7));Assert.That(station.Echo.GetComponent<EchoNavigation>().IsBound,Is.True);Finish();
  }
  [UnityTest,Timeout(180000)]public IEnumerator F04_B01_ClosedDoorThenDynamicBlockerWaitAndExplicitResumeRemainRecoverable()
  {
   yield return new EnterPlayMode();yield return Boot();Begin("B-Recovery");var controller=station.GetComponent<ScriptDialogueController>();Phase("closed_door_scan");controller.SubmitPlayerIntent(ScriptCommands.Investigate("scan_lab_calibration_01"));yield return Until(()=>station.Echo.CurrentMode==IntentKind.Wait,6,"Blocked investigation did not settle");Assert.That(station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered("scan_lab_calibration_01"),Is.False);yield return Press(Key.Digit1);Phase("walk_to_cabin");yield return Walk(0,-2);yield return Near();station.Progress.TryRestore(new[]{"pz02"});yield return Door("cabin_door_01",3.3f);Phase("enter_descent");yield return Walk(0,10);Assert.That(station.Echo.transform.position.z,Is.LessThan(8));
   var blocker=GameObject.CreatePrimitive(PrimitiveType.Cube);blocker.name="F04_Test_TransientObstacle";blocker.transform.position=new Vector3(0,.55f,8.3f);blocker.transform.localScale=new Vector3(3.2f,3,.6f);blocker.GetComponent<MeshRenderer>().enabled=false;var obstacle=blocker.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.size=Vector3.one;obstacle.center=Vector3.zero;obstacle.carving=true;obstacle.carveOnlyStationary=false;Physics.SyncTransforms();scene.GetPhysicsScene().Simulate(.02f);Phase("dynamic_blocked");yield return Walk(0,14);yield return new WaitForSecondsRealtime(1);Assert.That(station.Echo.transform.position.z,Is.LessThan(8.3f));yield return Press(Key.Digit2);var stopped=station.Echo.transform.position;yield return new WaitForSecondsRealtime(.5f);Assert.That(Vector3.Distance(stopped,station.Echo.transform.position),Is.LessThan(.03f));Assert.That(station.Memories.Store.TryGetCharacter("echo_position_agreement",out var agreement)&&agreement.Summary.Contains("待命"),Is.True);
   UnityEngine.Object.Destroy(blocker);yield return R04ShadowProbe.Frame();Phase("explicit_resume");yield return Press(Key.Digit1);yield return Near(18);Assert.That(station.Echo.transform.position.z,Is.GreaterThan(10));Assert.That(station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered("scan_lab_calibration_01"),Is.False);Assert.That(station.GetComponent<StationPowerPanel>().Cycle.Powered,Is.False);Finish();
  }
  [TearDown]public void StopTrace(){watching=false;if(trace!=null&&!trace.passed){trace.wallSeconds=timer?.Elapsed.TotalSeconds??0;trace.samples=samples.ToArray();File.WriteAllText("Docs/Tests/Stage2/Evidence/S2-F04/"+label+"-IncompleteTrace.json",JsonUtility.ToJson(trace,true));}}
 }
}
