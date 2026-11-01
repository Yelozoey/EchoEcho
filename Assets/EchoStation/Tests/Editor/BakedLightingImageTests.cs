using System.Collections;
using System.IO;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.Rendering
{
    public class BakedLightingImageTests
    {
        [UnityTest] public IEnumerator B02_ActualLightmapsAndDynamicProbeLightingSurviveLightsOff()
        {
            yield return new EnterPlayMode();
            var background = Application.runInBackground; Application.runInBackground = true;
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.LoadSceneInPlayMode(BakedLightingSetup.ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
            while (!scene.isLoaded) yield return null;
            SceneManager.SetActiveScene(scene);
            R03LightingProbe probe = null;
            try
            {
                probe = new R03LightingProbe(scene, AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardHdrBaselineSetup.RendererPath));
                var directory = Path.Combine(BakedLightingSetup.EvidencePath, "Editor");
                yield return probe.Run(directory);
                File.WriteAllText(Path.Combine(directory, "LightingReport.json"), JsonUtility.ToJson(probe.Report, true));
                Assert.That(probe.Report.passed, Is.True, JsonUtility.ToJson(probe.Report, true));
            }
            finally
            {
                probe?.Dispose();
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                SceneManager.UnloadSceneAsync(scene);
                Application.runInBackground = background;
            }
        }
        [UnityTearDown] public IEnumerator LeaveRuntime() { if (Application.isPlaying) yield return new ExitPlayMode(); }
    }
}
