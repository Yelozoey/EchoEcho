using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
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
    public sealed class NextFocusTests
    {
        Scene old,scene;bool background;SalvageMotor motor;PlayerInputReader input;SalvageCarry carry;NextSession session;NextFocus focus;Volume baseVolume;VolumeProfile baseInstance;
        IEnumerator Boot()
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(NextLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;
            var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");motor=root.GetComponentInChildren<SalvageMotor>();motor.enabled=false;input=motor.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);carry=motor.GetComponent<SalvageCarry>();session=motor.GetComponent<NextSession>();focus=motor.GetComponent<NextFocus>();var echo=root.GetComponentInChildren<SalvageEcho>();echo.GetComponent<SalvageEchoCoordinator>().enabled=false;echo.enabled=false;
            var fog=root.GetComponent<NextFogField>();if(fog)fog.enabled=false;
            var actor=root.GetComponentsInChildren<CargoActor>().Single(a=>a.Id=="cargo_a");motor.RestorePose(actor.transform.position+new Vector3(0,.1f,-1.8f),0,0);var delta=actor.Body.bounds.center-motor.View.position;motor.RestorePose(motor.transform.position,0,-Mathf.Atan2(delta.y,delta.z)*Mathf.Rad2Deg);Physics.SyncTransforms();Assert.That(carry.Focus(),Is.SameAs(actor));Directory.CreateDirectory(NextFocusSetup.Evidence);
        }
        [UnityTest]public IEnumerator ActualTargetFadesAndMenuLowHeldDisableReleaseWithoutMutatingSharedProfile()
        {
            yield return new EnterPlayMode();yield return Boot();focus.Evaluate(.075f);Assert.That(focus.State.Weight,Is.GreaterThan(0).And.LessThan(1));focus.Evaluate(.075f);Assert.That(focus.FocusVolume.weight,Is.EqualTo(1));Assert.That(focus.Depth.gaussianStart.value,Is.EqualTo(focus.State.Distance+.75f));Assert.That(motor.View.GetComponent<Camera>().GetUniversalAdditionalCameraData().requiresDepthTexture,Is.True);
            input.SetUiBlocked(true);focus.Evaluate(.2f);Assert.That(focus.FocusVolume.weight,Is.Zero);input.SetUiBlocked(false);input.SetPointerCaptured(true);session.SetQuality(RecoveryQuality.Low);focus.Evaluate(.2f);Assert.That(focus.FocusVolume.weight,Is.Zero);session.SetQuality(RecoveryQuality.Medium);focus.Evaluate(.2f);Assert.That(focus.FocusVolume.weight,Is.EqualTo(1));
            Assert.That(carry.TryPickFromView(),Is.True,carry.LastMessage);focus.Evaluate(.2f);Assert.That(focus.FocusVolume.weight,Is.Zero);focus.enabled=false;Assert.That(focus.State.Active,Is.False);Assert.That(focus.FocusVolume.weight,Is.Zero);
            var shared=AssetDatabase.LoadAssetAtPath<VolumeProfile>(NextFocusSetup.ProfilePath);Assert.That(shared.TryGet<DepthOfField>(out var depth),Is.True);Assert.That(depth.gaussianStart.value,Is.EqualTo(2));Assert.That(depth.gaussianEnd.value,Is.EqualTo(6));Assert.That(focus.FocusVolume.profile,Is.Not.SameAs(shared));
        }
        static IEnumerator Frame(){int frame=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.frameCount==frame){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(5));yield return null;}}
        [UnityTest]public IEnumerator SelectedCargoFocusActuallyBlursFarDepthAndOwnedRuntimeProfileIsDestroyed()
        {
            yield return new EnterPlayMode();yield return Boot();focus.Evaluate(.2f);var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var camera=motor.View.GetComponent<Camera>();camera.scene=scene;baseVolume=root.GetComponentsInChildren<Volume>().Single(v=>v.name=="ES_Next_GlobalVolume");baseInstance=baseVolume.profile;baseInstance.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;baseInstance.TryGet<ChromaticAberration>(out var chromatic);chromatic.intensity.value=0;
            var rendererData=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(NextRendering.RendererPath);var ao=rendererData.rendererFeatures.Single(f=>f.name.Contains("AmbientOcclusion"));bool active=ao.isActive;var lights=root.GetComponentsInChildren<Light>(true).Where(l=>l.lightmapBakeType==LightmapBakeType.Realtime).ToArray();var flags=lights.Select(l=>l.enabled).ToArray();var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="FarDepthTestChart";quad.transform.SetPositionAndRotation(camera.transform.position+camera.transform.forward*8,camera.transform.rotation);quad.transform.localScale=new Vector3(4,2.25f,1);var texture=new Texture2D(128,128,TextureFormat.RGBA32,false,true);texture.filterMode=FilterMode.Point;var pixels=new Color32[128*128];for(int y=0;y<128;y++)for(int x=0;x<128;x++)pixels[y*128+x]=(x/2+y/2)%2==0?new Color32(255,255,255,255):new Color32(0,0,0,255);texture.SetPixels32(pixels);texture.Apply();var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetTexture("_BaseMap",texture);quad.GetComponent<MeshRenderer>().sharedMaterial=material;var id=Guid.NewGuid().ToString("N");
            try
            {
                foreach(var light in lights)light.enabled=false;ao.SetActive(false);focus.enabled=false;yield return Frame();Color[] off,on;using(var capture=new SalvageFrameCapture(camera))off=capture.Capture(NextFocusSetup.Evidence+"/DepthOff_"+id+".png");focus.FocusVolume.weight=1;yield return Frame();using(var capture=new SalvageFrameCapture(camera))on=capture.Capture(NextFocusSetup.Evidence+"/DepthOn_"+id+".png");float difference=SalvageFrameCapture.Difference(off,on);File.WriteAllText(NextFocusSetup.Evidence+"/Depth_"+id+".json",new JObject{{"difference",difference},{"farChartDistance",8},{"selectedCargo","cargo_a"},{"start",focus.Depth.gaussianStart.value},{"end",focus.Depth.gaussianEnd.value},{"realTargetParameters",true}}.ToString());Assert.That(difference,Is.GreaterThan(.00001f));quad.GetComponent<MeshRenderer>().enabled=false;yield return Frame();using(var capture=new SalvageFrameCapture(camera))capture.Capture(NextFocusSetup.Evidence+"/ActualFocus_"+id+".png");
            }
            finally{focus.FocusVolume.weight=0;focus.enabled=true;ao.SetActive(active);for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].enabled=flags[i];Object.Destroy(quad);Object.Destroy(texture);Object.Destroy(material);}
            var runtime=focus.FocusVolume.profile;var components=runtime.components.ToArray();if(old.IsValid())SceneManager.SetActiveScene(old);var operation=SceneManager.UnloadSceneAsync(scene);while(operation!=null&&!operation.isDone)yield return null;scene=default;yield return null;Assert.That(runtime==null,Is.True);Assert.That(components.All(c=>c==null),Is.True);
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(baseVolume)baseVolume.profile=null;if(baseInstance){foreach(var component in baseInstance.components)if(component)Object.Destroy(component);Object.Destroy(baseInstance);baseInstance=null;}if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
