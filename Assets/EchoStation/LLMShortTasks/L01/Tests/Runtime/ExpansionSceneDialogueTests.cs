using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.SalvageExpansion;
using EchoStation.Tests.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.L01.Tests
{
    public sealed class ExpansionSceneDialogueTests
    {
        Scene previous,scene;SyntheticInputScope input;ExpansionWorld world;ExpansionEcho echo;ExpansionDialoguePanel panel;ExpansionStreaming streaming;bool background;float scale;
        IEnumerator Wait(Func<bool> condition,float seconds,string reason)
        {float deadline=Time.realtimeSinceStartup+seconds;while(!condition()){Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline),reason);yield return null;}}
        IEnumerator Boot()
        {
            background=Application.runInBackground;Application.runInBackground=true;scale=Time.timeScale;previous=SceneManager.GetActiveScene();input=new SyntheticInputScope();var load=EditorSceneManager.LoadSceneAsyncInPlayMode(ExpansionDialogueBootstrap.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!load.isDone)yield return null;scene=SceneManager.GetSceneByPath(ExpansionDialogueBootstrap.ScenePath);SceneManager.SetActiveScene(scene);
            world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ExpansionWorld>()).Single();echo=world.GetComponent<ExpansionEcho>();streaming=world.GetComponent<ExpansionStreaming>();world.GetComponent<ExpansionPersistence>().ConfigureTests(Path.GetFullPath("Library/EchoStation/SalvageExpansion/Tests/L01_"+Guid.NewGuid().ToString("N")));
            yield return Wait(()=>streaming.Owner!=null&&!streaming.Blocking,25,"Initial actual room loaded");panel=world.GetComponent<ExpansionDialoguePanel>();Assert.That(panel,Is.Not.Null,"Runtime bootstrap attaches communication to the actual scene");Assert.That(world.GetComponents<ExpansionDialoguePanel>().Length,Is.EqualTo(1));world.Input.SetApplicationFocus(true);world.Input.SetPointerCaptured(true);yield return null;
        }
        IEnumerator Press(Key key)
        {InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState());yield return null;InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState(key));yield return null;InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState());yield return null;}
        [UnityTest]public IEnumerator ActualSceneTUsesChineseInputAndBlocksSettingsWithoutChangingInventoryOrDevices()
        {
            yield return new EnterPlayMode();yield return Boot();long revision=world.Inventory.Revision;var facility=world.GetComponent<FacilityController>();yield return Press(Key.T);yield return Wait(()=>panel.InputField.isFocused,5,"Text input focused");Assert.That(panel.IsOpen,Is.True);Assert.That(world.Pause.Paused,Is.True);Assert.That(world.GetComponent<ExpansionGraphics>().Open(),Is.False);Assert.That(world.Backpack.Open(),Is.False);Assert.That(echo.OpenMenu(),Is.False);
            InputSystem.QueueTextEvent(input.Keyboard,'电');InputSystem.QueueTextEvent(input.Keyboard,'量');yield return null;yield return Press(Key.Enter);Assert.That(panel.LastReply,Does.Contain("当前电量"));Assert.That(panel.MessageCount,Is.EqualTo(2));Assert.That(panel.IsOpen,Is.True);Assert.That(world.Inventory.Revision,Is.EqualTo(revision));Assert.That(facility.Isolated||facility.ValveClosed||facility.Clamps!=0,Is.False);Assert.That(GameRuntimeOptions.UseLLM,Is.False);
            panel.SubmitText("原地待命");Assert.That(panel.IsOpen,Is.False);Assert.That(echo.Mode,Is.EqualTo(IntentKind.Wait));Assert.That(world.Pause.Blocked,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator ActualSceneTextInvestigationTravelsScansAndReportsWithoutClosingValve()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(echo.Command(ScriptCommands.Wait()),Is.True);world.Motor.RestorePose(new Vector3(15,.15f,6),0,0);streaming.EvaluateNow();yield return Wait(()=>!streaming.Blocking,25,"Storage room preloaded");world.Input.SetApplicationFocus(true);world.Input.SetPointerCaptured(true);
            var facility=world.GetComponent<FacilityController>();var valve=facility.Devices.Single(d=>d.Kind==FacilityDeviceKind.Valve);var delta=valve.transform.position-world.Motor.View.position;world.Motor.RestorePose(world.transform.position,Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg,-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg);Physics.SyncTransforms();
            Assert.That(panel.Open(),Is.True);panel.SubmitText("调查这个");Assert.That(panel.IsOpen,Is.False);Assert.That(echo.CurrentTarget,Is.EqualTo(valve.Id),panel.LastReply);yield return Wait(()=>echo.TryReadKnownReport(valve.Id,out _,out _),18,"Echo actually reaches and scans the selected valve");Assert.That(facility.ValveClosed,Is.False);
            Assert.That(panel.Open(),Is.True);panel.SubmitText("报告"+valve.DisplayName);Assert.That(panel.LastReply,Does.Contain("扳手"));Assert.That(panel.IsOpen,Is.True);Assert.That(facility.ValveClosed,Is.False);
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;if(panel)panel.Close();if(input!=null)InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState());var owner=streaming?streaming.Owner:null;if(previous.IsValid())SceneManager.SetActiveScene(previous);
            if(scene.IsValid()&&scene.isLoaded){var unload=SceneManager.UnloadSceneAsync(scene);while(!unload.isDone)yield return null;}if(owner!=null)yield return Wait(()=>owner.IsClosed,25,"Addressables owner fully retired");
            bool timeRestored=Time.timeScale==scale;input?.Dispose();if(input!=null)Assert.That(input.IsRestored,Is.True);input=null;Time.timeScale=scale;Application.runInBackground=background;Assert.That(timeRestored,Is.True,"Scene UI releases its actual time lease before fallback cleanup");yield return new ExitPlayMode();
        }
    }
}
