using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EchoStation.Echo;
using EchoStation.Salvage;
using EchoStation.Salvage.Editor;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextPostTests
    {
        Scene old,scene;bool background;Volume volume;VolumeProfile instance;
        [Test]public void OwnedProfileUsesApprovedEffectsAndNeutralTextureKeepsOfficialSourceHash()
        {
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(NextPostProcessing.ProfilePath);Assert.That(profile.components.Count,Is.EqualTo(4));Assert.That(profile.TryGet<Tonemapping>(out var tone),Is.True);Assert.That(tone.mode.value,Is.EqualTo(TonemappingMode.ACES));Assert.That(profile.TryGet<FilmGrain>(out var grain),Is.True);Assert.That(grain.intensity.value,Is.EqualTo(.06f));Assert.That(profile.TryGet<ChromaticAberration>(out var chromatic),Is.True);Assert.That(chromatic.intensity.value,Is.EqualTo(.012f));Assert.That(profile.TryGet<ColorAdjustments>(out var color),Is.True);Assert.That(color.saturation.value,Is.EqualTo(-30));
            using(var sha=SHA256.Create())Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(NextPostProcessing.NeutralPath))).Replace("-","").ToLowerInvariant(),Is.EqualTo("7b13c6a7d82b434314237c4b1930b8e12ddc69a70ffef28d73cbd178d3ff6293"));var importer=(TextureImporter)AssetImporter.GetAtPath(NextPostProcessing.NeutralPath);Assert.That(importer.sRGBTexture&&importer.mipmapEnabled,Is.True);Assert.That(importer.maxTextureSize,Is.EqualTo(1024));
        }
        static IEnumerator Frame(){int frame=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.frameCount==frame){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(5));yield return null;}}
        [UnityTest]public IEnumerator ActualAcesGrainAndChromaticEachChangeRenderedFrameAndAllStatesRestore()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(NextLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;
            var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");root.GetComponentInChildren<SalvageMotor>().enabled=false;var echo=root.GetComponentInChildren<SalvageEcho>();echo.GetComponent<SalvageEchoCoordinator>().enabled=false;echo.enabled=false;var camera=root.GetComponentInChildren<Camera>();camera.scene=scene;camera.transform.position=new Vector3(3,1.65f,9);camera.transform.LookAt(new Vector3(10,1.1f,18));volume=root.GetComponentsInChildren<Volume>().Single(v=>v.name=="ES_Next_GlobalVolume");instance=volume.profile;instance.TryGet<Tonemapping>(out var tone);instance.TryGet<FilmGrain>(out var grain);instance.TryGet<ChromaticAberration>(out var chromatic);var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(NextRendering.RendererPath);var ao=renderer.rendererFeatures.Single(f=>f.name.Contains("AmbientOcclusion"));bool active=ao.isActive;var lights=root.GetComponentsInChildren<Light>(true).Where(l=>l.lightmapBakeType==LightmapBakeType.Realtime).ToArray();var flags=lights.Select(l=>l.enabled).ToArray();var rows=new JArray();var id=Guid.NewGuid().ToString("N");
            try
            {
                foreach(var focus in root.GetComponentsInChildren<NextFocus>())focus.enabled=false;
                var fog=root.GetComponent<NextFogField>();if(fog)fog.enabled=false;
                ao.SetActive(false);foreach(var light in lights)light.enabled=false;
                for(int effect=0;effect<3;effect++)
                {
                    tone.mode.value=effect==0?TonemappingMode.None:TonemappingMode.ACES;grain.intensity.value=0;chromatic.intensity.value=0;yield return Frame();Color[] baseline,applied;using(var capture=new SalvageFrameCapture(camera))baseline=capture.Capture(NextPostProcessing.Evidence+"/Effect"+effect+"_Off_"+id+".png");
                    if(effect==0)tone.mode.value=TonemappingMode.ACES;else if(effect==1)grain.intensity.value=.06f;else chromatic.intensity.value=.012f;yield return Frame();using(var capture=new SalvageFrameCapture(camera))applied=capture.Capture(NextPostProcessing.Evidence+"/Effect"+effect+"_On_"+id+".png");float difference=SalvageFrameCapture.Difference(baseline,applied);rows.Add(new JObject{{"effect",new[]{"ACES","FilmGrain","Chromatic"}[effect]},{"difference",difference}});
                }
                File.WriteAllText(NextPostProcessing.Evidence+"/Effects_"+id+".json",rows.ToString());foreach(var row in rows)Assert.That(row["difference"].Value<float>(),Is.GreaterThan(.000001f),row.ToString());tone.mode.value=TonemappingMode.ACES;grain.intensity.value=.06f;chromatic.intensity.value=.012f;ao.SetActive(active);yield return Frame();using(var capture=new SalvageFrameCapture(camera))capture.Capture(NextPostProcessing.Evidence+"/AllEffects_"+id+".png");
            }
            finally{tone.mode.value=TonemappingMode.ACES;grain.intensity.value=.06f;chromatic.intensity.value=.012f;ao.SetActive(active);for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=flags[i];}
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(volume)volume.profile=null;if(instance){foreach(var component in instance.components)if(component)Object.Destroy(component);Object.Destroy(instance);instance=null;}if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
