EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(UnityEditor.EditorApplication.isPlaying||scene.path!=EchoStation.ReflectionSample.Editor.ReflectionSampleBuilder.ScenePath)throw new System.InvalidOperationException("Preserve changed user session");
var root=EchoStation.ReflectionSample.Editor.ReflectionSampleBuilder.World(scene);
int missing=root.GetComponentsInChildren<UnityEngine.Transform>(true).Sum(t=>UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
var p=root.GetComponentsInChildren<EchoStation.ReflectionSample.ReflectionPlanar>();var probe=root.GetComponentsInChildren<UnityEngine.ReflectionProbe>();
int cameras=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Camera>().Count(c=>c.name.StartsWith("RS_PlanarCamera_"));
int textures=UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.RenderTexture>().Count(t=>t.name.StartsWith("RS_PlanarHDR_"));
return new{passed=missing==0&&p.Length==1&&probe.Length==1&&probe[0].customBakedTexture&&probe[0].customBakedTexture.width==512&&cameras==0&&textures==0,missingScripts=missing,planes=p.Length,probeWidth=probe[0].customBakedTexture.width,probePath=UnityEditor.AssetDatabase.GetAssetPath(probe[0].customBakedTexture),sourceCamera=p[0].sourceCamera.name,temporaryCameras=cameras,temporaryTextures=textures,scene=scene.path,dirty=scene.isDirty,playing=UnityEditor.EditorApplication.isPlaying};