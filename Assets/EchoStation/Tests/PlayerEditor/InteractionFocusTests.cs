using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.PlayerFixtures;
using EchoStation.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class InteractionFocusTests
    {
        private Scene scene,previous;private FocusChart chart;private bool background;private SyntheticInputScope inputs;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-P06";
        [Serializable]private class Result{public bool passed;public float nearOff,nearOn,farOff,farOn,focusDistance;public int bokehEvents;}
        [UnityTest]public IEnumerator P06_B01_SelectionDrivesNativeBokehAndUiOcclusionRemoveFocus()
        {
            yield return new EnterPlayMode();previous=SceneManager.GetActiveScene();background=Application.runInBackground;Application.runInBackground=true;scene=SceneManager.CreateScene("InteractionFocusFixture",new CreateSceneParameters(LocalPhysicsMode.Physics3D));SceneManager.SetActiveScene(scene);inputs=new SyntheticInputScope();Directory.CreateDirectory(Evidence);chart=new FocusChart();
            var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));actor.GetComponent<FirstPersonMotor>().enabled=false;actor.GetComponent<CharacterController>().enabled=false;actor.transform.position=new Vector3(0,-1.6f,0);var camera=actor.GetComponentInChildren<Camera>();camera.enabled=false;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.aspect=16f/9;var reader=actor.GetComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);var interaction=actor.GetComponent<PlayerInteraction>();var focus=actor.GetComponent<InteractionFocus>();var sourceProfile=focus.FocusVolume.sharedProfile;Assert.That(sourceProfile.components.Count,Is.EqualTo(1));Assert.That(focus.FocusVolume.profile,Is.Not.SameAs(sourceProfile));Assert.That(camera.GetUniversalAdditionalCameraData().requiresDepthTexture,Is.True);
            var go=new GameObject("FocusTarget");go.SetActive(false);go.transform.position=new Vector3(0,0,1.05f);var collider=go.AddComponent<BoxCollider>();collider.size=Vector3.one*.1f;collider.isTrigger=true;go.AddComponent<WorldTarget>().Configure("focus_target","焦点测试");go.SetActive(true);scene.GetPhysicsScene().Simulate(.02f);yield return Observe(.2f);Assert.That(interaction.Selection.IsValid,Is.True);Assert.That(focus.State.Weight,Is.EqualTo(1));Assert.That(focus.State.Distance,Is.EqualTo(1).Within(.001f));
            using(var frames=new SceneFrameCapture(camera))
            {
                focus.enabled=false;var off=frames.Capture(Evidence+"/FocusOff.png");focus.enabled=true;yield return Observe(.2f);var on=frames.Capture(Evidence+"/FocusOn.png");var result=new Result{nearOff=FocusChart.Contrast(off,1920,430,450,630,630),nearOn=FocusChart.Contrast(on,1920,430,450,630,630),farOff=FocusChart.Contrast(off,1920,1300,450,1600,630),farOn=FocusChart.Contrast(on,1920,1300,450,1600,630),focusDistance=focus.State.Distance};FrameDebuggerEvidence.CaptureResult debugger=null;yield return FrameDebuggerEvidence.Capture(frames.RenderAndSynchronize,Evidence+"/FocusFrameDebugger.json",r=>debugger=r);result.bokehEvents=debugger.events.Count(e=>e.label.IndexOf("Bokeh",StringComparison.OrdinalIgnoreCase)>=0);result.passed=result.nearOff>.02f&&result.nearOn>result.nearOff*.65f&&result.farOn<result.farOff*.8f&&result.bokehEvents>0;File.WriteAllText(Evidence+"/FocusImages.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
            }
            reader.SetUiBlocked(true);yield return Observe(.2f);Assert.That(focus.State.Active,Is.False);Assert.That(focus.FocusVolume.weight,Is.Zero);reader.SetUiBlocked(false);reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);yield return Observe(.2f);Assert.That(focus.State.Active,Is.True);var wall=new GameObject("Occluder").AddComponent<BoxCollider>();wall.transform.position=new Vector3(0,0,.5f);wall.size=Vector3.one*.2f;scene.GetPhysicsScene().Simulate(.02f);yield return Observe(.2f);Assert.That(focus.State.Active,Is.False);wall.gameObject.SetActive(false);scene.GetPhysicsScene().Simulate(.02f);yield return Observe(.2f);reader.SetApplicationFocus(false);yield return Observe(.2f);Assert.That(focus.State.Active,Is.False);Assert.That(sourceProfile.TryGet<DepthOfField>(out var source),Is.True);Assert.That(source.focusDistance.value,Is.EqualTo(1));Assert.That(source.aperture.value,Is.EqualTo(5.6f));
        }
        private static IEnumerator Observe(float seconds){var end=Time.unscaledTime+seconds;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.unscaledTime<end){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Focus observe timeout.");yield return R04ShadowProbe.Frame();}}
        [UnityTearDown]public IEnumerator After()
        {
            if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}if(Application.isPlaying){chart?.Dispose();chart=null;if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Focus unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
