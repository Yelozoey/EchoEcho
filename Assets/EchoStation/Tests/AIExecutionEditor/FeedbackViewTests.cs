using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.AI.Execution;
using EchoStation.AI.Presentation;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Player;
using EchoStation.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public sealed class FeedbackViewTests
 {
  private Scene scene,previous;private bool background;private SyntheticInputScope inputs;private CabinDemo demo;private DialoguePanel panel;private LlmFeedbackView view;private EchoPresentation presentation;private EchoRig rig;
  private static IEnumerator Until(Func<bool> condition,double seconds,string label)
  {var clock=System.Diagnostics.Stopwatch.StartNew();while(!condition()){if(clock.Elapsed.TotalSeconds>seconds)Assert.Fail(label+" timeout");yield return R04ShadowProbe.Frame();}}
  private IEnumerator Setup()
  {
   background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();inputs=new SyntheticInputScope();scene=EditorSceneManager.LoadSceneInPlayMode("Assets/EchoStation/World/Scenes/ES_Cabin_Demo.unity",new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));yield return Until(()=>scene.isLoaded,5,"Feedback cabin load");SceneManager.SetActiveScene(scene);
   demo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CabinDemo>()).Single();yield return Until(()=>demo.Ready,5,"Feedback cabin ready");demo.Echo.AutomaticWarningsEnabled=false;demo.Echo.TrySubmit(ScriptCommands.Wait(),out _);var reader=demo.GetComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);
   panel=demo.gameObject.AddComponent<DialoguePanel>();presentation=demo.Echo.GetComponent<EchoPresentation>();rig=demo.Echo.GetComponent<EchoRig>();view=demo.gameObject.AddComponent<LlmFeedbackView>();view.Configure(panel,presentation);Assert.That(panel.SetOpen(true),Is.True);yield return R04ShadowProbe.Frame();
  }
  [UnityTest]public IEnumerator RealStatusCancelAndHiddenPanelKeepInferenceOwnershipAndModality()
  {
   yield return new EnterPlayMode();yield return Setup();var cancels=0;view.CancelRequested+=()=>cancels++;view.SetPhase(FeedbackPhase.Loading);Assert.That(presentation.InferenceThinking,Is.True);Assert.That(view.CancelButton.interactable,Is.True);view.CancelButton.onClick.Invoke();view.CancelButton.onClick.Invoke();Assert.That(cancels,Is.EqualTo(1));Assert.That(view.CancelButton.interactable,Is.False);
   view.SetPhase(FeedbackPhase.Thinking);Assert.That(panel.SetOpen(false),Is.True);Assert.That(view.StatusText.gameObject.activeInHierarchy,Is.False);Assert.That(presentation.InferenceThinking,Is.True);view.CancelButton.onClick.Invoke();Assert.That(cancels,Is.EqualTo(1));Assert.That(view.Phase,Is.EqualTo(FeedbackPhase.Thinking));
   Assert.That(panel.SetOpen(true),Is.True);Assert.That(demo.GetComponent<PlayerInputReader>().UiBlocked,Is.True);view.CancelButton.onClick.Invoke();Assert.That(cancels,Is.EqualTo(2));view.SetPhase(FeedbackPhase.Draining);Assert.That(view.CancelButton.interactable,Is.False);Assert.That(presentation.InferenceThinking,Is.True);
   view.SetPhase(FeedbackPhase.Ready);Assert.That(presentation.InferenceThinking,Is.False);view.SetPhase(FeedbackPhase.Failure);Assert.That(presentation.InferenceThinking,Is.False);Assert.That(view.StatusText.text,Is.EqualTo(InferenceFeedback.For(FeedbackPhase.Failure).Message));
   view.SetPhase(FeedbackPhase.Thinking);yield return R04ShadowProbe.Frame();CaptureUi();
   view.enabled=false;Assert.That(presentation.InferenceThinking,Is.False);Assert.That(view.StatusText.gameObject.activeInHierarchy,Is.False);Assert.That(cancels,Is.EqualTo(2));view.enabled=true;Assert.That(presentation.InferenceThinking,Is.True);view.SetPhase(FeedbackPhase.Script);yield return R04ShadowProbe.Frame();Assert.That(presentation.Mood,Is.EqualTo(EchoMood.Calm));
  }
  private void CaptureUi()
  {
   // Editor test runner does not guarantee an OS backbuffer present/end-of-frame.
   // Render the same live Canvas through the real URP camera, restoring all transient values.
   var camera=demo.GetComponentInChildren<Camera>();var additional=camera.GetUniversalAdditionalCameraData();var post=additional.renderPostProcessing;var canvas=panel.Canvas;var mode=canvas.renderMode;var world=canvas.worldCamera;var distance=canvas.planeDistance;var target=camera.targetTexture;var aspect=camera.aspect;var active=RenderTexture.active;
   var output=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
   try
   {
    output.Create();camera.targetTexture=output;camera.aspect=16f/9f;additional.renderPostProcessing=false;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();
    RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=output});RenderTexture.active=output;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0,false);pixels.Apply(false);
    var directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Tests/Stage3/Evidence/S3-E02b"));Directory.CreateDirectory(directory);File.WriteAllBytes(Path.Combine(directory,"FeedbackUI_RT_"+DateTime.UtcNow.Ticks+".png"),pixels.EncodeToPNG());
    Assert.That(view.StatusText.preferredHeight,Is.LessThanOrEqualTo(view.StatusText.rectTransform.rect.height+.001f));
   }
   finally
   {canvas.renderMode=mode;canvas.worldCamera=world;canvas.planeDistance=distance;camera.targetTexture=target;camera.aspect=aspect;additional.renderPostProcessing=post;RenderTexture.active=active;output.Release();UnityEngine.Object.Destroy(output);UnityEngine.Object.Destroy(pixels);Canvas.ForceUpdateCanvases();}
   Assert.That(canvas.renderMode,Is.EqualTo(mode));Assert.That(camera.targetTexture,Is.SameAs(target));
  }
  [UnityTest]public IEnumerator ThinkingActuallyBlinksMovesProbeAndPreservesWarningAndScannerPriority()
  {
   yield return new EnterPlayMode();yield return Setup();view.SetPhase(FeedbackPhase.Thinking);var start=rig.ProbePitch.localRotation;var lit=false;var dark=false;var timer=System.Diagnostics.Stopwatch.StartNew();
   while(timer.Elapsed.TotalSeconds<.8){yield return R04ShadowProbe.Frame();lit|=rig.Status.enabled;dark|=!rig.Status.enabled;}
   Assert.That(lit&&dark,Is.True);Assert.That(presentation.Mood,Is.EqualTo(EchoMood.Thinking));Assert.That(rig.Status.sharedMaterial,Is.SameAs(rig.Thinking));Assert.That(Quaternion.Angle(start,rig.ProbePitch.localRotation),Is.GreaterThan(1));
   Assert.That(WorldTarget.Active.TryGet("power_panel_01",out var danger),Is.True);Assert.That(demo.Echo.GetComponent<EchoNavigation>().TryBind(new Vector3(3.8f,0,-1.5f)),Is.True);var toward=danger.transform.position-demo.Echo.transform.position;toward.y=0;demo.Echo.transform.rotation=Quaternion.LookRotation(toward);scene.GetPhysicsScene().Simulate(.02f);Assert.That(demo.Echo.TrySubmit(ScriptCommands.Warn(danger.Id),out _),Is.True);yield return R04ShadowProbe.Frame();Assert.That(presentation.Mood,Is.EqualTo(EchoMood.Alert));Assert.That(rig.Status.sharedMaterial,Is.SameAs(rig.Alert));Assert.That(rig.Status.enabled,Is.True);
   demo.Echo.TrySubmit(ScriptCommands.Wait(),out _);var collider=demo.GetComponent<CharacterController>();collider.enabled=false;demo.transform.position=new Vector3(7.7f,.05f,1);collider.enabled=true;scene.GetPhysicsScene().Simulate(.02f);Assert.That(demo.Echo.TrySubmit(ScriptCommands.Investigate("scan_mark_01"),out _),Is.True);var scanner=demo.Echo.GetComponent<EchoScanController>().Scanner;yield return Until(()=>scanner.IsBusy,15,"Real scanner becomes busy");
   var owned=rig.ProbePitch.localRotation;presentation.SendMessage("LateUpdate");Assert.That(Quaternion.Angle(owned,rig.ProbePitch.localRotation),Is.LessThan(.001f));Assert.That(presentation.ScanLight.enabled,Is.True);scanner.Cancel();demo.Echo.TrySubmit(ScriptCommands.Wait(),out _);view.SetPhase(FeedbackPhase.Failure);yield return R04ShadowProbe.Frame();Assert.That(presentation.InferenceThinking,Is.False);Assert.That(presentation.Mood,Is.EqualTo(EchoMood.Calm));
  }
  [UnityTearDown]public IEnumerator Cleanup()
  {
   if(view)view.enabled=false;if(panel&&panel.IsOpen)panel.SetOpen(false);if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}
   if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var clock=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(clock.Elapsed.TotalSeconds>5)throw new TimeoutException("Feedback fixture unload");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
  }
 }
}
