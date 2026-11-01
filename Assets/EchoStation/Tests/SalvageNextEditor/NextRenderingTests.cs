using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Rendering;
using EchoStation.Salvage;
using EchoStation.Salvage.Editor;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextRenderingTests
    {
        const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R01a";Scene old,scene;bool background;
        [Test]public void DedicatedThreeProfilesUseOrdinaryForwardHdrAndOwnedFeatures()
        {
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(NextRendering.RendererPath);Assert.That(renderer,Is.Not.Null);Assert.That(new SerializedObject(renderer).FindProperty("m_RenderingMode").intValue,Is.Zero);Assert.That(renderer.rendererFeatures.Count,Is.EqualTo(3));foreach(var feature in renderer.rendererFeatures)Assert.That(AssetDatabase.GetAssetPath(feature),Is.EqualTo(NextRendering.RendererPath));
            for(int i=0;i<3;i++){var profile=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(NextRendering.PipelinePath(i));Assert.That(profile.supportsHDR,Is.True);Assert.That(profile.msaaSampleCount,Is.EqualTo(new[]{1,2,4}[i]));Assert.That(profile.renderScale,Is.EqualTo(i==0?.8f:1));Assert.That(profile.shadowCascadeCount,Is.EqualTo(new[]{1,2,4}[i]));Assert.That(profile.shadowDistance,Is.EqualTo(new[]{20f,32,48}[i]));Assert.That(profile.mainLightShadowmapResolution,Is.EqualTo(new[]{1024,2048,4096}[i]));Assert.That(profile.useSRPBatcher,Is.True);Assert.That(profile.supportsMainLightShadows&&profile.supportsAdditionalLightShadows,Is.True);Assert.That(profile.additionalLightsRenderingMode,Is.EqualTo(LightRenderingMode.PerPixel));Assert.That(new SerializedObject(profile).FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue,Is.SameAs(renderer));}
        }
        [Test]public void SceneBindingCameraSdrAndCloseRestoreAllNativeBindings()
        {
            var beforeDefault=GraphicsSettings.defaultRenderPipeline;var beforeQuality=QualitySettings.renderPipeline;int beforeAA=QualitySettings.antiAliasing;old=SceneManager.GetActiveScene();scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try{var roots=scene.GetRootGameObjects();var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single();Assert.That(GraphicsSettings.currentRenderPipeline,Is.SameAs(AssetDatabase.LoadMainAssetAtPath(NextRendering.PipelinePath(1))));Assert.That(camera.allowHDR,Is.True);Assert.That(camera.GetUniversalAdditionalCameraData().allowHDROutput,Is.False);Assert.That(roots.SelectMany(r=>r.GetComponentsInChildren<DistanceShadowCaster>()).Count(),Is.EqualTo(11));}
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);scene=default;}
            Assert.That(GraphicsSettings.defaultRenderPipeline,Is.SameAs(beforeDefault));Assert.That(QualitySettings.renderPipeline,Is.SameAs(beforeQuality));Assert.That(QualitySettings.antiAliasing,Is.EqualTo(beforeAA));
        }
        IEnumerator Boot()
        {Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(NextLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;foreach(var focus in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<NextFocus>()))focus.enabled=false;foreach(var fog in scene.GetRootGameObjects().SelectMany(r=>r.GetComponents<NextFogField>()))fog.enabled=false;}
        [UnityTest]public IEnumerator QualitySelectionChangesActualAssetAndShadowBudgetsRestore()
        {
            yield return new EnterPlayMode();var priorDefault=GraphicsSettings.defaultRenderPipeline;var priorQuality=QualitySettings.renderPipeline;yield return Boot();var roots=scene.GetRootGameObjects();var session=roots.SelectMany(r=>r.GetComponentsInChildren<NextSession>()).Single();var scope=roots.SelectMany(r=>r.GetComponentsInChildren<NextRenderScope>()).Single();
            foreach(RecoveryQuality quality in Enum.GetValues(typeof(RecoveryQuality))){session.SetQuality(quality);Assert.That(GraphicsSettings.currentRenderPipeline,Is.SameAs(scope.Profile(quality)));Assert.That(QualitySettings.renderPipeline,Is.SameAs(scope.Profile(quality)));}
            scope.enabled=false;Assert.That(GraphicsSettings.defaultRenderPipeline,Is.SameAs(priorDefault));Assert.That(QualitySettings.renderPipeline,Is.SameAs(priorQuality));scope.enabled=true;Assert.That(GraphicsSettings.currentRenderPipeline,Is.SameAs(scope.Profile(session.Quality)));
            var actor=roots.SelectMany(r=>r.GetComponentsInChildren<CargoActor>()).First();var caster=actor.GetComponent<DistanceShadowCaster>();var renderers=actor.GetComponentInChildren<CargoVisualModel>().GetComponentsInChildren<MeshRenderer>();caster.Evaluate(actor.transform.position+Vector3.forward*40);Assert.That(renderers.All(r=>r.shadowCastingMode==ShadowCastingMode.Off),Is.True);caster.Evaluate(actor.transform.position);Assert.That(renderers.Any(r=>r.shadowCastingMode!=ShadowCastingMode.Off),Is.True);
            var budget=roots.SelectMany(r=>r.GetComponentsInChildren<LocalShadowBudget>()).Single();var torch=roots.SelectMany(r=>r.GetComponentsInChildren<Light>(true)).Single(l=>l.name=="Flashlight");var data=torch.GetUniversalAdditionalLightData();int original=data.additionalLightsShadowResolutionTier;budget.Evaluate(torch.transform.position+Vector3.forward*25);Assert.That(data.additionalLightsShadowResolutionTier,Is.Zero);budget.Evaluate(torch.transform.position+Vector3.forward*15);Assert.That(data.additionalLightsShadowResolutionTier,Is.EqualTo(1));budget.Evaluate(torch.transform.position);Assert.That(data.additionalLightsShadowResolutionTier,Is.EqualTo(2));budget.enabled=false;Assert.That(data.additionalLightsShadowResolutionTier,Is.EqualTo(original));
        }
        [UnityTest]public IEnumerator RealFlashlightProducesImageContributionInNewForwardPipeline()
        {
            yield return new EnterPlayMode();yield return Boot();var motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;motor.RestorePose(new Vector3(9,.1f,16.5f),0,30);var torch=motor.GetComponent<EchoStation.Player.PlayerFlashlight>();var camera=motor.GetComponentInChildren<Camera>();Directory.CreateDirectory(Evidence);var id=Guid.NewGuid().ToString("N");Color[] off,on;
            torch.SetOn(false);yield return null;using(var capture=new SalvageFrameCapture(camera))off=capture.Capture(Evidence+"/FlashOff_"+id+".png");torch.SetOn(true);yield return null;using(var capture=new SalvageFrameCapture(camera))on=capture.Capture(Evidence+"/FlashOn_"+id+".png");double difference=on.Zip(off,(a,b)=>(double)(a.r+a.g+a.b-b.r-b.g-b.b)/3).Average();Assert.That(difference,Is.GreaterThan(.001),"Actual realtime light contribution missing.");
            File.WriteAllText(Evidence+"/Flash_"+id+".json",new JObject{{"positiveMeanRgbDifference",difference},{"api",SystemInfo.graphicsDeviceType.ToString()},{"HDR",camera.allowHDR},{"displayHDR",camera.GetUniversalAdditionalCameraData().allowHDROutput},{"pipeline",GraphicsSettings.currentRenderPipeline.name}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
