using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class FlashlightImageTests
    {
        private Scene scene,previous;private bool background;private Material material;private Light[] foreignLights;private bool[] foreignEnabled;private SphericalHarmonicsL2 oldProbe;private bool probeCaptured;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-P03";
        [Serializable]private class Result{public bool passed;public float lightDifference,shadowDifference,turnDifference,axisDot,staticBrightnessDelta,dynamicBrightnessDelta;public int shadowSubmissionEvents,mainLightShadowEvents,additionalLightShadowEvents;}
        private GameObject Cube(string name,Vector3 position,Vector3 size){var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name=name;cube.layer=30;cube.transform.position=position;cube.transform.localScale=size;var renderer=cube.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;return cube;}
        private static float Brightness(Color[] pixels,Camera camera,Vector3 position){var point=camera.WorldToViewportPoint(position);var x=Mathf.RoundToInt(point.x*1920);var y=Mathf.RoundToInt(point.y*1080);float sum=0;for(var dy=-2;dy<=2;dy++)for(var dx=-2;dx<=2;dx++){var c=pixels[(y+dy)*1920+x+dx];sum+=(c.r+c.g+c.b)/3;}return sum/25;}
        [UnityTest]public IEnumerator P03_B01_ActualFlashlightIlluminatesStaticAndDynamicSurfacesCastsShadowAndTurnsWithView()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();oldProbe=RenderSettings.ambientProbe;probeCaptured=true;scene=SceneManager.CreateScene("FlashlightImageFixture");SceneManager.SetActiveScene(scene);foreignLights=Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Where(l=>l.gameObject.scene!=scene).ToArray();foreignEnabled=foreignLights.Select(l=>l.enabled).ToArray();foreach(var light in foreignLights)light.enabled=false;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;RenderSettings.ambientProbe=new SphericalHarmonicsL2();RenderSettings.reflectionIntensity=0;Directory.CreateDirectory(Evidence);
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.SetColor("_BaseColor",new Color(.55f,.6f,.65f));material.SetFloat("_Smoothness",.15f);var floor=Cube("StaticReceiver",new Vector3(0,-.1f,2),new Vector3(10,.2f,12));floor.isStatic=true;floor.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;Cube("DynamicOccluder",new Vector3(0,.6f,0),new Vector3(.8f,1.2f,.8f));
            var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));actor.GetComponent<CharacterController>().enabled=false;actor.transform.position=new Vector3(0,.03f,-3);actor.GetComponent<PlayerInputReader>().enabled=false;var motor=actor.GetComponent<FirstPersonMotor>();motor.enabled=false;motor.SetLookAngles(0,15);actor.GetComponentInChildren<Camera>().enabled=false;var torch=actor.GetComponent<PlayerFlashlight>();torch.enabled=false;var source=torch.Source;source.cullingMask=1<<30;
            var camera=new GameObject("OffAxisShadowObserver").AddComponent<Camera>();camera.scene=scene;camera.cullingMask=1<<30;camera.transform.position=new Vector3(4,3,-5);camera.transform.LookAt(new Vector3(0,.4f,1));camera.fieldOfView=60;camera.aspect=16f/9;camera.nearClipPlane=.05f;camera.farClipPlane=40;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowHDR=true;var data=camera.GetUniversalAdditionalCameraData();data.allowHDROutput=false;data.renderPostProcessing=false;data.SetRenderer(0);yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();
            using(var frames=new SceneFrameCapture(camera))
            {
                torch.enabled=true;torch.SetOn(false);var off=frames.Capture(Evidence+"/FlashlightOff.png");torch.SetOn(true);source.shadows=LightShadows.Soft;var on=frames.Capture(Evidence+"/FlashlightOn.png");source.shadows=LightShadows.None;var noShadow=frames.Capture(Evidence+"/FlashlightNoShadow.png");source.shadows=LightShadows.Soft;var result=new Result{lightDifference=SceneFrameCapture.Difference(off,on),shadowDifference=SceneFrameCapture.Difference(on,noShadow),staticBrightnessDelta=Brightness(on,camera,new Vector3(1,0,1))-Brightness(off,camera,new Vector3(1,0,1)),dynamicBrightnessDelta=Brightness(on,camera,new Vector3(0,.6f,-.4001f))-Brightness(off,camera,new Vector3(0,.6f,-.4001f))};FrameDebuggerEvidence.CaptureResult debugger=null;yield return FrameDebuggerEvidence.Capture(frames.RenderAndSynchronize,Evidence+"/FlashlightFrameDebugger.json",r=>debugger=r);result.shadowSubmissionEvents=debugger.shadowSubmissionEvents;result.mainLightShadowEvents=debugger.events.Count(e=>e.type=="SRPBatch"&&e.label.Contains("/MainLightShadow/"));result.additionalLightShadowEvents=debugger.events.Count(e=>e.type=="SRPBatch"&&e.label.Contains("/AdditionalLightsShadow/"));
                motor.SetLookAngles(180,15);result.axisDot=Vector3.Dot(source.transform.forward,motor.View.forward);var turned=frames.Capture(Evidence+"/FlashlightTurnedAway.png");result.turnDifference=SceneFrameCapture.Difference(on,turned);result.passed=result.lightDifference>.0001f&&result.shadowDifference>.000001f&&result.turnDifference>.0001f&&result.axisDot>.9999f&&result.staticBrightnessDelta>.001f&&result.dynamicBrightnessDelta>.001f&&result.additionalLightShadowEvents>0&&result.mainLightShadowEvents==0;File.WriteAllText(Evidence+"/FlashlightImages.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
            }
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(Application.isPlaying){if(foreignLights!=null)for(var i=0;i<foreignLights.Length;i++)if(foreignLights[i])foreignLights[i].enabled=foreignEnabled[i];if(material)Object.Destroy(material);if(previous.IsValid())SceneManager.SetActiveScene(previous);if(probeCaptured){RenderSettings.ambientProbe=oldProbe;probeCaptured=false;}var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Flashlight image fixture unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
