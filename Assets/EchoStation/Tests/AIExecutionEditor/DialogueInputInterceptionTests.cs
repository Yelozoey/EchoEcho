using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using EchoStation.Dialogue.Commands;
using EchoStation.Dialogue.Integration;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Memory.Integration;
using EchoStation.Player;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public sealed class DialogueInputInterceptionTests
 {
  private Scene scene,previous;private bool background;private SyntheticInputScope inputs;private CabinDemo demo;private DialoguePanel panel;private ScriptDialogueController controller;private MemorySession memory;private JournalMemoryBridge bridge;
  private static IEnumerator Until(Func<bool> condition,double seconds,string label)
  {var clock=System.Diagnostics.Stopwatch.StartNew();while(!condition()){if(clock.Elapsed.TotalSeconds>seconds)Assert.Fail(label+" timeout");yield return R04ShadowProbe.Frame();}}
  private IEnumerator Setup()
  {
   background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();inputs=new SyntheticInputScope();scene=EditorSceneManager.LoadSceneInPlayMode("Assets/EchoStation/World/Scenes/ES_Cabin_Demo.unity",new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));yield return Until(()=>scene.isLoaded,5,"Interception cabin load");SceneManager.SetActiveScene(scene);demo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CabinDemo>()).Single();yield return Until(()=>demo.Ready,5,"Interception cabin ready");demo.Echo.AutomaticWarningsEnabled=false;demo.Echo.TrySubmit(ScriptCommands.Wait(),out _);
   var reader=demo.GetComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);panel=demo.gameObject.AddComponent<DialoguePanel>();memory=new MemorySession();bridge=new JournalMemoryBridge(demo.GetComponent<PlayerJournal>().Store,memory.Store);controller=demo.gameObject.AddComponent<ScriptDialogueController>();controller.Configure(panel,demo.Echo,memory,bridge,()=>Array.Empty<ScriptTarget>());Assert.That(panel.SetOpen(true),Is.True);yield return R04ShadowProbe.Frame();
  }
  private void Send(string input){panel.InputField.text=input;Assert.That(panel.SubmitInput(),Is.True);}
  [UnityTest]public IEnumerator InputIsConsumedExactlyOnceAndFalseKeepsOriginalScriptRoute()
  {
   yield return new EnterPlayMode();yield return Setup();var received=0;controller.InputInterceptor=message=>{received++;Assert.That(message.Text,Is.EqualTo("跟随我"));return true;};Send("跟随我");Assert.That(received,Is.EqualTo(1));Assert.That(demo.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(memory.Store.Events,Is.Empty);
   controller.InputInterceptor=_=>{received++;return false;};Send("跟随我");Assert.That(received,Is.EqualTo(2));Assert.That(demo.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(memory.Store.Events.Count,Is.EqualTo(1));controller.InputInterceptor=null;Send("原地待命");Assert.That(memory.Store.Events.Count,Is.EqualTo(2));Assert.That(demo.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));
  }
  [UnityTest]public IEnumerator InterceptorFailureDoesNotReplayInputIntoScriptOrRecordAction()
  {
   yield return new EnterPlayMode();yield return Setup();controller.InputInterceptor=_=>throw new InvalidOperationException("fixture interceptor failure");LogAssert.Expect(LogType.Exception,new Regex("InvalidOperationException: fixture interceptor failure"));Send("跟随我");Assert.That(demo.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(memory.Store.Events,Is.Empty);Assert.That(panel.Session.Messages.Last().IsRefusal,Is.True);Assert.That(panel.Session.Messages.Last().Text,Is.EqualTo("通信请求未能处理，请重试。"));
  }
  [UnityTest]public IEnumerator ManualIntentEventOccursBeforeExecutionAndDisabledControllerDoesNotConsumeText()
  {
   yield return new EnterPlayMode();yield return Setup();var manual=0;controller.PlayerIntentRequested+=request=>{manual++;Assert.That(demo.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(request.Kind,Is.EqualTo(IntentKind.Follow));};Assert.That(controller.SubmitPlayerIntent(ScriptCommands.Follow()),Is.True);Assert.That(manual,Is.EqualTo(1));
   var received=0;controller.InputInterceptor=_=>{received++;return true;};controller.enabled=false;Send("原地待命");Assert.That(received,Is.Zero);Assert.That(demo.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));controller.enabled=true;Send("原地待命");Assert.That(received,Is.EqualTo(1));Assert.That(manual,Is.EqualTo(1));
  }
  [UnityTearDown]public IEnumerator Cleanup()
  {
   if(controller){controller.InputInterceptor=null;controller.enabled=false;}bridge?.Dispose();bridge=null;if(panel&&panel.IsOpen)panel.SetOpen(false);if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}
   if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var clock=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(clock.Elapsed.TotalSeconds>5)throw new TimeoutException("Interception fixture unload");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
  }
 }
}
