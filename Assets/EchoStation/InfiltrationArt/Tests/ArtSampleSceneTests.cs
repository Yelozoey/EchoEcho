using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt.Editor;
using EchoStation.Tests.Fixtures;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.InfiltrationArt.Tests
{
    public sealed class ArtSampleSceneTests
    {
        Scene previous, sample;
        bool background;
        ArtSampleAudioScope audio;
        void LoadEdit()
        {
            previous=SceneManager.GetActiveScene();
            sample=EditorSceneManager.OpenScene(ArtSampleScene.ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(sample);
        }
        [Test]
        public void CriticalObjectsAreSourceModelsWithComfortableHeightAndOpenCorner()
        {
            LoadEdit();
            var root=sample.GetRootGameObjects().Single(g=>g.name=="IA_ArtSample_World");
            var models=root.GetComponentsInChildren<ArtSampleModel>();
            foreach(var role in new[]{"Guard","Echo","Desk","Chair","Terminal","Toolbox","Door","DoorFrame"})
                Assert.That(models.Any(m=>m.Role==role),Is.True,role);
            Assert.That(models.Where(m=>m.name.StartsWith("Ceiling_")).All(m=>Mathf.Abs(m.transform.position.y-4.5f)<.02f),Is.True);
            Physics.SyncTransforms();
            Assert.That(Physics.Raycast(new Vector3(0,1,-4),Vector3.down,out var floor,2),Is.True);
            Assert.That(floor.point.y,Is.EqualTo(0).Within(.03f));
            Assert.That(Physics.Linecast(new Vector3(2.55f,1.6f,.8f),new Vector3(2.55f,1.6f,3.2f)),Is.False,"Door-frame collider must leave its actual opening clear");
        }
        [Test]
        public void StaticRoomHasActualLightmapsAndDynamicCharactersHaveProbes()
        {
            LoadEdit();
            var root=sample.GetRootGameObjects().Single(g=>g.name=="IA_ArtSample_World");
            Assert.That(LightmapSettings.lightmaps.Length,Is.GreaterThan(0),"A lightmap flag is not a bake");
            Assert.That(root.GetComponentsInChildren<ArtSampleModel>().Where(m=>m.Role=="Floor"||m.Role=="Wall").SelectMany(m=>m.GetComponentsInChildren<MeshRenderer>())
                .Count(r=>r.lightmapIndex>=0&&r.lightmapIndex<LightmapSettings.lightmaps.Length),Is.GreaterThanOrEqualTo(12));
            Assert.That(root.GetComponentInChildren<LightProbeGroup>().probePositions.Length,Is.GreaterThanOrEqualTo(27));
            Assert.That(root.GetComponentsInChildren<Light>().Count(l=>l.lightmapBakeType==LightmapBakeType.Realtime),Is.EqualTo(1));
        }
        [Test]
        public void CameraAndVolumeUseInternalHdrSdrAndAces()
        {
            LoadEdit();
            var camera=sample.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>()).Single();
            Assert.That(camera.allowHDR,Is.True);
            var data=camera.GetUniversalAdditionalCameraData();Assert.That(data.allowHDROutput,Is.False);Assert.That(data.renderPostProcessing,Is.True);
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(ArtSampleScene.VolumePath);
            Assert.That(profile.TryGet<Tonemapping>(out var tone)&&tone.mode.value==TonemappingMode.ACES,Is.True);
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ArtSampleScene.PipelinePath);
            Assert.That(pipeline.supportsHDR&&pipeline.useSRPBatcher,Is.True);
        }
        static double Luma(Color c)=>.2126*c.r+.7152*c.g+.0722*c.b;
        [UnityTest,Timeout(180000)]
        public IEnumerator FourFirstPersonViewsHaveReadableActualGeometry()
        {
            yield return new EnterPlayMode();
            background=Application.runInBackground;Application.runInBackground=true;
            previous=SceneManager.GetActiveScene();
            audio=new ArtSampleAudioScope();
            sample=EditorSceneManager.LoadSceneInPlayMode(ArtSampleScene.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));
            while(!sample.isLoaded)yield return null;
            SceneManager.SetActiveScene(sample);yield return null;
            Assert.That(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.enabled),Is.EqualTo(1));
            var root=sample.GetRootGameObjects().Single(g=>g.name=="IA_ArtSample_World");
            var camera=root.GetComponentInChildren<Camera>();camera.scene=sample;
            root.GetComponentInChildren<EchoStation.Salvage.SalvageMotor>().enabled=false;
            root.GetComponentInChildren<EchoStation.Player.PlayerInputReader>().enabled=false;
            var rows=new JArray();string version=Guid.NewGuid().ToString("N").Substring(0,8);
            for(int i=0;i<ArtSampleScene.ViewPositions.Length;i++)
            {
                camera.transform.position=ArtSampleScene.ViewPositions[i];camera.transform.LookAt(ArtSampleScene.ViewTargets[i]);
                for(int f=0;f<3;f++)yield return null;
                string path=ArtSampleAssets.Evidence+"/View_"+i+"_"+version+".png";
                Color[] pixels;using(var capture=new SceneFrameCapture(camera))pixels=capture.Capture(path);
                double mean=pixels.Average(Luma),dark=pixels.Count(p=>Luma(p)<.01)/(double)pixels.Length,white=pixels.Count(p=>p.r>.99f&&p.g>.99f&&p.b>.99f)/(double)pixels.Length;
                var row=new JObject{{"view",i},{"image",path},{"linearMean",mean},{"darkFraction",dark},{"whiteFraction",white}};rows.Add(row);
                File.WriteAllText(ArtSampleAssets.Evidence+"/ViewMetrics_"+version+".json",rows.ToString());
                Assert.That(mean,Is.GreaterThan(.035),row.ToString());Assert.That(dark,Is.LessThan(.45),row.ToString());Assert.That(white,Is.LessThan(.1),row.ToString());
            }
        }
        [TearDown]
        public void CleanupEdit()
        {
            if(Application.isPlaying)return;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(sample.IsValid()&&sample.isLoaded)EditorSceneManager.CloseScene(sample,true);
        }
        [UnityTearDown]
        public IEnumerator CleanupPlay()
        {
            if(!Application.isPlaying)yield break;
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(sample.IsValid()&&sample.isLoaded){var op=SceneManager.UnloadSceneAsync(sample);while(op!=null&&!op.isDone)yield return null;}
            audio?.Dispose();audio=null;
            Application.runInBackground=background;
            yield return new ExitPlayMode();
        }
    }
}
