using System;
using System.Collections;
using System.IO;
using System.Linq;
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
    public sealed class NextDecorTests
    {
        Scene old,scene;bool background;Volume volume;VolumeProfile profile;
        [Test]public void ActualLodGeometryTransparentImpostorAndShaderOnlyWindAreBound()
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(NextDecor.ShaderPath);Assert.That(ShaderUtil.ShaderHasError(shader),Is.False);int[] expected={204,12,4,2};var names=new[]{"LOD0","LOD1","LOD2","Impostor"};for(int i=0;i<4;i++){var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(NextDecor.PathFor(names[i]+".asset"));Assert.That(mesh.triangles.Length/3,Is.EqualTo(expected[i]));Assert.That(mesh.bounds.size.magnitude,Is.LessThan(2));}Assert.That(new[]{0f,7.9f,8,15.9f,16,23.9f,24,47.9f,48}.Select(InstancedRoomDecor.LodAt),Is.EqualTo(new[]{0,0,1,1,2,2,3,3,-1}));
            var image=new Texture2D(2,2);try{Assert.That(image.LoadImage(File.ReadAllBytes(NextDecor.PathFor("Impostor.png"))),Is.True);var pixels=image.GetPixels();Assert.That(pixels.Count(p=>p.a>.9f),Is.GreaterThan(10000));Assert.That(pixels.Count(p=>p.a<.1f),Is.GreaterThan(10000));}finally{Object.DestroyImmediate(image);}foreach(var name in new[]{"Solid","Billboard","Fabric"}){var material=AssetDatabase.LoadAssetAtPath<Material>(NextDecor.PathFor(name+".mat"));Assert.That(material.enableInstancing,Is.True);Assert.That(material.GetFloat("_Wind"),Is.EqualTo(name=="Fabric"?1:0));}StringAssert.Contains("_Time.y",File.ReadAllText(NextDecor.ShaderPath));
            old=SceneManager.GetActiveScene();scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);try{var roots=scene.GetRootGameObjects();var batches=roots.SelectMany(r=>r.GetComponentsInChildren<InstancedRoomDecor>()).ToArray();Assert.That(batches.Length,Is.EqualTo(3));Assert.That(batches.Sum(b=>b.InstanceCount),Is.EqualTo(36));Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<Animator>()).Any(a=>a.GetComponentInParent<InstancedRoomDecor>()),Is.False);Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<Transform>()).Single(t=>t.name=="DecorCollision").GetComponentsInChildren<BoxCollider>().Length,Is.EqualTo(36));}finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);scene=default;}
        }
        static IEnumerator Frame(){int frame=Time.frameCount;while(Time.frameCount==frame)yield return null;}
        [UnityTest,Timeout(180000)]public IEnumerator NativeInstancedDrawsChangeActualImageAndFrustumLodSelection()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(NextLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var motor=root.GetComponentInChildren<SalvageMotor>();motor.enabled=false;var echo=root.GetComponentInChildren<SalvageEcho>();echo.GetComponent<SalvageEchoCoordinator>().enabled=false;echo.enabled=false;motor.GetComponent<NextFocus>().enabled=false;root.GetComponent<NextFogField>().enabled=false;volume=root.GetComponentsInChildren<Volume>().Single(v=>v.name=="ES_Next_GlobalVolume");profile=volume.profile;profile.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;profile.TryGet<ChromaticAberration>(out var chromatic);chromatic.intensity.value=0;var camera=motor.View.GetComponent<Camera>();camera.scene=scene;camera.transform.position=new Vector3(5,1.6f,3);camera.transform.LookAt(new Vector3(5,.47f,.65f));var batches=root.GetComponentsInChildren<InstancedRoomDecor>();foreach(var batch in batches)batch.enabled=false;yield return Frame();Color[] off,on;string id=Guid.NewGuid().ToString("N");using(var capture=new SalvageFrameCapture(camera))off=capture.Capture(NextDecor.Evidence+"/Off_"+id+".png");int before=UnityStats.standardInstancedDrawCalls;foreach(var batch in batches){batch.enabled=true;batch.Submit(camera);}using(var capture=new SalvageFrameCapture(camera))on=capture.Capture(NextDecor.Evidence+"/On_"+id+".png");float difference=SalvageFrameCapture.Difference(off,on);int nativeCalls=0,nativeInstances=0,instancedBatches=0;for(int frame=0;frame<5;frame++){yield return Frame();nativeCalls=Math.Max(nativeCalls,UnityStats.standardInstancedDrawCalls);nativeInstances=Math.Max(nativeInstances,UnityStats.standardInstancedInstances);instancedBatches=Math.Max(instancedBatches,UnityStats.instancedBatches);}var lods=new JArray();foreach(var batch in batches)lods.Add(new JArray(Enumerable.Range(0,5).Select(batch.CountAt)));File.WriteAllText(NextDecor.Evidence+"/NativeAB_"+id+".json",new JObject{{"difference",difference},{"beforeNativeInstancedDraws",before},{"nativeInstancedDraws",nativeCalls},{"nativeInstancedInstances",nativeInstances},{"instancedBatches",instancedBatches},{"countsByRoomLod",lods},{"SRPBatcher",GraphicsSettings.useScriptableRenderPipelineBatching}}.ToString());Assert.That(difference,Is.GreaterThan(.00001f));JArray events=null;yield return NextFrameEvidence.Capture(()=>{foreach(var batch in batches)batch.Submit(camera);using(var capture=new SalvageFrameCapture(camera))capture.Capture(NextDecor.Evidence+"/Captured_"+id+".png");},NextDecor.Evidence+"/FD_"+id+".json",value=>events=value);var actual=events.Where(e=>e["type"].Value<string>()=="InstancedMesh"&&e["shader"].Value<string>()=="EchoStation/Next/InstancedDecor").ToArray();Assert.That(actual.Length,Is.GreaterThan(0),events.ToString());Assert.That(actual.Max(e=>e["instances"].Value<int>()),Is.GreaterThan(1));Assert.That(batches.Sum(b=>b.CountAt(0)),Is.GreaterThan(0));
            camera.transform.position=new Vector3(6,1.6f,-20);camera.transform.LookAt(new Vector3(6,.5f,12));foreach(var batch in batches)batch.Submit(camera);Assert.That(batches.Sum(b=>b.CountAt(2)+b.CountAt(3)),Is.GreaterThan(0));camera.transform.position=new Vector3(-100,2,-100);camera.transform.LookAt(new Vector3(-150,2,-150));foreach(var batch in batches){batch.Submit(camera);Assert.That(batch.LastBatches,Is.Zero);Assert.That(batch.LastInstances,Is.Zero);}
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(volume)volume.profile=null;if(profile){foreach(var c in profile.components)if(c)Object.Destroy(c);Object.Destroy(profile);}if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
