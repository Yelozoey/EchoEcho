EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
var baseline=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Docs/Tests/InfiltrationPrototype/Evidence/A01R/NativeBefore_A.json"));
var rows=new Newtonsoft.Json.Linq.JArray();
foreach(var entry in baseline.Properties())
{
    var obj=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(entry.Name).Single();
    rows.Add(new Newtonsoft.Json.Linq.JObject{{"path",entry.Name},{"nativeExact",UnityEditor.EditorJsonUtility.ToJson(obj)==(string)entry.Value["json"]},{"diskExact",System.IO.File.ReadAllBytes(entry.Name).SequenceEqual(System.Convert.FromBase64String((string)entry.Value["disk"]))},{"dirtyExact",UnityEditor.EditorUtility.IsDirty(obj)==(bool)entry.Value["dirty"]}});
}
bool passed=rows.All(r=>(bool)r["nativeExact"]&&(bool)r["diskExact"]&&(bool)r["dirtyExact"]);
return new {passed=passed,rows=rows,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,quality=UnityEngine.QualitySettings.GetQualityLevel(),aa=UnityEngine.QualitySettings.antiAliasing,playing=UnityEditor.EditorApplication.isPlaying,device=UnityEngine.SystemInfo.graphicsDeviceType.ToString()};
