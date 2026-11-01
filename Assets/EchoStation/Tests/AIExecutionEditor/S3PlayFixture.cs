using System;
using System.Collections;
using System.Linq;
using EchoStation.AI.GameIntegration;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Station;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public abstract class S3PlayFixture
 {
  protected Scene scene;protected StationSession station;protected SyntheticInputScope inputs;private Scene previous;private bool background,captured;private AudioListener[] muted=Array.Empty<AudioListener>();
  protected IEnumerator BootS3()
  {
   if(!Application.isPlaying)throw new InvalidOperationException("Enter PlayMode before S3 boot");background=Application.runInBackground;captured=true;Application.runInBackground=true;previous=SceneManager.GetActiveScene();inputs=new SyntheticInputScope();muted=UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Where(a=>a.enabled).ToArray();foreach(var listener in muted)listener.enabled=false;
   scene=EditorSceneManager.LoadSceneInPlayMode(S3Bootstrap.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));yield return Until(()=>scene.isLoaded,6,"S3 scene load");SceneManager.SetActiveScene(scene);station=scene.GetRootGameObjects().Single(r=>r.GetComponent<StationSession>()).GetComponent<StationSession>();yield return Until(()=>station.Ready&&station.GetComponent<S3DialogueDriver>().Ready,6,"S3 ready");
   station.Echo.AutomaticWarningsEnabled=false;station.Echo.TrySubmit(ScriptCommands.Wait(),out _);var reader=station.GetComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);scene.GetPhysicsScene().Simulate(.02f);
  }
  protected static IEnumerator Until(Func<bool> condition,double seconds,string label)
  {var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){if(timer.Elapsed.TotalSeconds>seconds)Assert.Fail(label+" timeout");yield return R04ShadowProbe.Frame();}}
  protected void Move(Vector3 point){var controller=station.GetComponent<CharacterController>();controller.enabled=false;station.transform.position=point;controller.enabled=true;station.Player.ResetMotion();Physics.SyncTransforms();scene.GetPhysicsScene().Simulate(.02f);}
  protected void Look(Vector3 point){var d=point-station.Player.View.position;station.Player.SetLookAngles(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg);}
  protected IEnumerator Press(params Key[] keys){InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(keys));yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return R04ShadowProbe.Frame();}
  protected IEnumerator Hold(float seconds,params Key[] keys){InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(keys));var timer=System.Diagnostics.Stopwatch.StartNew();while(timer.Elapsed.TotalSeconds<seconds)yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return R04ShadowProbe.Frame();}
  [UnityTearDown]public IEnumerator CleanupS3()
  {
   if(station){var driver=station.GetComponent<S3DialogueDriver>();if(driver&&driver.Ready){driver.enabled=false;var session=driver.Session;session.Dispose();yield return Until(()=>session.CleanupComplete,40,"S3 Owner release before scene unload");}}
   foreach(var listener in muted)if(listener)listener.enabled=true;muted=Array.Empty<AudioListener>();if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}
   if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var unload=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(unload!=null&&!unload.isDone){if(timer.Elapsed.TotalSeconds>8)throw new TimeoutException("S3 unload");yield return null;}if(captured){Application.runInBackground=background;captured=false;}yield return new ExitPlayMode();}else if(captured){Application.runInBackground=background;captured=false;}
  }
 }
}
