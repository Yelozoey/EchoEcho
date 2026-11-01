EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
if(UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()!=null)throw new System.InvalidOperationException("Preserve open prefab");
const string evidence="Docs/Tests/InfiltrationPrototype/Evidence/A01R";
var path=evidence+"/NativeBefore_A.json";
if(System.IO.File.Exists(path))throw new System.InvalidOperationException("Preserve native snapshot");
var rows=new Newtonsoft.Json.Linq.JObject();
foreach(var setting in new[]{"ProjectSettings/GraphicsSettings.asset","ProjectSettings/QualitySettings.asset","ProjectSettings/EditorSettings.asset"}){var obj=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(setting).Single();rows[setting]=new Newtonsoft.Json.Linq.JObject{{"json",UnityEditor.EditorJsonUtility.ToJson(obj)},{"dirty",UnityEditor.EditorUtility.IsDirty(obj)},{"disk",System.Convert.ToBase64String(System.IO.File.ReadAllBytes(setting))}};}
System.IO.File.WriteAllText(path,rows.ToString());
return new {scenes=UnityEditor.SceneManagement.EditorSceneManager.GetSceneManagerSetup().Select(s=>new {path=s.path,loaded=s.isLoaded,active=s.isActive,dirty=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(s.path).isDirty}).ToArray(),graphics=UnityEditor.AssetDatabase.GetAssetPath(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline),quality=UnityEditor.AssetDatabase.GetAssetPath(UnityEngine.QualitySettings.renderPipeline),aa=UnityEngine.QualitySettings.antiAliasing,device=UnityEngine.SystemInfo.graphicsDeviceType.ToString()};
