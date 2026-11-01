using System;
using System.Collections;
using System.Linq;
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
namespace EchoStation.Tests.Station
{
 public abstract class StationPlayFixture
 {
  protected Scene scene;protected StationSession station;protected SyntheticInputScope inputs;private Scene previous;private bool background,capturedBackground;private UnityEngine.AudioListener[] muted=System.Array.Empty<UnityEngine.AudioListener>();
  protected IEnumerator Boot(){if(!Application.isPlaying)throw new InvalidOperationException("Caller must enter PlayMode at root before Boot.");background=Application.runInBackground;capturedBackground=true;Application.runInBackground=true;previous=SceneManager.GetActiveScene();inputs=new SyntheticInputScope();muted=UnityEngine.Object.FindObjectsByType<UnityEngine.AudioListener>(UnityEngine.FindObjectsSortMode.None).Where(l=>l.enabled).ToArray();foreach(var l in muted){TestContext.WriteLine("Muted foreign listener: "+l.gameObject.scene.path+"/"+l.name);l.enabled=false;}scene=EditorSceneManager.LoadSceneInPlayMode(StationLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));yield return Until(()=>scene.isLoaded,6,"Scene load timeout");SceneManager.SetActiveScene(scene);station=scene.GetRootGameObjects().Single(r=>r.GetComponent<StationSession>()).GetComponent<StationSession>();yield return Until(()=>station.Ready,6,"Startup timeout");station.Echo.AutomaticWarningsEnabled=false;station.Echo.TrySubmit(ScriptCommands.Wait(),out _);var input=station.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);scene.GetPhysicsScene().Simulate(.02f);}
  protected static IEnumerator Until(Func<bool> condition,double seconds,string message){var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){if(timer.Elapsed.TotalSeconds>seconds)Assert.Fail(message);yield return R04ShadowProbe.Frame();}}
  protected void Move(Vector3 point){var cc=station.GetComponent<CharacterController>();cc.enabled=false;station.transform.position=point;cc.enabled=true;station.Player.ResetMotion();Physics.SyncTransforms();scene.GetPhysicsScene().Simulate(.02f);}
  protected void Look(Vector3 target){var d=target-station.Player.View.position;station.Player.SetLookAngles(Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg);}
  protected IEnumerator Press(params Key[] keys){InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(keys));yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return R04ShadowProbe.Frame();}
  protected IEnumerator Hold(float seconds,params Key[] keys){InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(keys));var timer=System.Diagnostics.Stopwatch.StartNew();while(timer.Elapsed.TotalSeconds<seconds)yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return R04ShadowProbe.Frame();}
  [UnityTearDown]public IEnumerator Cleanup(){foreach(var l in muted)if(l)l.enabled=true;muted=System.Array.Empty<UnityEngine.AudioListener>();if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var op=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;while(op!=null&&!op.isDone)yield return null;if(capturedBackground){Application.runInBackground=background;capturedBackground=false;}yield return new ExitPlayMode();}else if(capturedBackground){Application.runInBackground=background;capturedBackground=false;}}
 }
}
