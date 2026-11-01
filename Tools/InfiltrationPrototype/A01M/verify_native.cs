EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
var baseline=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Docs/Tests/InfiltrationPrototype/Evidence/A01M/NativeBefore_A.json"));
var rows=new Newtonsoft.Json.Linq.JArray();
foreach(var entry in baseline.Properties())
{
    var obj=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(entry.Name).Single();
    rows.Add(new Newtonsoft.Json.Linq.JObject{{"path",entry.Name},{"nativeExact",UnityEditor.EditorJsonUtility.ToJson(obj)==(string)entry.Value["json"]},{"diskExact",System.IO.File.ReadAllBytes(entry.Name).SequenceEqual(System.Convert.FromBase64String((string)entry.Value["disk"]))},{"dirtyExact",UnityEditor.EditorUtility.IsDirty(obj)==(bool)entry.Value["dirty"]}});
}
var runtime=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Docs/Tests/InfiltrationPrototype/Evidence/A01M/RuntimeBefore_A.json"));
bool runtimeExact=UnityEngine.Application.runInBackground==(bool)runtime["background"]&&UnityEngine.Application.targetFrameRate==(int)runtime["fps"]&&UnityEngine.Time.timeScale==(float)runtime["scale"];
bool passed=rows.All(r=>(bool)r["nativeExact"]&&(bool)r["diskExact"]&&(bool)r["dirtyExact"]);
passed=passed&&runtimeExact;
return new {passed=passed,runtimeExact=runtimeExact,rows=rows,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,quality=UnityEngine.QualitySettings.GetQualityLevel(),aa=UnityEngine.QualitySettings.antiAliasing,playing=UnityEditor.EditorApplication.isPlaying,device=UnityEngine.SystemInfo.graphicsDeviceType.ToString()};
