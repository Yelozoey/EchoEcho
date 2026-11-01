using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.AI.Inference;
using EchoStation.Echo;
using EchoStation.L01;
using EchoStation.SalvageExpansion;
using EchoStation.Tests.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace EchoStation.L02.Tests
{
    public class SceneQuestionSceneTests
    {
        Scene previous,scene;SyntheticInputScope input;ExpansionWorld world;ExpansionEcho echo;ExpansionDialoguePanel panel;SceneQuestionBridge bridge;ExpansionStreaming streaming;bool background;float scale;
        IEnumerator Wait(Func<bool> ready,float seconds=25){float deadline=Time.realtimeSinceStartup+seconds;while(!ready()){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline));yield return null;}}
        [UnityTest]public IEnumerator ActualSceneBootstrapsOffThenScansAndRoutesQuestionWithoutDeviceWrites()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;scale=Time.timeScale;previous=SceneManager.GetActiveScene();input=new SyntheticInputScope();var load=EditorSceneManager.LoadSceneAsyncInPlayMode(ExpansionDialogueBootstrap.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!load.isDone)yield return null;scene=SceneManager.GetSceneByPath(ExpansionDialogueBootstrap.ScenePath);SceneManager.SetActiveScene(scene);
            world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ExpansionWorld>()).Single();echo=world.GetComponent<ExpansionEcho>();streaming=world.GetComponent<ExpansionStreaming>();world.GetComponent<ExpansionPersistence>().ConfigureTests(Path.GetFullPath("Library/EchoStation/SalvageExpansion/Tests/L02_"+Guid.NewGuid().ToString("N")));yield return Wait(()=>streaming.Owner!=null&&!streaming.Blocking);panel=world.GetComponent<ExpansionDialoguePanel>();bridge=world.GetComponent<SceneQuestionBridge>();Assert.That(bridge,Is.Not.Null);Assert.That(world.GetComponents<SceneQuestionBridge>().Length,Is.EqualTo(1));Assert.That(bridge.AIEnabled,Is.False);Assert.That(bridge.ToggleButton,Is.Not.Null);
            Object.Destroy(bridge);yield return null;bridge=world.gameObject.AddComponent<SceneQuestionBridge>();bridge.ConfigureTests(new QuestionTestFactory());bridge.Initialize();world.Input.SetApplicationFocus(true);world.Input.SetPointerCaptured(true);Assert.That(echo.Command(ScriptCommands.Wait()),Is.True);world.Motor.RestorePose(new Vector3(15,.15f,6),0,0);streaming.EvaluateNow();yield return Wait(()=>!streaming.Blocking);world.Input.SetApplicationFocus(true);world.Input.SetPointerCaptured(true);
            var facility=world.GetComponent<FacilityController>();var valve=facility.Devices.Single(d=>d.Kind==FacilityDeviceKind.Valve);var delta=valve.transform.position-world.Motor.View.position;world.Motor.RestorePose(world.transform.position,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg);Physics.SyncTransforms();Assert.That(panel.Open(),Is.True);panel.SubmitText("调查这个");yield return Wait(()=>echo.TryReadKnownReport(valve.Id,out _,out _),18);Assert.That(panel.Open(),Is.True);Assert.That(bridge.SetAIEnabled(true),Is.True);yield return Wait(()=>bridge.Phase==RequestPhase.Ready,5);long inventory=world.Inventory.Revision;panel.SubmitText("这个应该如何处理？");yield return Wait(()=>bridge.LastReply!=null,5);Assert.That(bridge.LastReply.Success,Is.True,bridge.LastReply.Error);Assert.That(bridge.SourceText,Does.Contain("扳手"));Assert.That(facility.ValveClosed,Is.False);Assert.That(world.Inventory.Revision,Is.EqualTo(inventory));Assert.That(panel.IsOpen,Is.True);Assert.That(Time.timeScale,Is.Zero);
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;if(bridge){bridge.SetAIEnabled(false);yield return Wait(()=>bridge.CleanupComplete,10);}if(panel)panel.Close();var owner=streaming?streaming.Owner:null;if(previous.IsValid())SceneManager.SetActiveScene(previous);if(scene.IsValid()&&scene.isLoaded){var unload=SceneManager.UnloadSceneAsync(scene);while(!unload.isDone)yield return null;}if(owner!=null)yield return Wait(()=>owner.IsClosed);input?.Dispose();if(input!=null)Assert.That(input.IsRestored,Is.True);Time.timeScale=scale;Application.runInBackground=background;yield return new ExitPlayMode();
        }
    }
}
