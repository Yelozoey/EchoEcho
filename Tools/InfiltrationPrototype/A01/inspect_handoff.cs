EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!=EchoStation.InfiltrationLevel.Editor.LevelBuilder.ScenePath||UnityEditor.EditorApplication.isPlaying)throw new System.InvalidOperationException("Preserve changed handoff session");
var root=EchoStation.InfiltrationLevel.Editor.LevelBuilder.World(scene);
int missing=root.GetComponentsInChildren<UnityEngine.Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
var renderer=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>(EchoStation.InfiltrationLevel.Editor.LevelLighting.RendererPath);
var posts=root.GetComponentsInChildren<EchoStation.InfiltrationLevel.LevelPost>();
return new {passed=missing==0&&posts.Length==4&&renderer.rendererFeatures.All(f=>f&&f.isActive),missingScripts=missing,models=root.GetComponentsInChildren<EchoStation.InfiltrationArt.ArtSampleModel>().Length,guards=posts.Select(p=>new{id=p.Id,patrolPreview=p.FuturePatrol}).ToArray(),features=renderer.rendererFeatures.Select(f=>new{name=f.name,active=f.isActive}).ToArray(),scene=scene.path,dirty=scene.isDirty,playing=UnityEditor.EditorApplication.isPlaying,lightmaps=UnityEngine.LightmapSettings.lightmaps.Length,netArea=EchoStation.InfiltrationLevel.LevelLayout.ConservativeNetArea(root)};
