using System.Collections;
using System.IO;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.World
{
    public class CabinLightingImageTests
    {
        private AsyncOperation pendingUnload;
        [UnityTest]public IEnumerator B203_ActualBakedLightingInAllThreeRooms()
        {
            yield return new EnterPlayMode();var background=Application.runInBackground;Application.runInBackground=true;
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));
            while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);CabinLightingProbe probe=null;
            try
            {
                probe=new CabinLightingProbe(scene,AssetDatabase.LoadAssetAtPath<UniversalRendererData>(ForwardHdrBaselineSetup.RendererPath));
                var directory=Path.Combine(CabinGreyboxSetup.EvidencePath,"EditorLighting");yield return probe.Run(directory);
                File.WriteAllText(Path.Combine(directory,"LightingReport.json"),JsonUtility.ToJson(probe.Report,true));Assert.That(probe.Report.passed,Is.True,JsonUtility.ToJson(probe.Report,true));
            }
            finally{probe?.Dispose();if(previous.IsValid())SceneManager.SetActiveScene(previous);pendingUnload=SceneManager.UnloadSceneAsync(scene);Application.runInBackground=background;}
        }
        [UnityTearDown]public IEnumerator LeaveRuntime()
        {
            var background=Application.runInBackground;Application.runInBackground=true;
            try
            {
                var timer=System.Diagnostics.Stopwatch.StartNew();while(pendingUnload!=null&&!pendingUnload.isDone)
                {if(timer.Elapsed.TotalSeconds>5)throw new System.TimeoutException("Cabin scene unload timed out.");yield return null;}
                if(LightmapSettings.lightProbes&&LightmapSettings.lightProbes.count>0)LightProbes.Tetrahedralize();
            }
            finally{pendingUnload=null;Application.runInBackground=background;}
            if(Application.isPlaying)yield return new ExitPlayMode();
        }
    }
}
