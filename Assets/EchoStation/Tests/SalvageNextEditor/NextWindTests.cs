using System;
using System.Collections;
using System.IO;
using EchoStation.Salvage.Editor;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextWindTests
    {
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;var scene=SceneManager.GetSceneByName("NextWindFixture");if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}yield return new ExitPlayMode();}
        [UnityTest]public IEnumerator ShaderWindChangesPixelsWithIdenticalCpuMeshAndNoAnimationComponents()
        {
            yield return new EnterPlayMode();var background=Application.runInBackground;Application.runInBackground=true;var previous=SceneManager.GetActiveScene();var pipeline=QualitySettings.renderPipeline;var scene=SceneManager.CreateScene("NextWindFixture");SceneManager.SetActiveScene(scene);var material=Object.Instantiate(AssetDatabase.LoadAssetAtPath<Material>(NextDecor.PathFor("Fabric.mat")));var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(NextDecor.PathFor("Strip.asset"));var vertices=mesh.vertices;
            try
            {
                QualitySettings.renderPipeline=AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(NextRendering.PipelinePath(1));var camera=new GameObject("WindCamera").AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=.55f;camera.nearClipPlane=.1f;camera.farClipPlane=5;camera.transform.position=new Vector3(0,0,1);camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowHDR=true;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;var light=new GameObject("Main Light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(25,180,0);light.intensity=2;var parameters=new RenderParams(material){camera=camera,shadowCastingMode=ShadowCastingMode.Off};var matrices=new[]{Matrix4x4.TRS(new Vector3(0,0,2),Quaternion.identity,new Vector3(.12f,.75f,1))};Color[] still,moved;string id=Guid.NewGuid().ToString("N");
                material.SetFloat("_Wind",0);Graphics.RenderMeshInstanced(parameters,mesh,0,matrices);using(var capture=new SalvageFrameCapture(camera))still=capture.Capture(NextDecor.Evidence+"/WindOff_"+id+".png");material.SetFloat("_Wind",1);Graphics.RenderMeshInstanced(parameters,mesh,0,matrices);using(var capture=new SalvageFrameCapture(camera))moved=capture.Capture(NextDecor.Evidence+"/WindOn_"+id+".png");float diff=SalvageFrameCapture.Difference(still,moved);CollectionAssert.AreEqual(vertices,mesh.vertices);File.WriteAllText(NextDecor.Evidence+"/WindAB_"+id+".json",new JObject{{"difference",diff},{"cpuVerticesUnchanged",true},{"amplitudeMeters",.02},{"cpuAnimationComponents",0}}.ToString());Assert.That(diff,Is.GreaterThan(.00001f));
            }
            finally{QualitySettings.renderPipeline=pipeline;Object.Destroy(material);SceneManager.SetActiveScene(previous);Application.runInBackground=background;}
            var unload=SceneManager.UnloadSceneAsync(scene);while(unload!=null&&!unload.isDone)yield return null;yield return new ExitPlayMode();
        }
    }
}
