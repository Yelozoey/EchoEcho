using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationMetal.Editor;
using EchoStation.InfiltrationWallPreview.Editor;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Player;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.InfiltrationWallPreview.Tests
{
    public sealed class WallVisualTests
    {
        Scene previous,sample;Transform root;Camera camera;WallBrightnessPreview preview;SyntheticInputScope inputs;
        bool background;int fps;float scale;AudioListener[] audio;RenderPipelineAsset oldDefault,oldQuality;
        IEnumerator Load()
        {
            Assert.That(Application.isPlaying,Is.True);previous=SceneManager.GetActiveScene();background=Application.runInBackground;fps=Application.targetFrameRate;scale=Time.timeScale;oldDefault=GraphicsSettings.defaultRenderPipeline;oldQuality=QualitySettings.renderPipeline;
            Application.runInBackground=true;Application.targetFrameRate=60;Time.timeScale=1;audio=UnityEngine.Object.FindObjectsByType<AudioListener>().Where(a=>a.enabled).ToArray();foreach(var a in audio)a.enabled=false;
            var game=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(game).Show();EditorWindow.GetWindow(game).Focus();
            sample=EditorSceneManager.LoadSceneInPlayMode(WallPreviewScene.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!sample.isLoaded){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8));yield return null;}
            SceneManager.SetActiveScene(sample);root=MetalScene.World(sample);camera=root.GetComponentInChildren<Camera>();camera.scene=sample;preview=root.GetComponent<WallBrightnessPreview>();yield return Frames(5);
        }
        static IEnumerator Frames(int n){for(int i=0;i<n;i++){int frame=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.frameCount==frame){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8));yield return null;}}}
        static double Luma(Color c)=>.2126*c.r+.7152*c.g+.0722*c.b;
        static double Roi(Color[] pixels){double sum=0;for(int y=320;y<760;y++)for(int x=680;x<1240;x++)sum+=Luma(pixels[y*1920+x]);return sum/(440*560);}
        [UnityTest,Timeout(120000)]public IEnumerator MatchedViewSeparatesBaseColorAndGrazingLight()
        {
            yield return new EnterPlayMode();yield return Load();root.GetComponentInChildren<SalvageMotor>().enabled=false;root.GetComponentInChildren<PlayerInputReader>().enabled=false;foreach(var a in root.GetComponentsInChildren<Animator>())a.enabled=false;
            camera.transform.position=new Vector3(0,1.66f,-11);camera.transform.LookAt(new Vector3(0,2.2f,-16));root.GetComponentInChildren<Volume>().profile.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;
            string id=Guid.NewGuid().ToString("N").Substring(0,8);using(var capture=new SceneFrameCapture(camera))
            {
                preview.SetMode(0);var before=capture.Capture(preview.Coated?null:WallPreviewScene.Evidence+"/Wall_Before_"+id+".png");preview.SetMode(1);var color=capture.Capture();preview.SetMode(0);preview.grazing.enabled=true;var light=capture.Capture();preview.SetMode(preview.Coated?1:2);var after=capture.Capture(WallPreviewScene.Evidence+"/"+(preview.Coated?"Coated_NoFill_":"Wall_After_")+id+".png");preview.SetMode(0);var repeat=capture.Capture();
                var receipt=new JObject{{"width",1920},{"height",1080},{"exposure",.55},{"coated",preview.Coated},{"afterHasPreviewFill",!preview.Coated},{"grainFrozen",true},{"sameCameraAndWorld",true},{"roiBefore",Roi(before)},{"roiColorOnly",Roi(color)},{"roiLightOnly",Roi(light)},{"roiCombined",Roi(after)},{"colorDifference",SceneFrameCapture.Difference(before,color)},{"lightDifference",SceneFrameCapture.Difference(before,light)},{"combinedDifference",SceneFrameCapture.Difference(before,after)},{"originalRestoredDifference",SceneFrameCapture.Difference(before,repeat)},{"targetHardwareAccepted",false}};
                File.WriteAllText(WallPreviewScene.Evidence+"/WallDeltas_"+id+".json",receipt.ToString());
                Assert.That(Roi(before),Is.GreaterThan(.001));Assert.That(Roi(color),Is.GreaterThan(Roi(before)));Assert.That(Roi(after),Is.GreaterThan(Roi(before)*1.05));Assert.That((float)receipt["lightDifference"],Is.GreaterThan(.0001));Assert.That((float)receipt["originalRestoredDifference"],Is.LessThan(.0002));
            }
        }
        [UnityTest,Timeout(120000)]public IEnumerator ActualF5F6F7MovementAndDisableRestore()
        {
            yield return new EnterPlayMode();yield return Load();inputs=new SyntheticInputScope();var motor=root.GetComponentInChildren<SalvageMotor>();var reader=motor.GetComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);
            foreach(var key in new[]{Key.F5,Key.F6,Key.F7}){InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(key));yield return Frames(2);Assert.That(preview.Mode,Is.EqualTo(key==Key.F5?0:key==Key.F6?1:2));Assert.That(preview.grazing.enabled,Is.EqualTo(key==Key.F7));InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return Frames(2);}
            var start=motor.transform.position;InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.D));yield return Frames(6);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return Frames(2);Assert.That(Mathf.Abs(motor.transform.position.x-start.x),Is.GreaterThan(.1));
            preview.enabled=false;Assert.That(preview.grazing.enabled,Is.False);Assert.That(preview.surfaces.All(r=>r.sharedMaterial==preview.original),Is.True);preview.enabled=true;Assert.That(preview.grazing.enabled,Is.True);Assert.That(preview.surfaces.All(r=>r.sharedMaterial==preview.brighter),Is.True);
            File.WriteAllText(WallPreviewScene.Evidence+"/Input_"+Guid.NewGuid().ToString("N").Substring(0,8)+".json",new JObject{{"keys","F5/F6/F7"},{"movedX",motor.transform.position.x-start.x},{"disableRestore",true},{"reenableDefault",true}}.ToString());
        }
        [UnityTearDown]public IEnumerator Close()
        {
            if(!Application.isPlaying)yield break;if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded){var op=SceneManager.UnloadSceneAsync(sample);while(!op.isDone)yield return null;}
            foreach(var a in audio??Array.Empty<AudioListener>())if(a)a.enabled=true;Application.runInBackground=background;Application.targetFrameRate=fps;Time.timeScale=scale;Assert.That(GraphicsSettings.defaultRenderPipeline,Is.SameAs(oldDefault));Assert.That(QualitySettings.renderPipeline,Is.SameAs(oldQuality));yield return new ExitPlayMode();
        }
    }
}
