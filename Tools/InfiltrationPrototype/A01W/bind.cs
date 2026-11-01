EchoStation.Rendering.Editor.SceneSafetyGuard.RequireClean();
if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/EchoStation/Tests/Scenes/S1_P09_RegressionHost.unity")throw new System.InvalidOperationException("Preserve unexpected workspace");
var pipeline=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/EchoStation/Rendering/Settings/ES_Gameplay_ForwardHDR.asset");
if(!pipeline)throw new System.InvalidOperationException("Missing validated pipeline");
UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline=pipeline;UnityEngine.QualitySettings.renderPipeline=pipeline;
return new {bound=true};
