using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using EchoStation.SalvageNext.Editor;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextLightmapBuildTests
    {
        [TestCase(false)][TestCase(true)]public void RequiredLightmapFlagsAreTemporaryAndRestoreNativeDiskAndDirtyStateOnFailure(bool fail)
        {
            string receipt="Docs/Tests/SalvagePrototype/Evidence/R4-R03b-4a/LightmapFlags_"+Guid.NewGuid().ToString("N")+".json";var graphics=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0];string json=EditorJsonUtility.ToJson(graphics);var bytes=File.ReadAllBytes("ProjectSettings/GraphicsSettings.asset");bool dirty=EditorUtility.IsDirty(graphics);
            try{using(new NextLightmapBuildScope(receipt)){var so=new SerializedObject(graphics);Assert.That(so.FindProperty("m_LightmapStripping").intValue,Is.EqualTo(1));Assert.That(so.FindProperty("m_LightmapKeepPlain").boolValue,Is.True);Assert.That(so.FindProperty("m_LightmapKeepDirCombined").boolValue,Is.True);foreach(var name in new[]{"m_LightmapKeepDynamicPlain","m_LightmapKeepDynamicDirCombined","m_LightmapKeepShadowMask","m_LightmapKeepSubtractive"})Assert.That(so.FindProperty(name).boolValue,Is.False);if(fail)throw new InvalidOperationException("intentional build failure");}}catch(InvalidOperationException e){if(!fail||e.Message!="intentional build failure")throw;}
            Assert.That(EditorJsonUtility.ToJson(graphics),Is.EqualTo(json));Assert.That(File.ReadAllBytes("ProjectSettings/GraphicsSettings.asset"),Is.EqualTo(bytes));Assert.That(EditorUtility.IsDirty(graphics),Is.EqualTo(dirty));var row=JObject.Parse(File.ReadAllText(receipt));Assert.That(row["nativeExactRestored"].Value<bool>()&&row["diskBytesUnchanged"].Value<bool>(),Is.True);
        }
    }
}
