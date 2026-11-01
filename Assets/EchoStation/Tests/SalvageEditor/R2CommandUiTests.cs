using System;
using System.Collections;
using System.Linq;
using EchoStation.Dialogue;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage.Editor;
using EchoStation.Tests.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace EchoStation.Salvage.Tests
{
    public class R2CommandUiTests
    {
        Scene old,scene;bool background;SyntheticInputScope devices;SalvageCommandPanel panel;SalvageCommandHub hub;SalvageMotor motor;PlayerInputReader input;
        static IEnumerator Frame(){int start=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.frameCount<=start){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Actual frame timeout");yield return null;}}
        IEnumerator Boot(){Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>8)Assert.Fail("UI load timeout");yield return null;}SceneManager.SetActiveScene(scene);devices=new SyntheticInputScope();yield return Frame();motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;panel=motor.GetComponent<SalvageCommandPanel>();hub=motor.GetComponent<SalvageCommandHub>();input=motor.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);}
        IEnumerator KeyPress(Key key){InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState());yield return Frame();InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState(key));yield return Frame();InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState());yield return Frame();}
        IEnumerator Click(Button button){Canvas.ForceUpdateCanvases();var p=RectTransformUtility.WorldToScreenPoint(null,button.GetComponent<RectTransform>().TransformPoint(button.GetComponent<RectTransform>().rect.center));InputSystem.QueueStateEvent(devices.Mouse,new MouseState{position=p});yield return Frame();InputSystem.QueueStateEvent(devices.Mouse,new MouseState{position=p}.WithButton(MouseButton.Left));yield return Frame();InputSystem.QueueStateEvent(devices.Mouse,new MouseState{position=p});yield return Frame();}
        [UnityTest]public IEnumerator RKeyboardAndActualMouseButtonsBlockPlayerAndDispatchSixIntents()
        {
            yield return new EnterPlayMode();yield return Boot();IntentKind? accepted=null;hub.Execute=r=>{accepted=r.Kind;return true;};int cancelled=0;hub.Cancel=()=>{cancelled++;return true;};
            yield return KeyPress(Key.R);Assert.That(panel.IsOpen,Is.True);Assert.That(input.UiBlocked,Is.True);Assert.That(input.CanPlay,Is.False);
            yield return Click(panel.WaitButton);Assert.That(accepted,Is.EqualTo(IntentKind.Wait));yield return Click(panel.FollowButton);Assert.That(accepted,Is.EqualTo(IntentKind.Follow));yield return Click(panel.CancelButton);Assert.That(cancelled,Is.EqualTo(1));
            yield return KeyPress(Key.Escape);Assert.That(panel.IsOpen,Is.False);Assert.That(input.UiBlocked,Is.False);Assert.That(input.PointerCaptured,Is.True);
        }
        [UnityTest]public IEnumerator PointedTargetAndTaskBoardOfferNoHiddenTargets()
        {
            yield return new EnterPlayMode();yield return Boot();motor.RestorePose(new Vector3(9,.1f,17),0,35);Physics.SyncTransforms();Assert.That(hub.PointedId(),Is.EqualTo("cargo_a"));string accepted=null;hub.Execute=r=>{accepted=r.TargetName;return true;};Assert.That(panel.Open(false),Is.True);yield return Click(panel.InvestigateButton);Assert.That(accepted,Is.EqualTo("cargo_a"));Assert.That(panel.TargetButtons.Count(b=>b.gameObject.activeSelf),Is.EqualTo(1));panel.Close();
            hub.Known.CommitValidatedSensor("cargo_b");motor.RestorePose(new Vector3(5,.1f,12),270,0);Physics.SyncTransforms();var mission=motor.GetComponent<SalvageMission>();Assert.That(mission.OpenBoard(),Is.True);var button=motor.GetComponentsInChildren<Button>(true).Single(b=>b.name=="EchoTargets");yield return Click(button);Assert.That(panel.IsOpen,Is.True);Assert.That(mission.BoardOpen,Is.False);Assert.That(panel.TargetButtons.Count(b=>b.gameObject.activeSelf),Is.EqualTo(1));Assert.That(panel.TargetButtons[0].GetComponentInChildren<Text>().text,Does.Contain("备件"));
        }
        [UnityTest]public IEnumerator TextAndImeUseRealFieldWithBudgetsAndHistoryCap()
        {
            yield return new EnterPlayMode();yield return Boot();yield return KeyPress(Key.T);Assert.That(panel.TextMode,Is.True);yield return Frame();yield return Frame();Assert.That(panel.InputField.isFocused,Is.True);
            var composition=IMECompositionEvent.Create(devices.Keyboard.deviceId,"diao",InputState.currentTime);InputSystem.QueueEvent(ref composition);yield return Frame();yield return KeyPress(Key.Enter);Assert.That(hub.History.Messages,Is.Empty);Assert.That(panel.IsOpen,Is.True);
            var end=IMECompositionEvent.Create(devices.Keyboard.deviceId,"",InputState.currentTime);InputSystem.QueueEvent(ref end);yield return Frame();foreach(var c in "电量")InputSystem.QueueTextEvent(devices.Keyboard,c);yield return Frame();yield return Frame();yield return KeyPress(Key.Enter);Assert.That(hub.History.Messages.Count,Is.EqualTo(2));Assert.That(hub.History.Messages.Last().Text,Does.Contain("电量"));
            foreach(var c in new string('查',205))InputSystem.QueueTextEvent(devices.Keyboard,c);yield return Frame();Assert.That(panel.InputField.text.Length,Is.EqualTo(200));Assert.That(hub.Submit(new string('字',201)),Is.False);
            panel.InputField.text="";for(int i=0;i<60;i++)Assert.That(hub.Submit("电量"),Is.True);yield return Frame();Assert.That(hub.History.Messages.Count,Is.EqualTo(100));Assert.That(hub.History.CaptureRecent().Count,Is.EqualTo(50));Assert.That(panel.DisplayedMessages,Is.EqualTo(100));Assert.That(hub.History.Messages.Where(m=>m.Speaker==DialogueSpeaker.Echo).All(m=>m.Text.Length<=280),Is.True);
            yield return KeyPress(Key.Escape);Assert.That(panel.IsOpen,Is.False);Assert.That(panel.InputField.text,Is.Empty);
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(panel)panel.Close();if(devices!=null){devices.Dispose();Assert.That(devices.IsRestored,Is.True);devices=null;}if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
