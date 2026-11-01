using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextRoomBundleTests
    {
        readonly AsyncOperationHandle<SceneInstance>[] loads=new AsyncOperationHandle<SceneInstance>[3],unloads=new AsyncOperationHandle<SceneInstance>[3];readonly bool[] ownLoad=new bool[3],ownUnload=new bool[3];bool background;
        [Test]public void RoomsAreSeparateLocalScenesWithExplicitThirtySixSharedAssets()
        {
            var settings=NextAddressables.Settings;var group=settings.groups.Single(g=>g&&g.Name==NextRoomBundles.RoomGroup);var common=settings.groups.Single(g=>g&&g.Name==NextRoomBundles.SharedGroup);Assert.That(group.entries.Count,Is.EqualTo(3));Assert.That(group.GetSchema<BundledAssetGroupSchema>().BundleMode,Is.EqualTo(BundledAssetGroupSchema.BundlePackingMode.PackSeparately));for(int i=0;i<3;i++)Assert.That(group.entries.Single(e=>e.address==NextRoomBundles.Address(i)).AssetPath,Is.EqualTo(NextRoomPartition.RoomPath(i)));var expected=JObject.Parse(File.ReadAllText(NextRoomBundles.Evidence+"/Prepared_A.json"));Assert.That(common.entries.Count,Is.EqualTo(expected["sharedCount"].Value<int>()));Assert.That(common.entries.Count,Is.EqualTo(36));foreach(var item in new[]{group,common}){var schema=item.GetSchema<BundledAssetGroupSchema>();Assert.That(schema.LoadPath.GetValue(settings),Does.Not.Contain("http"));Assert.That(schema.UseUnityWebRequestForLocalBundles,Is.False);}Assert.That(settings.BuildRemoteCatalog,Is.False);
        }
        IEnumerator Load(int index)
        {
            loads[index]=Addressables.LoadSceneAsync(NextRoomBundles.Address(index),LoadSceneMode.Additive,true);ownLoad[index]=true;var timer=System.Diagnostics.Stopwatch.StartNew();while(!loads[index].IsDone){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(10));yield return null;}Assert.That(loads[index].Status,Is.EqualTo(AsyncOperationStatus.Succeeded),loads[index].OperationException?.ToString());Assert.That(loads[index].Result.Scene.isLoaded,Is.True);
        }
        IEnumerator Unload(int index)
        {
            // SDK 2.11.2 SceneProvider consumes the original load handle on completion.
            unloads[index]=Addressables.UnloadSceneAsync(loads[index],UnloadSceneOptions.UnloadAllEmbeddedSceneObjects,false);ownLoad[index]=false;ownUnload[index]=true;var timer=System.Diagnostics.Stopwatch.StartNew();while(!unloads[index].IsDone){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(5));yield return null;}Assert.That(unloads[index].Status,Is.EqualTo(AsyncOperationStatus.Succeeded));Addressables.Release(unloads[index]);ownUnload[index]=false;yield return null;
        }
        static RoomPackage Package(Scene scene)=>scene.GetRootGameObjects().Single().GetComponent<RoomPackage>();
        [UnityTest,Timeout(180000)]public IEnumerator RealRoomSceneBundlesReleaseUniqueMeshMapsAndKeepSharedMaterialUntilLastRoom()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;int before=AssetBundle.GetAllLoadedAssetBundles().Count();var timer=System.Diagnostics.Stopwatch.StartNew();yield return Load(0);double firstSeconds=timer.Elapsed.TotalSeconds;var first=Package(loads[0].Result.Scene);Assert.That(first.Index,Is.Zero);Assert.That(first.gameObject.activeSelf,Is.False);var uniqueMesh=first.Renderer(0).GetComponent<MeshFilter>().sharedMesh;var uniqueMap=first.ColorMap(0);var shared=first.Renderer(0).sharedMaterial;Assert.That(AssetDatabase.GetAssetPath(uniqueMesh),Is.Empty);Assert.That(AssetDatabase.GetAssetPath(uniqueMap),Is.Empty);int firstBundles=AssetBundle.GetAllLoadedAssetBundles().Count();timer.Restart();yield return Load(1);double secondSeconds=timer.Elapsed.TotalSeconds;var second=Package(loads[1].Result.Scene);Assert.That(second.Index,Is.EqualTo(1));Assert.That(second.Renderer(0).sharedMaterial,Is.SameAs(shared));Assert.That(second.ColorMap(0),Is.Not.SameAs(uniqueMap));int bothBundles=AssetBundle.GetAllLoadedAssetBundles().Count();var secondMap=second.ColorMap(0);yield return Unload(0);var unused=Resources.UnloadUnusedAssets();while(!unused.isDone)yield return null;bool uniqueMeshGone=uniqueMesh==null,uniqueMapGone=uniqueMap==null;Assert.That(uniqueMeshGone,Is.True);Assert.That(uniqueMapGone,Is.True);Assert.That(shared!=null&&secondMap!=null,Is.True);int afterFirst=AssetBundle.GetAllLoadedAssetBundles().Count();Assert.That(afterFirst,Is.LessThan(bothBundles));Assert.That(afterFirst,Is.GreaterThan(before));timer.Restart();yield return Load(2);double thirdSeconds=timer.Elapsed.TotalSeconds;var third=Package(loads[2].Result.Scene);Assert.That(third.Index,Is.EqualTo(2));Assert.That(third.gameObject.activeSelf,Is.False);Assert.That(third.Renderer(0).sharedMaterial,Is.SameAs(shared));yield return Unload(2);Assert.That(shared!=null,Is.True);yield return Unload(1);unused=Resources.UnloadUnusedAssets();while(!unused.isDone)yield return null;timer.Restart();while(AssetBundle.GetAllLoadedAssetBundles().Count()>before){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(5));yield return null;}Assert.That(shared==null&&secondMap==null,Is.True);int after=AssetBundle.GetAllLoadedAssetBundles().Count();Assert.That(after,Is.EqualTo(before));File.WriteAllText(NextRoomBundles.Evidence+"/RoomRuntime_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"firstSceneLoadSeconds",firstSeconds},{"secondSceneLoadSeconds",secondSeconds},{"thirdSceneLoadSeconds",thirdSeconds},{"bundlesBefore",before},{"firstBundles",firstBundles},{"bothBundles",bothBundles},{"afterFirstUnload",afterFirst},{"bundlesAfter",after},{"uniqueMeshReleased",uniqueMeshGone},{"uniqueLightmapReleased",uniqueMapGone},{"sharedMaterialRetainedUntilLastRoom",true},{"rootsInitiallyInactive",true}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;for(int i=0;i<3;i++){if(ownUnload[i]&&unloads[i].IsValid()){while(!unloads[i].IsDone)yield return null;Addressables.Release(unloads[i]);ownUnload[i]=false;}if(ownLoad[i]&&loads[i].IsValid()){while(!loads[i].IsDone)yield return null;if(loads[i].Status==AsyncOperationStatus.Succeeded)yield return Unload(i);else{Addressables.Release(loads[i]);ownLoad[i]=false;}}}Application.runInBackground=background;yield return new ExitPlayMode();
        }
    }
}
