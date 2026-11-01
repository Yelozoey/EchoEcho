using System;
using System.Collections;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Rendering;
using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
namespace EchoStation.Salvage.Tests
{
    public class R2ScanFeedbackTests
    {
        Scene old,scene;bool background;SalvageMotor motor;SalvageCommandHub hub;SalvageEchoCoordinator brain;SalvageEcho echo;SalvageScanner scanner;
        IEnumerator Boot(){Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>8)Assert.Fail("Feedback load timeout");yield return null;}SceneManager.SetActiveScene(scene);yield return null;motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;motor.GetComponent<PlayerInputReader>().SetApplicationFocus(true);motor.GetComponent<PlayerInputReader>().SetPointerCaptured(true);hub=motor.GetComponent<SalvageCommandHub>();echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;echo.enabled=false;Assert.That(brain.Initialize(),Is.True);scanner=echo.GetComponent<SalvageScanner>();scanner.Initialize();brain.ResetBase(IntentKind.Wait);}
        IEnumerator Until(Func<bool> done,float max=8){var timer=System.Diagnostics.Stopwatch.StartNew();while(!done()){if(timer.Elapsed.TotalSeconds>max)Assert.Fail("Feedback timeout "+scanner.LastError+" "+brain.LastFailure);yield return null;brain.Advance(Mathf.Min(Time.deltaTime,.1f),false);}}
        [UnityTest]public IEnumerator AllEightActualSensorsCreateOnlyCanonicalMarkersAndNoScore()
        {
            yield return new EnterPlayMode();yield return Boot();var wave=echo.GetComponent<ScanWaveEmitter>();Assert.That(motor.View.GetComponent<ScanWaveView>().Source,Is.SameAs(wave));int count=0;
            foreach(var target in hub.Targets.OrderBy(t=>t.Id))
            {
                TestContext.Out.WriteLine("Actual sensor target "+target.Id);brain.CancelCurrent();var d=target.Definition;var p=target.transform.position;
                if(d.Kind==SalvageTargetKind.Cargo)motor.RestorePose(new Vector3(p.x-2,.1f,p.z),90,35);else motor.RestorePose(new Vector3(p.x,.1f,p.z-3),0,17);
                Assert.That(echo.Navigation.TryBind(target.GetComponent<EchoTargetPermissions>().ApproachPoint.position),Is.True,target.Id);Physics.SyncTransforms();echo.Battery.Restore(100,false);
                Assert.That(hub.PointedId(),Is.EqualTo(target.Id),"Real player sight "+target.Id);uint generation=wave.State.Generation;Assert.That(brain.Command(ScriptCommands.Investigate(target.Id)),Is.True);yield return Until(()=>scanner.Busy);
                Assert.That(wave.State.Generation,Is.GreaterThan(generation));Assert.That(wave.State.Active,Is.True);brain.Advance(.02f,false);Assert.That(echo.GetComponent<EchoPresentation>().Mood,Is.EqualTo(EchoMood.Thinking));Assert.That(echo.GetComponent<EchoPresentation>().ScanLight.enabled,Is.True);
                yield return Until(()=>hub.Known.Contains(target.Id));yield return Until(()=>brain.CurrentMode==IntentKind.Wait);hub.GetComponent<SalvageDiscoveryMarkers>().Refresh();Assert.That(hub.GetComponent<SalvageDiscoveryMarkers>().VisibleCount,Is.EqualTo(++count));Assert.That(hub.Known.Summary(target.Id),Is.EqualTo(d.Summary));Assert.That(echo.GetComponent<EchoPresentation>().ScanLight.enabled,Is.False);
            }
            Assert.That(scanner.PaidDiscoveries,Is.EqualTo(8));Assert.That(hub.GetComponent<SalvageDelivery>().Points,Is.Zero);Assert.That(hub.GetComponent<SalvageCarry>().Registry.Records.All(r=>r.State==CargoState.World),Is.True);Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ParticleSystem>()).Count(),Is.Zero);
            var panel=hub.GetComponent<SalvageCommandPanel>();Assert.That(panel.Open(false),Is.True);yield return null;Assert.That(panel.TargetButtons.Count(b=>b.gameObject.activeSelf),Is.EqualTo(8));Assert.That(hub.GetComponentsInChildren<Text>(true).Any(t=>t.name.StartsWith("Summary_")&&t.text==hub.Known.Summary("cargo_a")),Is.True);
            var directory="Docs/Tests/SalvagePrototype/Evidence/R2-P01b/Runs/"+Guid.NewGuid().ToString("N");TestContext.Out.WriteLine("Fresh image/text evidence: "+directory);System.IO.Directory.CreateDirectory(directory);System.IO.File.WriteAllText(directory+"/KnownTargets_B.txt",string.Join("\n",hub.GetComponentsInChildren<Text>(true).Where(t=>t.name.StartsWith("Summary_")).Select(t=>t.text)));
        }
        [UnityTest]public IEnumerator MarkerHidesWhenHeldAndReturnsAfterSafeDrop()
        {
            yield return new EnterPlayMode();yield return Boot();hub.Known.CommitValidatedSensor("cargo_a");var markers=hub.GetComponent<SalvageDiscoveryMarkers>();markers.Refresh();Assert.That(markers.VisibleCount,Is.EqualTo(1));motor.RestorePose(new Vector3(9,.1f,17),0,35);Physics.SyncTransforms();Assert.That(hub.GetComponent<SalvageCarry>().TryPickFromView(),Is.True);markers.Refresh();Assert.That(markers.VisibleCount,Is.Zero);motor.RestorePose(new Vector3(9,.1f,15),0,0);Physics.SyncTransforms();Assert.That(hub.GetComponent<SalvageCarry>().TryDrop(),Is.True);markers.Refresh();Assert.That(markers.VisibleCount,Is.EqualTo(1));Assert.That(hub.Known.Count,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator ActualGameplayCameraRendersBlueWaveAboveSameFrameNoise()
        {
            yield return new EnterPlayMode();yield return Boot();motor.RestorePose(new Vector3(7,.1f,19),90,35);Assert.That(echo.Navigation.TryBind(new Vector3(9,0,17.5f)),Is.True);var camera=motor.View.GetComponent<Camera>();var data=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();bool oldPost=data.renderPostProcessing;var wave=echo.GetComponent<ScanWaveEmitter>();var directory="Docs/Tests/SalvagePrototype/Evidence/R2-P01b/Runs/"+Guid.NewGuid().ToString("N");TestContext.Out.WriteLine("Fresh image/text evidence: "+directory);System.IO.Directory.CreateDirectory(directory);
            try
            {
                data.renderPostProcessing=false;yield return null;using(var frames=new SalvageFrameCapture(camera)){wave.State.Cancel();var off=frames.Capture(directory+"/WaveOff_B.png");var off2=frames.Capture(directory+"/WaveOffRepeat_B.png");wave.State.Begin(echo.GetComponent<EchoRig>().ScanOrigin.position);wave.State.Tick(.4f);var on=frames.Capture(directory+"/WaveOn_B.png");float noise=SalvageFrameCapture.Difference(off,off2),difference=SalvageFrameCapture.Difference(off,on);TestContext.Out.WriteLine("Actual gameplay camera wave difference="+difference+" baseline noise="+noise);System.IO.File.WriteAllText(directory+"/WavePixels_B.json","{\"difference\":"+difference.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+",\"noise\":"+noise.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"}");Assert.That(difference,Is.GreaterThan(noise*5+.0001f));}
            }
            finally{data.renderPostProcessing=oldPost;wave.State.Cancel();}
        }
        [UnityTearDown]public IEnumerator Cleanup(){var panel=hub?hub.GetComponent<SalvageCommandPanel>():null;if(panel)panel.Close();if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
