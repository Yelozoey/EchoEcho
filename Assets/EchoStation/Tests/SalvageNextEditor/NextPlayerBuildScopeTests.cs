using System;
using System.IO;
using System.Linq;
using EchoStation.SalvageNext.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextPlayerBuildScopeTests
    {
        [Test]public void RestoreReloadsPipelineAfterNativeAssetUnload()
        {
            string folder="Docs/Tests/SalvagePrototype/Evidence/R4-P01b-1",id=Guid.NewGuid().ToString("N"),path=AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline);
            using(NextSdkSaveScope.Begin(folder+"/UnloadSave_"+id+".json"))using(new NextPlayerBuildScope(folder+"/UnloadRestore_"+id+".json")){EditorUtility.UnloadUnusedAssetsImmediate();}
            Assert.That(AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline),Is.EqualTo(path));Assert.That(GraphicsSettings.defaultRenderPipeline,Is.Not.Null);
        }
        [TestCase(false)][TestCase(true)]public void TemporaryNativePlayerAndThreePipelinesRestoreOnNormalAndException(bool fail)
        {
            string folder="Docs/Tests/SalvagePrototype/Evidence/R4-P01b-1",id=Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);
            try{using(NextSdkSaveScope.Begin(folder+"/Save_"+id+".json"))using(new NextPlayerBuildScope(folder+"/Restore_"+id+".json"))
            {Assert.That(PlayerSettings.defaultScreenWidth,Is.EqualTo(1920));Assert.That(PlayerSettings.defaultScreenHeight,Is.EqualTo(1080));Assert.That(PlayerSettings.fullScreenMode,Is.EqualTo(FullScreenMode.Windowed));Assert.That(PlayerSettings.resizableWindow,Is.True);Assert.That(PlayerSettings.useHDRDisplay||PlayerSettings.allowHDRDisplaySupport,Is.False);Assert.That(PlayerSettings.enableFrameTimingStats,Is.True);Assert.That(PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64),Is.EqualTo(new[]{GraphicsDeviceType.Direct3D11}));var q=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset").Single()).FindProperty("m_QualitySettings");Assert.That(q.arraySize,Is.EqualTo(3));for(int i=0;i<3;i++)Assert.That(AssetDatabase.GetAssetPath(q.GetArrayElementAtIndex(i).FindPropertyRelative("customRenderPipeline").objectReferenceValue),Is.EqualTo(NextRendering.PipelinePath(i)));if(fail)throw new ApplicationException("intentional-scope-test");}}
            catch(ApplicationException e){Assert.That(fail&&e.Message=="intentional-scope-test",Is.True);}
        }
    }
}
