var verified=new System.Func<object>(delegate(){
EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
var baseline=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Docs/Tests/InfiltrationPrototype/Evidence/A01F/NativeBefore_A.json"));
var rows=new Newtonsoft.Json.Linq.JArray();
foreach(var entry in baseline.Properties())
{
    var obj=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(entry.Name).Single();
    rows.Add(new Newtonsoft.Json.Linq.JObject{{"path",entry.Name},{"nativeExact",UnityEditor.EditorJsonUtility.ToJson(obj)==(string)entry.Value["json"]},{"diskExact",System.IO.File.ReadAllBytes(entry.Name).SequenceEqual(System.Convert.FromBase64String((string)entry.Value["disk"]))},{"dirtyExact",UnityEditor.EditorUtility.IsDirty(obj)==(bool)entry.Value["dirty"]}});
}
var runtime=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Docs/Tests/InfiltrationPrototype/Evidence/A01F/RuntimeBefore_A.json"));
bool runtimeExact=UnityEngine.Application.runInBackground==(bool)runtime["background"]&&UnityEngine.Application.targetFrameRate==(int)runtime["fps"]&&UnityEngine.Time.timeScale==(float)runtime["scale"];
bool passed=rows.All(r=>(bool)r["nativeExact"]&&(bool)r["diskExact"]&&(bool)r["dirtyExact"]);
passed=passed&&runtimeExact;
return new {passed=passed,runtimeExact=runtimeExact,rows=rows,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,quality=UnityEngine.QualitySettings.GetQualityLevel(),aa=UnityEngine.QualitySettings.antiAliasing,playing=UnityEditor.EditorApplication.isPlaying,device=UnityEngine.SystemInfo.graphicsDeviceType.ToString()};

})();
var inventory=new System.Func<object>(delegate(){
EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!=EchoStation.InfiltrationCoated.Editor.CoatScene.ScenePath||UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Preserve unexpected workspace");
var root=EchoStation.InfiltrationCoated.Editor.CoatScene.World(scene);
var objects=root.GetComponentsInChildren<UnityEngine.Transform>(true);
var probes=root.GetComponentsInChildren<UnityEngine.ReflectionProbe>();
var planes=root.GetComponentsInChildren<EchoStation.InfiltrationCoated.CoatPlanar>();
var camera=root.GetComponentInChildren<UnityEngine.Camera>();
var pipeline=new UnityEditor.SerializedObject(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(EchoStation.InfiltrationCoated.Editor.CoatScene.PipelinePath));
var list=pipeline.FindProperty("m_RendererDataList");
var main=(UnityEngine.Rendering.Universal.UniversalRendererData)list.GetArrayElementAtIndex(0).objectReferenceValue;
var auxiliary=(UnityEngine.Rendering.Universal.UniversalRendererData)list.GetArrayElementAtIndex(1).objectReferenceValue;
var result=new {scene=scene.path,dirty=scene.isDirty,playing=UnityEditor.EditorApplication.isPlaying,missingScripts=objects.Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),floors=root.GetComponentsInChildren<EchoStation.InfiltrationLevel.LevelFloor>().Length,netArea=EchoStation.InfiltrationLevel.LevelLayout.ConservativeNetArea(root),posts=root.GetComponentsInChildren<EchoStation.InfiltrationLevel.LevelPost>().Select(p=>new{id=p.Id,futurePatrol=p.FuturePatrol}).ToArray(),decorations=root.GetComponentsInChildren<EchoStation.InfiltrationRefinement.RefinementDecoration>().Length,mainCamera=camera.name,cameraHdr=camera.allowHDR,displayHdr=camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().allowHDROutput,planes=planes.Length,planeTextureAllocated=planes.Any(p=>p.LastTexture),temporaryCameras=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Camera>().Count(c=>c.name.StartsWith("CF_PlanarCamera_")),probes=probes.Select(p=>new{name=p.name,resolution=p.resolution,asset=UnityEditor.AssetDatabase.GetAssetPath(p.customBakedTexture)}).ToArray(),lightmaps=UnityEngine.LightmapSettings.lightmaps.Length,mainFeatures=main.rendererFeatures.Count(f=>f&&f.isActive),auxiliaryFeatures=auxiliary.rendererFeatures.Count,api=UnityEngine.SystemInfo.graphicsDeviceType.ToString(),aiConnected=false};
return result;

})();
return new Newtonsoft.Json.Linq.JObject{{"native",Newtonsoft.Json.Linq.JObject.FromObject(verified)},{"inventory",Newtonsoft.Json.Linq.JObject.FromObject(inventory)}};