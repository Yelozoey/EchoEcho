using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.TestTools;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextAddressableSmokeTests
    {
        AsyncOperationHandle<TextAsset> first,second;bool ownFirst,ownSecond,background,preferencesCaptured;bool hadPath,hadLog;string priorPath,priorLog;
        [Test]public void OwnDefaultSettingsUseLocalPackedContentAndPreserveBuildSceneList()
        {
            var settings=NextAddressables.Settings;Assert.That(AddressableAssetSettingsDefaultObject.Settings,Is.SameAs(settings));Assert.That(AssetDatabase.GetAssetPath(settings),Is.EqualTo(NextAddressables.SettingsPath));Assert.That(settings.BuildRemoteCatalog,Is.False);Assert.That(settings.DisableCatalogUpdateOnStartup,Is.True);Assert.That(settings.ActivePlayerDataBuilder,Is.TypeOf<BuildScriptPackedMode>());Assert.That(settings.ActivePlayModeDataBuilder,Is.TypeOf<BuildScriptPackedPlayMode>());var group=settings.DefaultGroup;var schema=group.GetSchema<BundledAssetGroupSchema>();Assert.That(schema.UseUnityWebRequestForLocalBundles,Is.False);Assert.That(schema.BundleMode,Is.EqualTo(BundledAssetGroupSchema.BundlePackingMode.PackTogether));Assert.That(schema.LoadPath.GetValue(settings),Does.Not.Contain("http"));Assert.That(schema.BuildPath.GetValue(settings),Does.StartWith("Library/"));Assert.That(group.entries.Single().address,Is.EqualTo(NextAddressables.Address));var preparation=JObject.Parse(File.ReadAllText(NextAddressables.Evidence+"/Prepared_A.json"));Assert.That(preparation["originalBuildScenesUnchanged"].Value<bool>(),Is.True);Assert.That(File.Exists(Path.Combine(Addressables.BuildPath,"settings.json")),Is.True);Assert.That(Directory.GetFiles(Addressables.BuildPath,"*.bundle",SearchOption.AllDirectories).Length,Is.GreaterThan(0));
        }
        void CapturePreferences()
        {
            preferencesCaptured=true;hadPath=PlayerPrefs.HasKey(Addressables.kAddressablesRuntimeDataPath);priorPath=PlayerPrefs.GetString(Addressables.kAddressablesRuntimeDataPath);hadLog=PlayerPrefs.HasKey(Addressables.kAddressablesRuntimeBuildLogPath);priorLog=PlayerPrefs.GetString(Addressables.kAddressablesRuntimeBuildLogPath);
        }
        [UnityTest,Timeout(120000)]public IEnumerator RealBundleLoadsAsynchronouslySharesTwoHandlesAndReleasesLastReference()
        {
            CapturePreferences();yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;int before=AssetBundle.GetAllLoadedAssetBundles().Count();var timer=System.Diagnostics.Stopwatch.StartNew();first=Addressables.LoadAssetAsync<TextAsset>(NextAddressables.Address);ownFirst=true;while(!first.IsDone){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(10));yield return null;}double loadSeconds=timer.Elapsed.TotalSeconds;Assert.That(first.Status,Is.EqualTo(AsyncOperationStatus.Succeeded),first.OperationException?.ToString());Assert.That(first.Result.text,Is.EqualTo(NextAddressables.Payload));Assert.That(AssetDatabase.GetAssetPath(first.Result),Is.Empty,"Packed Bundle asset must not come from AssetDatabase FastMode");int loaded=AssetBundle.GetAllLoadedAssetBundles().Count();Assert.That(loaded,Is.GreaterThan(before));IResourceLocation location=null;foreach(var locator in Addressables.ResourceLocators)if(locator.Locate(NextAddressables.Address,typeof(TextAsset),out var locations)){location=locations[0];break;}Assert.That(location,Is.Not.Null);Assert.That(location.ProviderId,Does.Contain("BundledAssetProvider"));
            second=Addressables.LoadAssetAsync<TextAsset>(NextAddressables.Address);ownSecond=true;while(!second.IsDone)yield return null;Assert.That(second.Status,Is.EqualTo(AsyncOperationStatus.Succeeded));Assert.That(second.Result,Is.SameAs(first.Result));Addressables.Release(first);ownFirst=false;yield return null;Assert.That(second.IsValid(),Is.True);Assert.That(second.Result.text,Is.EqualTo(NextAddressables.Payload));Assert.That(AssetBundle.GetAllLoadedAssetBundles().Count(),Is.EqualTo(loaded));Addressables.Release(second);ownSecond=false;timer.Restart();while(AssetBundle.GetAllLoadedAssetBundles().Count()>before){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(5));yield return null;}var unused=Resources.UnloadUnusedAssets();while(!unused.isDone)yield return null;int after=AssetBundle.GetAllLoadedAssetBundles().Count();Assert.That(after,Is.EqualTo(before));File.WriteAllText(NextAddressables.Evidence+"/Runtime_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"loadSeconds",loadSeconds},{"provider",location.ProviderId},{"internalId",location.InternalId},{"bundlesBefore",before},{"bundlesLoaded",loaded},{"bundlesAfter",after},{"sharedInstance",true},{"firstReleaseKeptSecondAlive",true},{"source","PackedPlayMode real local bundle"}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(ownFirst&&first.IsValid()){Addressables.Release(first);ownFirst=false;}if(ownSecond&&second.IsValid()){Addressables.Release(second);ownSecond=false;}if(Application.isPlaying){Application.runInBackground=background;yield return new ExitPlayMode();}if(preferencesCaptured){if(hadPath)PlayerPrefs.SetString(Addressables.kAddressablesRuntimeDataPath,priorPath);else PlayerPrefs.DeleteKey(Addressables.kAddressablesRuntimeDataPath);if(hadLog)PlayerPrefs.SetString(Addressables.kAddressablesRuntimeBuildLogPath,priorLog);else PlayerPrefs.DeleteKey(Addressables.kAddressablesRuntimeBuildLogPath);preferencesCaptured=false;}
        }
    }
}
