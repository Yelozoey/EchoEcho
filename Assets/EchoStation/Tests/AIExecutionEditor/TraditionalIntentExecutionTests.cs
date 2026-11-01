using System;
using System.Collections;
using System.Linq;
using EchoStation.AI.Authorization;
using EchoStation.AI.Context;
using EchoStation.AI.Execution;
using EchoStation.AI.IntentJson;
using EchoStation.Dialogue;
using EchoStation.Dialogue.Commands;
using EchoStation.Dialogue.Integration;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Memory.Integration;
using EchoStation.Player;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Player;
using EchoStation.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public sealed class TraditionalIntentExecutionTests
 {
  private Scene scene,previous;private bool background;private SyntheticInputScope inputs;private CabinDemo demo;private DialoguePanel panel;private ScriptDialogueController controller;private MemorySession memory;private JournalMemoryBridge bridge;private ScriptKnowledgeResponder knowledge;
  private static readonly ScriptTarget[] Targets={new ScriptTarget("scan_mark_01","储藏室可疑点",true,false,"储藏室可疑位置"),new ScriptTarget("power_panel_01","供电面板",true,true)};
  private IEnumerator Setup()
  {
   background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();inputs=new SyntheticInputScope();
   scene=EditorSceneManager.LoadSceneInPlayMode("Assets/EchoStation/World/Scenes/ES_Cabin_Demo.unity",new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));
   yield return Until(()=>scene.isLoaded,5,"Cabin fixture load");SceneManager.SetActiveScene(scene);demo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CabinDemo>()).Single();yield return Until(()=>demo.Ready,5,"Cabin fixture ready");
   demo.Echo.AutomaticWarningsEnabled=false;Assert.That(demo.Echo.TrySubmit(ScriptCommands.Wait(),out _),Is.True);
   panel=demo.gameObject.AddComponent<DialoguePanel>();memory=new MemorySession();bridge=new JournalMemoryBridge(demo.GetComponent<PlayerJournal>().Store,memory.Store);knowledge=new ScriptKnowledgeResponder(memory,new MemorySearch());
   controller=demo.gameObject.AddComponent<ScriptDialogueController>();controller.Configure(panel,demo.Echo,memory,bridge,()=>Targets,gameClock:()=>Time.time);
  }
  private static IEnumerator Until(Func<bool> condition,double seconds,string label)
  {var clock=System.Diagnostics.Stopwatch.StartNew();while(!condition()){if(clock.Elapsed.TotalSeconds>seconds)Assert.Fail(label+" timeout");yield return R04ShadowProbe.Frame();}}
  private void MovePlayer(Vector3 point)
  {var collider=demo.GetComponent<CharacterController>();collider.enabled=false;demo.transform.position=point;collider.enabled=true;scene.GetPhysicsScene().Simulate(.02f);}
  private bool Apply(string input,string kind,string id=null)
  {
   // Hand-authored JSON isolates execution. This is not a model-quality or live-inference fixture.
   var fields=kind=="investigate"?",\"targetName\":\""+id+"\"":kind=="warn"?",\"dangerTarget\":\""+id+"\"":"";
   var content=kind=="follow"||kind=="wait"?"":",\"content\":\"UNTRUSTED_MODEL_FACT_9999\"";
   var parsed=IntentJsonParser.Parse("{\"intent\":\""+kind+"\""+fields+content+"}");Assert.That(parsed.Success,Is.True);
   var scope=new InferenceSnapshot(memory.Store,input,Time.time,"cabin",Targets.Select((t,i)=>new TargetCandidate(t.Id,t.Name,"cabin",true,t.CanInvestigate,t.IsDanger,true,i+1))).Targets;
   var decision=IntentActionGuard.Evaluate(parsed.Intent,input,scope,scope,Targets.Select(t=>new TrustedTargetIdentity(t.Id,t.Name,t.Aliases.ToArray())));
   var accepted=controller.SubmitPlayerIntent(GuardedIntentMapper.Create(decision,input,knowledge));
   Assert.That(panel.Session.Messages.All(m=>!m.Text.Contains("UNTRUSTED_MODEL_FACT_9999")),Is.True);return accepted;
  }
  [UnityTest]public IEnumerator FollowWaitAnswerAndRefuseUseTraditionalBehaviorAndOnlyAcceptedActionSummaries()
  {
   yield return new EnterPlayMode();yield return Setup();Assert.That(Apply("原地等待","wait"),Is.True);MovePlayer(new Vector3(0,.05f,1));var start=demo.Echo.transform.position;
   Assert.That(Apply("跟随我","follow"),Is.True);yield return Until(()=>Vector3.Distance(start,demo.Echo.transform.position)>.15f,6,"Actual follow movement");Assert.That(Apply("原地待命","wait"),Is.True);
   var stopped=demo.Echo.transform.position;yield return new WaitForSecondsRealtime(.3f);Assert.That(Vector3.Distance(stopped,demo.Echo.transform.position),Is.LessThan(.02f));Assert.That(memory.Store.Events.Count,Is.EqualTo(3));var before=memory.CaptureJson();
   Assert.That(Apply("你是谁","answer"),Is.True);Assert.That(panel.Session.Messages.Last().Text,Is.EqualTo(TrustedReplyPolicy.Persona));Assert.That(Apply("替我打开门","refuse"),Is.True);Assert.That(panel.Session.Messages.Last().IsRefusal,Is.True);
   Assert.That(demo.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(memory.CaptureJson(),Is.EqualTo(before));Assert.That(memory.Store.Characters.Single(c=>c.Kind==CharacterMemoryKind.Agreement).Summary,Does.Contain("待命"));
  }
  [UnityTest]public IEnumerator AuthorizedInvestigateActuallyNavigatesScansAndArchivesKnownSummary()
  {
   yield return new EnterPlayMode();yield return Setup();MovePlayer(new Vector3(7.7f,.05f,1));Assert.That(WorldTarget.Active.TryGet("scan_mark_01",out var target),Is.True);Assert.That(target.GetComponent<ClueSource>().IsRevealed,Is.False);
   Assert.That(Apply("调查储藏室可疑位置","investigate",target.Id),Is.True);Assert.That(controller.LastDispatchedKind,Is.EqualTo(IntentKind.Investigate));
   yield return Until(()=>demo.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered(target.Id),18,"Actual authorized scan");
   Assert.That(target.GetComponent<ClueSource>().IsRevealed,Is.True);Assert.That(memory.Store.TryGetClue(target.Id,out var known),Is.True);Assert.That(known.Kind,Is.EqualTo(MemoryClueKind.Scan));Assert.That(known.Summary.Length,Is.LessThanOrEqualTo(120));Assert.That(memory.CaptureJson(),Does.Not.Contain("UNTRUSTED_MODEL_FACT_9999"));
  }
  [UnityTest]public IEnumerator WarningStillRequiresTraditionalPerceptionAndRejectedExecutionWritesNoSuccess()
  {
   yield return new EnterPlayMode();yield return Setup();Assert.That(WorldTarget.Active.TryGet("power_panel_01",out var target),Is.True);var pose=target.transform.localToWorldMatrix;var permissions=target.GetComponent<EchoTargetPermissions>();var investigate=permissions.CanInvestigate;var danger=permissions.IsDanger;var navigation=demo.Echo.GetComponent<EchoNavigation>();
   Assert.That(navigation.TryBind(new Vector3(-3,0,-2)),Is.True);scene.GetPhysicsScene().Simulate(.02f);var before=memory.CaptureJson();Assert.That(Apply("警告供电面板","warn",target.Id),Is.False);Assert.That(controller.LastRejection,Is.EqualTo(IntentRejection.ExecutorRejected));Assert.That(memory.CaptureJson(),Is.EqualTo(before));
   Assert.That(navigation.TryBind(new Vector3(3.8f,0,-1.5f)),Is.True);var direction=target.transform.position-demo.Echo.transform.position;direction.y=0;demo.Echo.transform.rotation=Quaternion.LookRotation(direction);scene.GetPhysicsScene().Simulate(.02f);
   Assert.That(Apply("警告供电面板","warn",target.Id),Is.True);Assert.That(demo.Echo.CurrentMode,Is.EqualTo(IntentKind.Warn));Assert.That(memory.Store.Characters.Any(c=>c.Kind==CharacterMemoryKind.KnownDanger&&c.TargetId==target.Id),Is.True);
   Assert.That(target.transform.localToWorldMatrix,Is.EqualTo(pose));Assert.That(permissions.CanInvestigate,Is.EqualTo(investigate));Assert.That(permissions.IsDanger,Is.EqualTo(danger));Assert.That(memory.Store.Events.Count,Is.EqualTo(1));
  }
  [UnityTearDown]public IEnumerator Cleanup()
  {
   if(controller)controller.enabled=false;bridge?.Dispose();bridge=null;if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}
   if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var clock=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(clock.Elapsed.TotalSeconds>5)throw new TimeoutException("AI execution fixture unload");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
  }
 }
}
