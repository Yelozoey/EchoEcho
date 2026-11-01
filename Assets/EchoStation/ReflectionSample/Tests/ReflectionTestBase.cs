using System;
using System.Collections;
using System.Linq;
using EchoStation.ReflectionSample.Editor;
using EchoStation.Salvage;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.ReflectionSample.Tests
{
    public class ReflectionTestBase
    {
        protected Scene previous,sample;protected Transform root;protected Camera camera;protected ReflectionPreview preview;
        bool background;int fps;float scale;AudioListener[] listeners;RenderPipelineAsset oldDefault,oldQuality;
        protected IEnumerator Load()
        {
            Assert.That(Application.isPlaying,Is.True,"EnterPlayMode must be yielded by the top-level UnityTest");previous=SceneManager.GetActiveScene();oldDefault=GraphicsSettings.defaultRenderPipeline;oldQuality=QualitySettings.renderPipeline;
            background=Application.runInBackground;fps=Application.targetFrameRate;scale=Time.timeScale;Application.runInBackground=true;Application.targetFrameRate=60;Time.timeScale=1;
            listeners=UnityEngine.Object.FindObjectsByType<AudioListener>().Where(a=>a.enabled).ToArray();foreach(var a in listeners)a.enabled=false;
            sample=EditorSceneManager.LoadSceneInPlayMode(ReflectionSampleBuilder.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!sample.isLoaded){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8),"Owned scene did not load");yield return null;}SceneManager.SetActiveScene(sample);root=ReflectionSampleBuilder.World(sample);camera=root.GetComponentInChildren<Camera>();camera.scene=sample;preview=root.GetComponent<ReflectionPreview>();yield return Frames(6);
        }
        protected void Freeze()
        {root.GetComponentInChildren<SalvageMotor>().enabled=false;root.GetComponentInChildren<EchoStation.Player.PlayerInputReader>().enabled=false;foreach(var a in root.GetComponentsInChildren<Animator>())a.enabled=false;foreach(var b in root.GetComponentsInChildren<MonoBehaviour>().Where(b=>b&&b.GetType().Name=="ReflectionDemoMotion"))b.enabled=false;}
        protected static IEnumerator Frames(int n)
        {for(int i=0;i<n;i++){int f=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.frameCount==f){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8));yield return null;}}}
        [UnityTearDown]public IEnumerator Close()
        {
            if(!Application.isPlaying)yield break;if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded){var op=SceneManager.UnloadSceneAsync(sample);while(!op.isDone)yield return null;}
            foreach(var a in listeners??Array.Empty<AudioListener>())if(a)a.enabled=true;Application.runInBackground=background;Application.targetFrameRate=fps;Time.timeScale=scale;
            Assert.That(GraphicsSettings.defaultRenderPipeline,Is.SameAs(oldDefault));Assert.That(QualitySettings.renderPipeline,Is.SameAs(oldQuality));yield return new ExitPlayMode();
        }
    }
}
