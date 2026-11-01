EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
var paths=UnityEditor.AssetDatabase.GetDependencies(EchoStation.InfiltrationCoated.Editor.CoatScene.ScenePath,true).OrderBy(p=>p).ToArray();
var row=new Newtonsoft.Json.Linq.JObject{{"scene",EchoStation.InfiltrationCoated.Editor.CoatScene.ScenePath},{"dependencies",new Newtonsoft.Json.Linq.JArray(paths)},{"scope","Current main scene recursive AssetDatabase dependencies; registry package paths may be resolved by UPM"}};
System.IO.File.WriteAllText("Docs/Repository/CoatedSceneDependencies.json",row.ToString());
return new {dependencies=paths.Length};
