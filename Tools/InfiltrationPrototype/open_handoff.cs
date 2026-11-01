EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
if(UnityEditor.EditorApplication.isPlaying||UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Preserve active play session");
if(UnityEngine.SceneManagement.SceneManager.sceneCount!=1||UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/EchoStation/SalvageExpansion/Scenes/ES_EnvironmentalSalvage.unity")throw new System.InvalidOperationException("Preserve unexpected scene layout");
var baseline=Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText("Docs/Tests/InfiltrationPrototype/Evidence/A00/NativeBefore_A.json"));
foreach(var entry in baseline.Properties())
{
    var obj=UnityEditor.AssetDatabase.LoadAllAssetsAtPath(entry.Name).Single();
    if(UnityEditor.EditorJsonUtility.ToJson(obj)!=(string)entry.Value["json"]||!System.IO.File.ReadAllBytes(entry.Name).SequenceEqual(System.Convert.FromBase64String((string)entry.Value["disk"]))||UnityEditor.EditorUtility.IsDirty(obj)!=(bool)entry.Value["dirty"])throw new System.InvalidOperationException("Restore original settings before handoff");
}
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(EchoStation.InfiltrationArt.Editor.ArtSampleScene.ScenePath,UnityEditor.SceneManagement.OpenSceneMode.Single);
UnityEditor.Selection.objects=new UnityEngine.Object[0];
var type=typeof(UnityEditor.EditorWindow).Assembly.GetType("UnityEditor.GameView");UnityEditor.EditorWindow.GetWindow(type).Show();UnityEditor.EditorWindow.GetWindow(type).Focus();
return new {scene=scene.path,dirty=scene.isDirty,playing=UnityEditor.EditorApplication.isPlaying,aiConnected=false,instructions="Play, click Game, WASD/mouse/Space/Esc",defaultPipeline=UnityEditor.AssetDatabase.GetAssetPath(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline),qualityPipeline=UnityEditor.AssetDatabase.GetAssetPath(UnityEngine.QualitySettings.renderPipeline)};
