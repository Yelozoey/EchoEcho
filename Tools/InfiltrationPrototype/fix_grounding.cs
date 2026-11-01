EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(EchoStation.InfiltrationArt.Editor.ArtSampleScene.ScenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var body=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UnityEngine.CharacterController>()).Single();
float old=body.minMoveDistance;body.minMoveDistance=0;
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("Owned scene save failed");
return new {oldMinimum=old,newMinimum=body.minMoveDistance,oldProjectsChanged=false};
}finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
