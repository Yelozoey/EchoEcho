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
    public sealed class NextArtV2PresentationTests
    {
        const string Evidence="Docs/Tests/SalvagePrototype/Evidence/Feedback02/V04/Presentation";
        RoomSceneOwner owner;RoomDemandPlanner policy;RoomPresenter presenter;Scene previous,main;GameObject root;Camera camera;bool background,aoActive;ScriptableRendererFeature ao;Volume volume;VolumeProfile profile;LightmapData[] prior;
        static IEnumerator Wait(Func<bool> condition,string stage){var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(12),stage);yield return null;}}
        IEnumerator Boot()
        {
            background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();main=EditorSceneManager.LoadSceneInPlayMode(NextArtV2Stream.Main,new LoadSceneParameters(LoadSceneMode.Additive));while(!main.isLoaded)yield return null;SceneManager.SetActiveScene(main);yield return null;
            root=main.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var fontManager=root.GetComponentInChildren<NextWorldLabels>();if(fontManager)fontManager.enabled=false;var automatic=root.GetComponentInChildren<NextRoomStreaming>();if(automatic){var initialOwner=automatic.Owner;automatic.enabled=false;yield return Wait(()=>initialOwner.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0,"Automatic owner closes before isolated presentation fixture");}camera=root.GetComponentInChildren<Camera>();camera.useOcclusionCulling=false;
            root.GetComponentInChildren<SalvageMotor>().enabled=false;var echo=root.GetComponentInChildren<SalvageEcho>();echo.GetComponent<SalvageEchoCoordinator>().enabled=false;echo.enabled=false;echo.GetComponent<EchoStation.Echo.EchoPresentation>().enabled=false;
            root.GetComponentInChildren<NextFocus>().enabled=false;root.GetComponent<NextFogField>().enabled=false;foreach(var light in root.GetComponentsInChildren<Light>())light.enabled=false;foreach(var flicker in root.GetComponentsInChildren<FaultLightFlicker>())flicker.enabled=false;
            foreach(var text in root.GetComponentsInChildren<TextMesh>())text.GetComponent<Renderer>().enabled=false;foreach(var r in echo.GetComponentsInChildren<Renderer>(true))r.enabled=false;foreach(var cargo in root.GetComponentsInChildren<CargoActor>())foreach(var r in cargo.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            volume=root.GetComponentsInChildren<Volume>().Single(v=>v.name=="ES_Next_GlobalVolume");profile=volume.profile;profile.TryGet<FilmGrain>(out var grain);grain.intensity.value=0;profile.TryGet<ChromaticAberration>(out var chromatic);chromatic.intensity.value=0;
            ao=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(NextRendering.RendererPath).rendererFeatures.Single(f=>f.name.Contains("AmbientOcclusion"));aoActive=ao.isActive;ao.SetActive(false);
            prior=LightmapSettings.lightmaps;presenter=new RoomPresenter(camera);policy=new RoomDemandPlanner();policy.Refresh(1,1,new StreamPoint(0,1,12),new StreamPoint(36,1,12),new StreamTaskLease(1,1,new StreamPoint(18,1,12)),new bool[3]);owner=RoomSceneOwner.Create(root,policy,presenter.Show,presenter.Hide,NextArtV2Stream.Prefix);Directory.CreateDirectory(Evidence);
            yield return Wait(()=>owner.IsReady(0)&&owner.IsReady(1)&&owner.IsReady(2),"All real room packages ready");for(int i=0;i<3;i++){presenter.Room(i).GetComponent<InstancedRoomDecor>().enabled=false;var lamps=presenter.Room(i).GetComponent<InstancedRoomLamps>();if(lamps)lamps.enabled=false;}for(int frame=0;frame<8;frame++)yield return null;Assert.That(owner.Failures,Is.Zero,owner.LastError);
        }
        void Bindings()
        {
            Assert.That(LightmapSettings.lightmaps.Length,Is.EqualTo(prior.Length+6));Assert.That(LightmapSettings.lightProbes,Is.SameAs(root.GetComponent<NextProbeScope>().Probes));
            for(int i=0;i<3;i++){var room=presenter.Room(i);Assert.That(room.gameObject.activeInHierarchy,Is.True);Assert.That(room.GetComponent<InstancedRoomDecor>().View,Is.SameAs(camera));Assert.That(room.GetComponent<DistanceShadowCaster>().TargetCamera,Is.SameAs(camera));for(int r=0;r<room.RendererCount;r++){var renderer=room.Renderer(r);int index=prior.Length+i*2+room.Atlas(r);Assert.That(renderer.lightmapIndex,Is.EqualTo(index),"Start/OnEnable must retain global mapping");Assert.That(LightmapSettings.lightmaps[index].lightmapColor,Is.SameAs(room.ColorMap(room.Atlas(r))));Assert.That(renderer.lightmapScaleOffset,Is.EqualTo(new Vector4(1,1,0,0)));}}
        }
        [UnityTest,Timeout(180000)]public IEnumerator ThreeActualSceneBundlesKeepCorrectDirectionalSlotsAcrossStartAndMatchSharedAtlasRendering()
        {
            yield return new EnterPlayMode();yield return Boot();Bindings();Assert.Throws<InvalidOperationException>(()=>new RoomLightingRegistry());
            var pairs=LightmapSettings.lightmaps;var shared=new[]{new LightmapData{lightmapColor=presenter.Room(0).ColorMap(0),lightmapDir=presenter.Room(0).DirectionMap(0)},new LightmapData{lightmapColor=presenter.Room(0).ColorMap(1),lightmapDir=presenter.Room(0).DirectionMap(1)}};
            var registry=typeof(RoomPresenter).GetField("lighting",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(presenter) as RoomLightingRegistry;
            var rows=new JArray();string id=Guid.NewGuid().ToString("N");
            for(int room=0;room<3;room++)
            {
                camera.transform.position=new Vector3(room*12+5,1.6f,12);camera.transform.LookAt(new Vector3(room*12+10,1.6f,16));
                for(int i=0;i<3;i++){var package=presenter.Room(i);for(int r=0;r<package.RendererCount;r++)package.Renderer(r).lightmapIndex=package.Atlas(r);}LightmapSettings.lightmaps=shared;
                Color[] warm,reference,actual;using(var capture=new SalvageFrameCapture(camera)){warm=capture.Capture(Evidence+"/Warm_"+room+"_"+id+".png");reference=capture.Capture(Evidence+"/Shared_"+room+"_"+id+".png");}
                registry.Register(presenter.Room(room));Bindings();using(var capture=new SalvageFrameCapture(camera))actual=capture.Capture(Evidence+"/Partitioned_"+room+"_"+id+".png");
                float difference=SalvageFrameCapture.Difference(reference,actual);double brightness=actual.Average(p=>(p.r+p.g+p.b)/3);rows.Add(new JObject{{"room",room},{"difference",difference},{"brightness",brightness},{"warmDifference",SalvageFrameCapture.Difference(warm,reference)}});
                if(brightness<=.001){var clones=new System.Collections.Generic.List<Material>();var records=new JArray();try{for(int i=0;i<3;i++){var package=presenter.Room(i);for(int r=0;r<package.RendererCount;r++){var renderer=package.Renderer(r);records.Add(new JObject{{"renderer",renderer.name},{"index",renderer.lightmapIndex},{"receiveGI",renderer.receiveGI.ToString()},{"shader",renderer.sharedMaterial.shader.name},{"shaderAssetPath",AssetDatabase.GetAssetPath(renderer.sharedMaterial.shader)},{"shaderSupported",renderer.sharedMaterial.shader.isSupported}});renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{var clone=new Material(m);clone.shader=Shader.Find("Universal Render Pipeline/Lit");clones.Add(clone);return clone;}).ToArray();}}Color[] swapped;using(var capture=new SalvageFrameCapture(camera))swapped=capture.Capture(Evidence+"/EditorShader_"+id+".png");File.WriteAllText(Evidence+"/DarkDiagnostic_"+id+".json",new JObject{{"brightness",brightness},{"editorShaderBrightness",swapped.Average(p=>(p.r+p.g+p.b)/3)},{"shaderSwapDifference",SalvageFrameCapture.Difference(actual,swapped)},{"renderers",records}}.ToString());}finally{foreach(var clone in clones)Object.Destroy(clone);}}
                Assert.That(brightness,Is.GreaterThan(.001));Assert.That(difference,Is.LessThan(.000001f));
            }
            var goneMap=presenter.Room(0).ColorMap(0);var goneMesh=presenter.Room(0).Renderer(0).GetComponent<MeshFilter>().sharedMesh;var sharedMaterial=presenter.Room(1).Renderer(0).sharedMaterial;
            File.WriteAllText(Evidence+"/Comparisons_"+id+".json",rows.ToString());policy.Refresh(1,1,new StreamPoint(36,1,12),new StreamPoint(36,1,12),null,new[]{true,true,true});yield return Wait(()=>owner.Phase(0)==RoomScenePhase.Empty,"Retire room zero");Assert.That(presenter.Room(0),Is.Null);Assert.That(LightmapSettings.lightmaps[prior.Length].lightmapColor,Is.Null);var unused=Resources.UnloadUnusedAssets();while(!unused.isDone)yield return null;yield return Wait(()=>goneMap==null&&goneMesh==null,"Room zero native textures/mesh released");Assert.That(sharedMaterial!=null,Is.True,"Shared material must remain for other rooms");Assert.That(LightmapSettings.lightProbes,Is.SameAs(root.GetComponent<NextProbeScope>().Probes));for(int i=1;i<3;i++){var package=presenter.Room(i);Assert.That(package.Renderer(0).lightmapIndex,Is.EqualTo(prior.Length+i*2+package.Atlas(0)));}
            File.WriteAllText(Evidence+"/Rendering_"+id+".json",new JObject{{"comparisons",rows},{"nativeSlots",pairs.Length},{"allFiftyTwoRendererBindings",true},{"initialStartRetainedSlots",true},{"roomZeroUniqueObjectsReleased",true},{"sharedMaterialRetained",true},{"probeCountAfterUnload",LightmapSettings.lightProbes.count},{"singletonGuard",true}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;if(owner!=null){owner.Dispose();yield return Wait(()=>owner.IsClosed,"Owner cleanup");}presenter?.Dispose();if(prior!=null)Assert.That(LightmapSettings.lightmaps.Length,Is.EqualTo(prior.Length));if(ao)ao.SetActive(aoActive);if(volume)volume.profile=null;if(profile){foreach(var c in profile.components)if(c)Object.Destroy(c);Object.Destroy(profile);}if(previous.IsValid())SceneManager.SetActiveScene(previous);if(main.IsValid()){var unload=SceneManager.UnloadSceneAsync(main);while(unload!=null&&!unload.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();
        }
    }
}
