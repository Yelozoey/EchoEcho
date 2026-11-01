var verified=new System.Func<object>(delegate(){
EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
var baseline=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Docs/Tests/InfiltrationPrototype/Evidence/A01W/NativeBefore_A.json"));
var rows=new Newtonsoft.Json.Linq.JArray();
foreach(var entry in baseline.Properties())
{
    var obj=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(entry.Name).Single();
    rows.Add(new Newtonsoft.Json.Linq.JObject{{"path",entry.Name},{"nativeExact",UnityEditor.EditorJsonUtility.ToJson(obj)==(string)entry.Value["json"]},{"diskExact",System.IO.File.ReadAllBytes(entry.Name).SequenceEqual(System.Convert.FromBase64String((string)entry.Value["disk"]))},{"dirtyExact",UnityEditor.EditorUtility.IsDirty(obj)==(bool)entry.Value["dirty"]}});
}
var runtime=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Docs/Tests/InfiltrationPrototype/Evidence/A01W/RuntimeBefore_A.json"));
bool runtimeExact=UnityEngine.Application.runInBackground==(bool)runtime["background"]&&UnityEngine.Application.targetFrameRate==(int)runtime["fps"]&&UnityEngine.Time.timeScale==(float)runtime["scale"];
bool passed=rows.All(r=>(bool)r["nativeExact"]&&(bool)r["diskExact"]&&(bool)r["dirtyExact"]);
passed=passed&&runtimeExact;
return new {passed=passed,runtimeExact=runtimeExact,rows=rows,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,quality=UnityEngine.QualitySettings.GetQualityLevel(),aa=UnityEngine.QualitySettings.antiAliasing,playing=UnityEditor.EditorApplication.isPlaying,device=UnityEngine.SystemInfo.graphicsDeviceType.ToString()};

})();
var inventory=new System.Func<object>(delegate(){
EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(s.path!=EchoStation.InfiltrationWallPreview.Editor.WallPreviewScene.ScenePath||UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Unexpected handoff scene");
var root=EchoStation.InfiltrationMetal.Editor.MetalScene.World(s);var p=root.GetComponent<EchoStation.InfiltrationWallPreview.WallBrightnessPreview>();
return new {scene=s.path,dirty=s.isDirty,playing=UnityEditor.EditorApplication.isPlaying,missingScripts=root.GetComponentsInChildren<UnityEngine.Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),coated=p.Coated,mode=p.Mode,fillEnabled=p.grazing.enabled,fillIntensity=p.grazing.intensity,wallRenderers=p.surfaces.Length,material=UnityEditor.AssetDatabase.GetAssetPath(p.brighter),metallic=p.brighter.GetFloat("_Metallic"),smoothness=p.brighter.GetFloat("_Smoothness"),floors=root.GetComponentsInChildren<EchoStation.InfiltrationLevel.LevelFloor>().Length,netArea=EchoStation.InfiltrationLevel.LevelLayout.ConservativeNetArea(root),guards=root.GetComponentsInChildren<EchoStation.InfiltrationLevel.LevelPost>().Length,lightmaps=UnityEngine.LightmapSettings.lightmaps.Length,probes=root.GetComponentsInChildren<UnityEngine.ReflectionProbe>().Length,temporaryCameras=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Camera>().Count(c=>c.name.StartsWith("MM_PlanarCamera_")),api=UnityEngine.SystemInfo.graphicsDeviceType.ToString(),aiConnected=false};

})();
return new Newtonsoft.Json.Linq.JObject{{"native",Newtonsoft.Json.Linq.JObject.FromObject(verified)},{"inventory",Newtonsoft.Json.Linq.JObject.FromObject(inventory)}};