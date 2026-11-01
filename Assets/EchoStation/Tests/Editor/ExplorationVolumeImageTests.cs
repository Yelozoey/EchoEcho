using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace EchoStation.Tests.Rendering
{
    public class ExplorationVolumeImageTests
    {
        [UnityTest]
        public IEnumerator T08_Real1080pImagesProveEffectsResponseAndLayerIsolation()
        {
            yield return new EnterPlayMode();
            var originalBackground = Application.runInBackground;
            Application.runInBackground = true;
            var preview = EditorSceneManager.OpenPreviewScene(ExplorationVolumeSetup.ScenePath);
            R02ImageProbe probe = null;
            try
            {
                var camera = preview.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Camera>()).Single();
                var volume = preview.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Volume>()).Single();
                camera.scene = preview; camera.enabled = false;
                probe = new R02ImageProbe(camera, volume);
                var directory = Path.Combine(ExplorationVolumeSetup.EvidencePath, "Editor");
                yield return probe.Run(directory);
                File.WriteAllText(Path.Combine(directory, "ImageReport.json"), JsonUtility.ToJson(probe.Report, true));
                Assert.That(probe.Report.passed, Is.True, JsonUtility.ToJson(probe.Report, true));
            }
            finally
            {
                probe?.Dispose();
                EditorSceneManager.ClosePreviewScene(preview);
                Application.runInBackground = originalBackground;
            }
        }

        [UnityTearDown]
        public IEnumerator ExitRuntime()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
