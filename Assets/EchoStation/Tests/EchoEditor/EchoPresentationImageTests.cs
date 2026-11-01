using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
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

namespace EchoStation.Tests.Echo
{
    public class EchoPresentationImageTests
    {
        private Scene scene,previous;private bool background;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-E11";
        [Serializable]private class Pixel{public string state;public Color linear;public bool lampEnabled;}
        [Serializable]private class Result{public bool passed;public Pixel[] pixels;public int opaqueSrpBatches;}
        private static Color Sample(Color[] pixels,Vector3 point)
        {
            var x=Mathf.Clamp(Mathf.RoundToInt(point.x*1920),4,1915);var y=Mathf.Clamp(Mathf.RoundToInt(point.y*1080),4,1075);var sum=Color.clear;
            for(var dy=-3;dy<=3;dy++)for(var dx=-3;dx<=3;dx++)sum+=pixels[(y+dy)*1920+x+dx];return sum/49;
        }
        [UnityTest]public IEnumerator E11_B01_ActualLampPixelsBlinkAndDynamicRobotUsesSrpBatcher()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("EchoPresentationImageFixture");SceneManager.SetActiveScene(scene);
            var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath));var rig=actor.GetComponent<EchoRig>();var presentation=actor.GetComponent<EchoPresentation>();presentation.enabled=false;
            var camera=new GameObject("PresentationCamera",typeof(Camera)).GetComponent<Camera>();camera.scene=scene;camera.allowHDR=true;camera.allowMSAA=false;camera.orthographic=true;camera.orthographicSize=.6f;camera.aspect=16f/9f;camera.nearClipPlane=.05f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            camera.transform.position=new Vector3(0,.7f,-2);camera.transform.LookAt(rig.Status.bounds.center);var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(0);data.allowHDROutput=false;data.renderPostProcessing=false;
            var light=new GameObject("PresentationLight",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.transform.rotation=Quaternion.Euler(45,-30,0);
            Directory.CreateDirectory(Evidence);using var frames=new SceneFrameCapture(camera);var rows=new List<Pixel>();
            foreach(var name in new[]{"Blue","YellowOn","YellowOff","Red"})
            {
                yield return R04ShadowProbe.Frame();var mood=name=="Blue"?EchoMood.Calm:name=="Red"?EchoMood.Alert:EchoMood.Thinking;
                presentation.ApplyFrame(0,mood,.1f,name=="YellowOff"?.4f:.1f,false);var colors=frames.Capture(Path.Combine(Evidence,name+".png"));var point=camera.WorldToViewportPoint(rig.Status.bounds.center);Assert.That(point.x,Is.InRange(.05f,.95f));Assert.That(point.y,Is.InRange(.05f,.95f));rows.Add(new Pixel{state=name,linear=Sample(colors,point),lampEnabled=rig.Status.enabled});
            }
            FrameDebuggerEvidence.CaptureResult capture=null;yield return FrameDebuggerEvidence.Capture(frames.RenderAndSynchronize,Evidence+"/RobotFrameDebugger.json",value=>capture=value);
            var blue=rows[0].linear;var yellow=rows[1].linear;var off=rows[2].linear;var red=rows[3].linear;var batches=capture.events.Count(e=>e.type=="SRPBatch"&&e.label.Contains("DrawOpaqueObjects"));
            var report=new Result{pixels=rows.ToArray(),opaqueSrpBatches=batches,passed=blue.b>blue.r*2&&blue.b>blue.g*1.2f&&yellow.r>yellow.b*3&&yellow.g>yellow.b*3&&red.r>red.g*3&&red.r>red.b*3&&off.maxColorComponent<yellow.maxColorComponent*.3f&&batches>0&&GraphicsSettings.useScriptableRenderPipelineBatching};
            File.WriteAllText(Evidence+"/IndicatorImages.json",JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True,JsonUtility.ToJson(report,true));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(Application.isPlaying)
            {if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Presentation image fixture unload timed out.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
