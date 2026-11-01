using System.Collections;
using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace EchoStation.Salvage.Tests
{
    public class SalvageHudTests
    {
        Scene old,scene;bool background;
        [UnityTest]public IEnumerator HudReflectsCompletedDeliveryAndCrosshairHasGlyph()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>8)Assert.Fail("HUD scene load timeout");yield return null;}SceneManager.SetActiveScene(scene);yield return null;
            var motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;var input=motor.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);var carry=motor.GetComponent<SalvageCarry>();var delivery=motor.GetComponent<SalvageDelivery>();motor.RestorePose(new Vector3(9,.1f,17),0,35);Physics.SyncTransforms();Assert.That(carry.TryPickFromView(),Is.True);yield return null;
            motor.RestorePose(new Vector3(5,.1f,12),0,0);Assert.That(delivery.Request()&&delivery.Confirm(),Is.True);yield return null;Canvas.ForceUpdateCanvases();
            var texts=motor.GetComponentsInChildren<Text>(true);var hud=texts.Single(t=>t.GetComponentInParent<Canvas>(true).name=="SalvageHUD");Assert.That(hud.text,Does.Contain("1/3").And.Contain("2/12").And.Contain("手持：空手"));var cross=texts.Single(t=>t.GetComponentInParent<Canvas>(true).name=="Crosshair");Assert.That(cross.cachedTextGenerator.characterCountVisible,Is.GreaterThan(0));Assert.That(cross.transform.parent.GetComponent<Image>().color.a,Is.Zero);
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
