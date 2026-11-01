using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.SalvageNext.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextPanelTests
    {
        Scene old,scene;bool background;SalvageMotor motor;SalvageCommandHub hub;SalvageEcho echo;SalvageEchoCoordinator brain;NextSession session;NextAdvice advice;NextCommandPanel panel;PlayerInputReader input;
        IEnumerator Boot()
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(NextLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;
            motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;input=motor.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);hub=motor.GetComponent<SalvageCommandHub>();session=motor.GetComponent<NextSession>();advice=motor.GetComponent<NextAdvice>();panel=motor.GetComponent<NextCommandPanel>();
            echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;echo.enabled=false;Assert.That(brain.Initialize(),Is.True);brain.AutomaticWarnings=false;brain.ResetBase(IntentKind.Wait);
            var seed=session.Capture();seed.World.Collaboration.Discovered=new[]{"cargo_a","cargo_b","cargo_e"};Assert.That(session.TryRestore(seed,out var error),Is.True,error);
        }
        [UnityTest]public IEnumerator ExplicitPreferenceButtonsOfferWithoutExecutionAndSingleConfirmationDispatches()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(panel.Open(false),Is.True);Assert.That(motor.GetComponent<SalvageMission>().enabled,Is.False);Assert.That(input.UiBlocked,Is.True);var generation=brain.Generation;
            panel.HighScoreButton.onClick.Invoke();Assert.That(session.Preference,Is.EqualTo(RecoveryPreference.HighScore));panel.SuggestButton.onClick.Invoke();Assert.That(advice.PendingTargetId,Is.EqualTo("cargo_e"));Assert.That(brain.Generation,Is.EqualTo(generation));Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Wait));
            panel.ConfirmSuggestionButton.onClick.Invoke();Assert.That(brain.CurrentTargetId,Is.EqualTo("cargo_e"));Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Investigate));Assert.That(advice.PendingTargetId,Is.Null);var next=brain.Generation;Assert.That(advice.Confirm(),Is.False);Assert.That(brain.Generation,Is.EqualTo(next));Assert.That(motor.GetComponent<SalvageDelivery>().Count,Is.Zero);
            panel.Close();Assert.That(motor.GetComponent<SalvageMission>().enabled,Is.True);Assert.That(input.UiBlocked,Is.False);Assert.That(input.PointerCaptured,Is.True);
        }
        [UnityTest]public IEnumerator OptionalTextRecommendationDoesNotPersistPreferenceAndAmbiguityOrForbiddenRequestNeverExecutes()
        {
            yield return new EnterPlayMode();yield return Boot();session.SetPreference(RecoveryPreference.HighScore);Assert.That(panel.Open(true),Is.True);var generation=brain.Generation;
            panel.InputField.text="推荐轻量";panel.Submit();Assert.That(panel.InputField.text,Is.Empty);Assert.That(advice.PendingTargetId,Is.EqualTo("cargo_a"));Assert.That(session.Preference,Is.EqualTo(RecoveryPreference.HighScore));Assert.That(brain.Generation,Is.EqualTo(generation));
            panel.InputField.text="推荐高分和轻量";panel.Submit();Assert.That(advice.PendingTargetId,Is.Null);Assert.That(hub.History.Messages.Last().Text,Does.Contain("只选一种"));
            panel.InputField.text="推荐高分并替我拿";panel.Submit();Assert.That(advice.PendingTargetId,Is.Null);Assert.That(hub.History.Messages.Last().IsRefusal,Is.True);Assert.That(brain.Generation,Is.EqualTo(generation));
            panel.InputField.text="跟随我";panel.Submit();Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(GameRuntimeOptions.UseLLM,Is.False);
        }
        [UnityTest]public IEnumerator HeldTargetInvalidatesAdviceAndRestoreClosesDraftWhilePreservingCompletedBoardBlock()
        {
            yield return new EnterPlayMode();yield return Boot();var snapshot=session.Capture();Assert.That(advice.Offer(RecoveryPreference.Nearby),Is.True);var actor=hub.Targets.Single(t=>t.Id=="cargo_a").GetComponent<CargoActor>();motor.RestorePose(actor.transform.position+new Vector3(0,.1f,-1.8f),0,0);var delta=actor.Body.bounds.center-motor.View.position;motor.RestorePose(motor.transform.position,0,-Mathf.Atan2(delta.y,delta.z)*Mathf.Rad2Deg);Physics.SyncTransforms();Assert.That(motor.GetComponent<SalvageCarry>().TryPickFromView(),Is.True);var generation=brain.Generation;Assert.That(advice.Confirm(),Is.False);Assert.That(brain.Generation,Is.EqualTo(generation));
            Assert.That(panel.Open(true),Is.True);panel.InputField.text="未提交";snapshot.World.Completed=true;foreach(var item in snapshot.World.Cargo.Where(c=>new[]{"cargo_b","cargo_d","cargo_e"}.Contains(c.Id)))item.State=2;
            Assert.That(session.TryRestore(snapshot,out var error),Is.True,error);Assert.That(panel.IsOpen,Is.False);Assert.That(panel.InputField.text,Is.Empty);Assert.That(motor.GetComponent<SalvageMission>().enabled,Is.True);Assert.That(motor.GetComponent<SalvageMission>().BoardOpen,Is.True);Assert.That(input.UiBlocked,Is.True);Assert.That(input.PointerCaptured,Is.False);
            Directory.CreateDirectory("Docs/Tests/SalvagePrototype/Evidence/R4-A01f-3");File.WriteAllText("Docs/Tests/SalvagePrototype/Evidence/R4-A01f-3/Panel_"+Guid.NewGuid().ToString("N")+".json","{\"explicitPreferences\":true,\"singleConfirmation\":true,\"staleAdviceRejected\":true,\"completedModalPreserved\":true,\"UseLLM\":false}");
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(panel)panel.Close();if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
