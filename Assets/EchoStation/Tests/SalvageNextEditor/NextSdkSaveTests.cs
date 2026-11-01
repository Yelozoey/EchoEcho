using System;
using System.IO;
using System.Linq;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextSdkSaveTests
    {
        [Test]public void OfficialSdkSaveIsBoundedAndForeignUnsavedValuesAndDirtyFlagSurvive()
        {
            string id=Guid.NewGuid().ToString("N"),folder=FacilityAssets.Generated+"/SdkSaveProbe_"+id,foreignPath="Assets/EchoStation/SalvageNext_SaveProbe_"+id+".asset",ownedPath=folder+"/Owned.asset",evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R03a-2s";Directory.CreateDirectory(evidence);Assert.That(File.Exists(foreignPath),Is.False);FacilityAssets.Folder(folder);var foreign=ScriptableObject.CreateInstance<NextSaveProbe>();var owned=ScriptableObject.CreateInstance<NextSaveProbe>();foreign.Value=owned.Value=1;AssetDatabase.CreateAsset(foreign,foreignPath);AssetDatabase.CreateAsset(owned,ownedPath);AssetDatabase.SaveAssetIfDirty(foreign);AssetDatabase.SaveAssetIfDirty(owned);var before=File.ReadAllBytes(foreignPath);foreign.Value=2;owned.Value=2;EditorUtility.SetDirty(foreign);EditorUtility.SetDirty(owned);string receipt=evidence+"/Scope_"+id+".json";
            try
            {
                using(NextSdkSaveScope.Begin(receipt)){var settings=AddressableAssetSettings.Create(folder,"Settings",false,true);Assert.That(settings,Is.Not.Null);Assert.That(settings.IsPersisted,Is.True);Assert.Throws<InvalidOperationException>(()=>NextSdkSaveScope.Begin(evidence+"/UnexpectedNested_"+id+".json"));}
                CollectionAssert.AreEqual(before,File.ReadAllBytes(foreignPath));Assert.That(foreign.Value,Is.EqualTo(2));Assert.That(EditorUtility.IsDirty(foreign),Is.True);Assert.That(File.ReadAllText(ownedPath),Does.Contain("Value: 2"));var result=JObject.Parse(File.ReadAllText(receipt));Assert.That(result["passed"].Value<bool>(),Is.True);Assert.That(result["foreignDirtyFlagsPreserved"].Value<bool>(),Is.True);Assert.That(result["blocked"].Values<string>(),Does.Contain(foreignPath));Assert.That(result["saved"].Values<string>(),Does.Contain(ownedPath));
            }
            finally
            {
                // Only the two fresh GUID-scoped test assets/folder are removed.
                if(AssetDatabase.LoadMainAssetAtPath(foreignPath)==foreign)AssetDatabase.DeleteAsset(foreignPath);if(AssetDatabase.IsValidFolder(folder))AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
