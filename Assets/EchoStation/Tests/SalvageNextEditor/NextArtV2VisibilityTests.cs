using System;
using System.Collections;
using System.IO;
using System.Linq;
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
    public sealed class NextArtV2VisibilityTests
    {
        static Scene previous,main;static GameObject root;static Camera camera;static NextRoomStreaming stream;static bool background,aoActive;static ScriptableRendererFeature ao;static Volume volume;static VolumeProfile profile;static JArray last;static void Received(JArray events)=>last=events;
        static IEnumerator Wait(Func<bool> condition,string stage){var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(15),stage);yield return null;}}
        [Test]public static void AllFourSceneIdsShareNewPvsAndTheMainDoesNotRetainRoomMeshesOrMaps()
        {
            string guid=AssetDatabase.AssetPathToGUID(NextArtV2Occlusion.DataPath);Assert.That(guid,Is.Not.Empty);foreach(var path in Enumerable.Range(0,3).Select(NextArtV2Stream.RoomPath).Prepend(NextArtV2Stream.Main))Assert.That(File.ReadAllText(path),Does.Contain("m_OcclusionCullingData: {fileID: 36300000, guid: "+guid));string data=File.ReadAllText(NextArtV2Occlusion.DataPath);Assert.That(data.Length,Is.GreaterThan(1000));Assert.That(data,Does.Contain("m_PVSData:"));var dependencies=AssetDatabase.GetDependencies(NextArtV2Stream.Main,true);Assert.That(dependencies,Does.Contain(NextArtV2Occlusion.DataPath));Assert.That(dependencies.Any(p=>p.StartsWith((NextArtV2Layout.Generated+"/Maps")+"/",StringComparison.Ordinal)||p.StartsWith(NextArtV2Layout.Generated+"/Meshes_B",StringComparison.Ordinal)),Is.False);var group=NextAddressables.Settings.groups.Single(g=>g&&g.Name==NextArtV2Occlusion.Group);Assert.That(group.entries.Single().AssetPath,Is.EqualTo(NextArtV2Occlusion.DataPath));
        }
        static IEnumerator Boot()
        {
            background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();main=EditorSceneManager.LoadSceneInPlayMode(NextArtV2Stream.Main,new LoadSceneParameters(LoadSceneMode.Additive));while(!main.isLoaded)yield return null;SceneManager.SetActiveScene(main);root=main.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var fontManager=root.GetComponentInChildren<NextWorldLabels>();if(fontManager)fontManager.enabled=false;stream=root.GetComponentInChildren<NextRoomStreaming>();var motor=stream.GetComponent<SalvageMotor>();motor.enabled=false;var echo=root.GetComponentInChildren<SalvageEcho>();var brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;brain.AutomaticWarnings=false;Assert.That(brain.Initialize(),Is.True);brain.ResetBase(EchoStation.Echo.IntentKind.Wait);
            yield return Wait(()=>stream.Owner.IsReady(0)&&stream.Owner.IsReady(1),"Initial rooms ready");Assert.That(echo.Navigation.Agent.Warp(new Vector3(35,0,12)),Is.True);stream.EvaluateNow();yield return Wait(()=>stream.Owner.IsReady(0)&&stream.Owner.IsReady(1)&&stream.Owner.IsReady(2),"All three actual scene bundles ready");
            foreach(var text in root.GetComponentsInChildren<TextMesh>())text.GetComponent<Renderer>().enabled=false;echo.GetComponent<EchoStation.Echo.EchoPresentation>().enabled=false;foreach(var r in echo.GetComponentsInChildren<Renderer>(true))r.enabled=false;foreach(var cargo in root.GetComponentsInChildren<CargoActor>())foreach(var r in cargo.GetComponentsInChildren<Renderer>(true))r.enabled=false;for(int i=0;i<3;i++){stream.Room(i).GetComponent<InstancedRoomDecor>().enabled=false;var lamps=stream.Room(i).GetComponent<InstancedRoomLamps>();if(lamps)lamps.enabled=false;}
            stream.GetComponent<NextFocus>().enabled=false;root.GetComponent<NextFogField>().enabled=false;foreach(var light in root.GetComponentsInChildren<Light>())light.enabled=false;foreach(var flicker in root.GetComponentsInChildren<FaultLightFlicker>())flicker.enabled=false;camera=motor.View.GetComponent<Camera>();Assert.That(camera.scene.IsValid(),Is.False,"Main camera must render additive rooms");volume=root.GetComponentsInChildren<Volume>().Single(v=>v.name=="ES_Next_GlobalVolume");profile=volume.profile;profile.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;profile.TryGet<ChromaticAberration>(out var chromatic);chromatic.intensity.value=0;ao=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(NextRendering.RendererPath).rendererFeatures.Single(f=>f.name.Contains("AmbientOcclusion"));aoActive=ao.isActive;ao.SetActive(false);
        }
        static IEnumerator Settle(){for(int i=0;i<10;i++)yield return null;}
        static int Draws(JArray events)=>events.Where(e=>e["label"].Value<string>().Contains("DrawOpaqueObjects")&&new[]{"SRPBatch","InstancedMesh","Mesh"}.Contains(e["type"].Value<string>())).Sum(e=>e["draws"].Value<int>());
        static IEnumerator Capture(string name){yield return NextFrameEvidence.Capture(()=>{using(var image=new SalvageFrameCapture(camera))image.Capture(NextArtV2Occlusion.Evidence+"/FDImage_"+name+".png");},NextArtV2Occlusion.Evidence+"/FD_"+name+".json",Received);}
        [UnityTest,Timeout(240000)]public static IEnumerator ActualAdditiveBundlesUseRebuiltPvsWithFewerNativeDrawsAndNoVisibleLoss()
        {
            yield return new EnterPlayMode();yield return Boot();string id=Guid.NewGuid().ToString("N");camera.transform.position=new Vector3(5,1.6f,12);camera.transform.LookAt(new Vector3(35,1.6f,12));camera.useOcclusionCulling=false;yield return Settle();Color[] warm,off,stable,on;using(var image=new SalvageFrameCapture(camera)){warm=image.Capture(NextArtV2Occlusion.Evidence+"/Warm_"+id+".png");off=image.Capture(NextArtV2Occlusion.Evidence+"/Off_"+id+".png");stable=image.Capture(NextArtV2Occlusion.Evidence+"/Stable_"+id+".png");}Assert.That(SalvageFrameCapture.Difference(off,stable),Is.LessThan(.000001f),"Off-Off stable");yield return Capture("Off_"+id);var offEvents=last;camera.useOcclusionCulling=true;yield return Settle();using(var image=new SalvageFrameCapture(camera))on=image.Capture(NextArtV2Occlusion.Evidence+"/On_"+id+".png");yield return Capture("On_"+id);var onEvents=last;camera.useOcclusionCulling=false;camera.transform.position=new Vector3(-5,1.6f,-5);camera.transform.LookAt(new Vector3(-10,1.6f,-10));yield return Settle();yield return Capture("Away_"+id);var awayEvents=last;
            var row=new JObject{{"offDraws",Draws(offEvents)},{"onDraws",Draws(onEvents)},{"awayDraws",Draws(awayEvents)},{"imageDifference",SalvageFrameCapture.Difference(off,on)},{"offOffDifference",SalvageFrameCapture.Difference(off,stable)},{"warmDifference",SalvageFrameCapture.Difference(warm,off)},{"meanBrightness",off.Average(p=>(p.r+p.g+p.b)/3)},{"actualAddressableRooms",3},{"source","Native FrameDebugger event details"}};File.WriteAllText(NextArtV2Occlusion.Evidence+"/Visibility_"+id+".json",row.ToString());Assert.That(row["meanBrightness"].Value<double>(),Is.GreaterThan(.001));Assert.That(row["imageDifference"].Value<float>(),Is.LessThan(.000001f));Assert.That(Draws(offEvents),Is.GreaterThan(Draws(onEvents)),row.ToString());Assert.That(Draws(onEvents),Is.GreaterThan(0));Assert.That(Draws(awayEvents),Is.LessThan(Draws(offEvents)));
        }
        [UnityTearDown]public static IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;if(ao)ao.SetActive(aoActive);if(volume)volume.profile=null;if(profile){foreach(var c in profile.components)if(c)Object.Destroy(c);Object.Destroy(profile);}var owner=stream?stream.Owner:null;if(previous.IsValid())SceneManager.SetActiveScene(previous);if(main.IsValid()&&main.isLoaded){var unload=SceneManager.UnloadSceneAsync(main);while(unload!=null&&!unload.isDone)yield return null;}if(owner!=null)yield return Wait(()=>owner.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0,"Visibility cleanup");Application.runInBackground=background;yield return new ExitPlayMode();
        }
    }
}
