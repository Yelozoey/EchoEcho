using System.Collections;
using System.IO;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.Rendering
{
    public class RealtimeShadowImageTests
    {
        [UnityTest] public IEnumerator B01_ActualRealtimeLightingCascadesAndDistanceShadows()
        {
            yield return new EnterPlayMode(); var background=Application.runInBackground; Application.runInBackground=true;
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.LoadSceneInPlayMode(RealtimeShadowFixtureSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));
            while(!scene.isLoaded) yield return null; SceneManager.SetActiveScene(scene);
            R04ShadowProbe probe=null;
            try
            {
                probe=new R04ShadowProbe(scene,AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardHdrBaselineSetup.RendererPath));
                var path=Path.Combine(RealtimeShadowSetup.EvidencePath,"Editor"); yield return probe.Run(path);
                File.WriteAllText(Path.Combine(path,"ShadowReport.json"),JsonUtility.ToJson(probe.Report,true));
                Assert.That(probe.Report.passed,Is.True,JsonUtility.ToJson(probe.Report,true));
            }
            finally
            {
                probe?.Dispose(); if(previous.IsValid()) SceneManager.SetActiveScene(previous); SceneManager.UnloadSceneAsync(scene);
                Application.runInBackground=background;
            }
        }
        [UnityTest] public IEnumerator B02_ActualShadowCasterDrawsDisappearWhenOff()
        {
            yield return new EnterPlayMode(); var background=Application.runInBackground; Application.runInBackground=true;
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.LoadSceneInPlayMode(RealtimeShadowFixtureSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));
            while(!scene.isLoaded) yield return null; SceneManager.SetActiveScene(scene);
            R04ShadowProbe probe=null;
            try
            {
                probe=new R04ShadowProbe(scene,AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardHdrBaselineSetup.RendererPath));
                var path=Path.Combine(RealtimeShadowSetup.EvidencePath,"Editor"); Directory.CreateDirectory(path);
                probe.RenderAndSynchronize(); yield return R04ShadowProbe.Frame();
                // Capture the same visible caster at constant camera with actual On and Off.
                probe.Caster.shadowCastingMode=ShadowCastingMode.On; yield return R04ShadowProbe.Frame();
                FrameDebuggerEvidence.CaptureResult on=null,off=null;
                yield return FrameDebuggerEvidence.Capture(probe.RenderAndSynchronize,Path.Combine(path,"FrameDebugger-On.json"),r=>on=r);
                probe.Caster.shadowCastingMode=ShadowCastingMode.Off; yield return R04ShadowProbe.Frame();
                yield return FrameDebuggerEvidence.Capture(probe.RenderAndSynchronize,Path.Combine(path,"FrameDebugger-Off.json"),r=>off=r);
                Assert.That(on.shadowSubmissionEvents,Is.GreaterThan(0),"No actual shadow draw/batch submissions captured.");
                Assert.That(off.shadowSubmissionEvents,Is.Zero,"Caster Off must remove shadow submissions in this single-caster fixture.");
            }
            finally
            {
                probe?.Dispose(); if(previous.IsValid()) SceneManager.SetActiveScene(previous); SceneManager.UnloadSceneAsync(scene);
                Application.runInBackground=background;
            }
        }
        [UnityTearDown] public IEnumerator LeaveRuntime() { if(Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
