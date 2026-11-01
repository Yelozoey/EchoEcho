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
    public sealed class NextVisibilityTests
    {
        static JArray lastCapture;static void Received(JArray value){lastCapture=value;}
        static Scene old,scene;
static bool background;
static Volume volume;
static VolumeProfile profile;
static GameObject root;
static Camera camera;
static UniversalRendererData renderer;
static ScriptableRendererFeature ao;
static bool aoActive;

        [Test]public static void SceneReferencesRealPvsAndCameraEnablesOcclusion()
        {string text=File.ReadAllText(NextLayout.ScenePath);
Assert.That(text,Does.Contain("smallestOccluder: 1"));
Assert.That(text,Does.Contain("m_OcclusionCullingData: {fileID: 36300000, guid:"));
string data=File.ReadAllText(NextLayout.ScenePath.Replace(".unity","/OcclusionCullingData.asset"));
Assert.That(data.Length,Is.GreaterThan(1000));
Assert.That(data,Does.Contain("m_PVSData:"));
}
        static IEnumerator Boot()
        {
            background=Application.runInBackground;
Application.runInBackground=true;
old=SceneManager.GetActiveScene();
scene=EditorSceneManager.LoadSceneInPlayMode(NextLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));
while(!scene.isLoaded)yield return null;
SceneManager.SetActiveScene(scene);
yield return null;
root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");
root.GetComponentInChildren<SalvageMotor>().enabled=false;
var echo=root.GetComponentInChildren<SalvageEcho>();
echo.GetComponent<SalvageEchoCoordinator>().enabled=false;
echo.enabled=false;
root.GetComponentInChildren<NextFocus>().enabled=false;
root.GetComponent<NextFogField>().enabled=false;
foreach(var decor in root.GetComponentsInChildren<InstancedRoomDecor>())decor.enabled=false;
foreach(var light in root.GetComponentsInChildren<Light>())light.enabled=false;
foreach(var flicker in root.GetComponentsInChildren<EchoStation.Rendering.FaultLightFlicker>())flicker.enabled=false;
volume=root.GetComponentsInChildren<Volume>().Single(v=>v.name=="ES_Next_GlobalVolume");
profile=volume.profile;
profile.TryGet<FilmGrain>(out var grain);
grain.intensity.value=0;
profile.TryGet<ChromaticAberration>(out var chromatic);
chromatic.intensity.value=0;
camera=root.GetComponentInChildren<Camera>();
camera.scene=scene;
renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(NextRendering.RendererPath);
ao=renderer.rendererFeatures.Single(f=>f.name.Contains("AmbientOcclusion"));
aoActive=ao.isActive;
ao.SetActive(false);

        }
        static IEnumerator Settle(){for(int i=0;
i<10;
i++){int frame=Time.frameCount;
while(Time.frameCount==frame)yield return null;
}}
        static int Draws(JArray events)=>events.Where(e=>e["label"].Value<string>().Contains("DrawOpaqueObjects")&&new[]{"SRPBatch","InstancedMesh","Mesh"}.Contains(e["type"].Value<string>())).Sum(e=>e["draws"].Value<int>());

        static IEnumerator Capture(string name,Action<JArray> done){yield return NextFrameEvidence.Capture(()=>{using(var capture=new SalvageFrameCapture(camera))capture.Capture(NextOcclusion.Evidence+"/FDImage_"+name+".png");
},NextOcclusion.Evidence+"/FD_"+name+".json",done);
}
        [UnityTest,Timeout(240000)]public static IEnumerator WallOcclusionReducesRealOpaqueDrawsWithoutImageLossAndFrustumRejectsOutside()
        {
            if(SessionState.GetInt("ES.Next.Visibility.OptionsEnabled",-1)<0){SessionState.SetInt("ES.Next.Visibility.OptionsEnabled",EditorSettings.enterPlayModeOptionsEnabled?1:0);
SessionState.SetInt("ES.Next.Visibility.OptionsFlags",(int)EditorSettings.enterPlayModeOptions);
var settings=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/EditorSettings.asset").Single();
SessionState.SetString("ES.Next.Visibility.SettingsJson",EditorJsonUtility.ToJson(settings));
SessionState.SetBool("ES.Next.Visibility.SettingsDirty",EditorUtility.IsDirty(settings));
}EditorSettings.enterPlayModeOptionsEnabled=false;
Phase("BeforeEnter");
yield return new EnterPlayMode();
Phase("AfterEnter");
yield return Boot();
Phase("AfterBoot");
scene=SceneManager.GetSceneByPath(NextLayout.ScenePath);
Assert.That(scene.IsValid()&&scene.isLoaded,Is.True,"Boot scene missing after domain reload");
root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");
Phase("RootFound");
camera=root.GetComponentInChildren<Camera>();
Assert.That(camera,Is.Not.Null,"Camera missing after domain reload Boot");
Phase("CameraFound");foreach(var text in root.GetComponentsInChildren<TextMesh>())text.GetComponent<Renderer>().enabled=false;var echoVisual=root.GetComponentInChildren<SalvageEcho>();echoVisual.GetComponent<EchoStation.Echo.EchoPresentation>().enabled=false;foreach(var r in echoVisual.GetComponentsInChildren<Renderer>(true))r.enabled=false;foreach(var cargo in root.GetComponentsInChildren<CargoActor>())foreach(var r in cargo.GetComponentsInChildren<Renderer>(true))r.enabled=false;
camera.transform.position=new Vector3(5,1.6f,12);
camera.transform.LookAt(new Vector3(35,1.6f,12));
camera.useOcclusionCulling=false;
yield return Settle();
string id=Guid.NewGuid().ToString("N");
Color[] warm,off,stable,on;using(var capture=new SalvageFrameCapture(camera))warm=capture.Capture(NextOcclusion.Evidence+"/Warm_"+id+".png");
using(var capture=new SalvageFrameCapture(camera))off=capture.Capture(NextOcclusion.Evidence+"/Off_"+id+".png");
using(var capture=new SalvageFrameCapture(camera))stable=capture.Capture(NextOcclusion.Evidence+"/Stable_"+id+".png");float stableDifference=SalvageFrameCapture.Difference(off,stable);Assert.That(stableDifference,Is.LessThan(.000001f),"Off-Off baseline unstable");JArray a,b,c;
yield return Capture("Off_"+id,Received);a=lastCapture;
camera.useOcclusionCulling=true;
yield return Settle();
using(var capture=new SalvageFrameCapture(camera))on=capture.Capture(NextOcclusion.Evidence+"/On_"+id+".png");
yield return Capture("On_"+id,Received);b=lastCapture;
camera.useOcclusionCulling=false;
camera.transform.position=new Vector3(-5,1.6f,-5);
camera.transform.LookAt(new Vector3(-10,1.6f,-10));
yield return Settle();
yield return Capture("Away_"+id,Received);c=lastCapture;
float difference=SalvageFrameCapture.Difference(off,on);
var row=new JObject{{"offDraws",Draws(a)},{"onDraws",Draws(b)},{"awayDraws",Draws(c)},{"difference",difference},{"warmDifference",SalvageFrameCapture.Difference(warm,off)},{"offOffDifference",stableDifference},{"staticGeometryIsolated",true},{"meanBrightness",off.Average(p=>(p.r+p.g+p.b)/3)}};
File.WriteAllText(NextOcclusion.Evidence+"/Visibility_"+id+".json",row.ToString());
Assert.That(row["meanBrightness"].Value<double>(),Is.GreaterThan(.001));
Assert.That(difference,Is.LessThan(.000001f));
Assert.That(Draws(a),Is.GreaterThan(Draws(b)),row.ToString());
Assert.That(Draws(b),Is.GreaterThan(0));
Assert.That(Draws(c),Is.LessThan(Draws(a)));

        }
        [UnityTest,Timeout(180000)]public static IEnumerator RealCargoUsesSrpBatcherAndMovementChangesImage()
        {
            yield return new EnterPlayMode();
yield return Boot();
camera.useOcclusionCulling=false;
camera.transform.position=new Vector3(9,1.6f,16.5f);
camera.transform.LookAt(new Vector3(9,.3f,19));
var actors=root.GetComponentsInChildren<CargoActor>();
var dynamic=actors.SelectMany(a=>a.GetComponentsInChildren<Renderer>(true)).Concat(root.GetComponentInChildren<RoverModelParts>().GetComponentsInChildren<Renderer>(true)).ToArray();
var flags=dynamic.Select(r=>r.enabled).ToArray();
var actor=actors.Single(a=>a.Id=="cargo_a");
var visual=actor.GetComponentInChildren<CargoVisualModel>().GetComponentsInChildren<MeshRenderer>();
string id=Guid.NewGuid().ToString("N");
JArray absent=null,present=null;
var position=actor.transform.position;

            try{foreach(var r in dynamic)r.enabled=false;
yield return Settle();
yield return Capture("CargoAbsent_"+id,v=>absent=v);
foreach(var r in visual)r.enabled=true;
yield return Settle();
Color[] before,after;
using(var capture=new SalvageFrameCapture(camera))before=capture.Capture(NextOcclusion.Evidence+"/Cargo_"+id+".png");
yield return Capture("CargoPresent_"+id,v=>present=v);
actor.transform.position+=Vector3.right*.4f;
yield return Settle();
using(var capture=new SalvageFrameCapture(camera))after=capture.Capture(NextOcclusion.Evidence+"/CargoMoved_"+id+".png");
int beforeBatches=absent.Count(e=>e["type"].Value<string>()=="SRPBatch"&&e["label"].Value<string>().Contains("DrawOpaqueObjects")),afterBatches=present.Count(e=>e["type"].Value<string>()=="SRPBatch"&&e["label"].Value<string>().Contains("DrawOpaqueObjects"));
float difference=SalvageFrameCapture.Difference(before,after);
File.WriteAllText(NextOcclusion.Evidence+"/Dynamic_"+id+".json",new JObject{{"absentSrpEvents",beforeBatches},{"presentSrpEvents",afterBatches},{"movementDifference",difference},{"SRPBatcher",GraphicsSettings.useScriptableRenderPipelineBatching}}.ToString());
Assert.That(GraphicsSettings.useScriptableRenderPipelineBatching,Is.True);
Assert.That(afterBatches,Is.GreaterThan(beforeBatches));
Assert.That(difference,Is.GreaterThan(.00001f));
}
            finally{actor.transform.position=position;
for(int i=0;
i<dynamic.Length;
i++)if(dynamic[i])dynamic[i].enabled=flags[i];
}
        }
        static void Phase(string phase){File.AppendAllText(NextOcclusion.Evidence+"/DomainProgress.log",DateTime.UtcNow.ToString("O")+" "+phase+" playing="+Application.isPlaying+" scenes="+SceneManager.sceneCount+"\n");
}
        static void RestoreOptions(){int enabled=SessionState.GetInt("ES.Next.Visibility.OptionsEnabled",-1);
if(enabled<0)return;
EditorSettings.enterPlayModeOptions=(EnterPlayModeOptions)SessionState.GetInt("ES.Next.Visibility.OptionsFlags",1);
EditorSettings.enterPlayModeOptionsEnabled=enabled==1;
var settings=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/EditorSettings.asset").Single();
Assert.That(EditorJsonUtility.ToJson(settings),Is.EqualTo(SessionState.GetString("ES.Next.Visibility.SettingsJson","missing")),"Editor options must restore the entire native settings snapshot.");
if(SessionState.GetBool("ES.Next.Visibility.SettingsDirty",true))EditorUtility.SetDirty(settings);else EditorUtility.ClearDirty(settings);
SessionState.EraseInt("ES.Next.Visibility.OptionsEnabled");
SessionState.EraseInt("ES.Next.Visibility.OptionsFlags");
SessionState.EraseString("ES.Next.Visibility.SettingsJson");
SessionState.EraseBool("ES.Next.Visibility.SettingsDirty");
}
        [UnityTearDown]public static IEnumerator Cleanup(){if(!Application.isPlaying){RestoreOptions();
yield break;
}if(ao)ao.SetActive(aoActive);
if(volume)volume.profile=null;
if(profile){foreach(var c in profile.components)if(c)Object.Destroy(c);
Object.Destroy(profile);
}if(old.IsValid())SceneManager.SetActiveScene(old);
if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);
while(op!=null&&!op.isDone)yield return null;
}Application.runInBackground=background;
yield return new ExitPlayMode();
RestoreOptions();
}
    }
}
