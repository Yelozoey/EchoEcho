using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Rendering;
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
    public sealed class NextBrightnessTests
    {
        const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-Feedback01";
        Scene previous,scene;bool background;Volume volume;VolumeProfile profile;
        ScriptableRendererFeature ao;bool aoActive;
        static double Luminance(Color c)=>.2126*c.r+.7152*c.g+.0722*c.b;
        static JObject Metrics(Color[] pixels)
        {
            return new JObject{{"linearMean",pixels.Average(Luminance)},
                {"darkFraction",pixels.Count(p=>Luminance(p)<.01)/(double)pixels.Length},
                {"whiteClippedFraction",pixels.Count(p=>p.r>=.99f&&p.g>=.99f&&p.b>=.99f)/(double)pixels.Length}};
        }
        static IEnumerator Frames(int count)
        {
            for(int i=0;i<count;i++){int frame=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.frameCount==frame){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(5));yield return null;}}
        }
        [UnityTest,Timeout(180000)]public IEnumerator ThreeRoomsAllQualitiesImproveDarkReadabilityWithoutWhiteClipping()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;
            previous=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(NextLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;
            var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");
            root.GetComponentInChildren<SalvageMotor>().enabled=false;var echo=root.GetComponentInChildren<SalvageEcho>();echo.GetComponent<SalvageEchoCoordinator>().enabled=false;echo.enabled=false;echo.GetComponent<EchoPresentation>().enabled=false;
            foreach(var f in root.GetComponentsInChildren<FaultLightFlicker>())f.enabled=false;
            foreach(var light in root.GetComponentsInChildren<Light>(true).Where(l=>l.lightmapBakeType==LightmapBakeType.Realtime))light.enabled=false;
            root.GetComponentInChildren<NextFocus>().enabled=false;root.GetComponent<NextFogField>().enabled=false;
            foreach(var d in root.GetComponentsInChildren<InstancedRoomDecor>())d.enabled=false;
            var camera=root.GetComponentInChildren<Camera>();camera.scene=scene;
            volume=root.GetComponentsInChildren<Volume>().Single(v=>v.name=="ES_Next_GlobalVolume");profile=volume.profile;
            profile.TryGet<ColorAdjustments>(out var color);Assert.That(color.postExposure.value,Is.EqualTo(1f));Assert.That(color.contrast.value,Is.EqualTo(2f));
            profile.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;profile.TryGet<ChromaticAberration>(out var ca);ca.intensity.value=0;
            ao=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(NextRendering.RendererPath).rendererFeatures.Single(f=>f.name.Contains("AmbientOcclusion"));aoActive=ao.isActive;ao.SetActive(false);
            var session=root.GetComponentInChildren<NextSession>();Assert.That(session,Is.Not.Null);Directory.CreateDirectory(Evidence);var rows=new JArray();string id=Guid.NewGuid().ToString("N");
            for(int quality=0;quality<3;quality++)
            {
                session.SetQuality((RecoveryQuality)quality);yield return Frames(3);
                for(int room=0;room<3;room++)
                {
                    camera.transform.position=new Vector3(room*12+5,1.6f,12);camera.transform.LookAt(new Vector3(room*12+10,1.6f,16));
                    color.postExposure.value=.25f;color.contrast.value=5;yield return Frames(2);
                    Color[] before,after;string prefix=Evidence+"/Q"+quality+"_Room"+room+"_"+id;
                    using(var capture=new SalvageFrameCapture(camera))before=capture.Capture(prefix+"_Before.png");
                    color.postExposure.value=1;color.contrast.value=2;yield return Frames(2);
                    using(var capture=new SalvageFrameCapture(camera))after=capture.Capture(prefix+"_After.png");
                    var a=Metrics(before);var b=Metrics(after);var row=new JObject{{"quality",quality},{"room",room},{"before",a},{"after",b},{"beforeImage",prefix+"_Before.png"},{"afterImage",prefix+"_After.png"}};rows.Add(row);
                    File.WriteAllText(Evidence+"/Brightness_"+id+".json",rows.ToString());
                    Assert.That(a["linearMean"].Value<double>(),Is.GreaterThan(.001),"Baseline must not be black");
                    Assert.That(b["linearMean"].Value<double>(),Is.GreaterThan(a["linearMean"].Value<double>()*1.1),row.ToString());
                    Assert.That(b["darkFraction"].Value<double>(),Is.LessThanOrEqualTo(a["darkFraction"].Value<double>()),row.ToString());
                    Assert.That(b["whiteClippedFraction"].Value<double>(),Is.LessThanOrEqualTo(a["whiteClippedFraction"].Value<double>()+.01),row.ToString());
                }
            }
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;
            if(ao)ao.SetActive(aoActive);if(volume)volume.profile=null;
            if(profile){foreach(var c in profile.components)if(c)Object.Destroy(c);Object.Destroy(profile);}
            if(previous.IsValid())SceneManager.SetActiveScene(previous);
            if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}
            Application.runInBackground=background;yield return new ExitPlayMode();
        }
    }
}
