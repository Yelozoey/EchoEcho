using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EchoStation.Rendering;
using EchoStation.Salvage;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextRoomPartitionTests
    {
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static GameObject World(Scene scene)=>scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");
        [Test]public void MainPreservesPhysicsCargoAndIndependentNavWithoutVisualRoomOrWholeGiReferences()
        {
            var expected=JObject.Parse(File.ReadAllText(NextRoomPartition.Evidence+"/Prepared_A.json"));var hashes=JObject.Parse(File.ReadAllText(NextRoomPartition.Evidence+"/ReferenceBefore_A.json"));foreach(var property in hashes.Properties())Assert.That(Hash(property.Name),Is.EqualTo(property.Value.Value<string>()));string text=File.ReadAllText(NextRoomPartition.Main);Assert.That(text,Does.Contain("m_LightingDataAsset: {fileID: 0}"));Assert.That(text,Does.Contain("m_OcclusionCullingData: {fileID: 36300000, guid: "+AssetDatabase.AssetPathToGUID(NextStreamOcclusion.DataPath)));var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextRoomPartition.Main,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);var root=World(scene);Assert.That(root.GetComponentsInChildren<Collider>(true).Length,Is.EqualTo(expected["persistentColliders"].Value<int>()));Assert.That(root.GetComponentsInChildren<RoomVisual>(true),Is.Empty);Assert.That(root.GetComponentsInChildren<LightProbeGroup>(true),Is.Empty);Assert.That(root.GetComponent<NextProbeScope>().Probes,Is.SameAs(AssetDatabase.LoadAssetAtPath<LightProbes>(NextProbeAssets.Path)));Assert.That(AssetDatabase.GetAssetPath(root.GetComponent<NavMeshSurface>().navMeshData),Is.EqualTo(NextRoomPartition.Nav));var actors=root.GetComponentsInChildren<CargoActor>();Assert.That(actors.Length,Is.EqualTo(7));foreach(var actor in actors)Assert.That(actor.GetComponentInChildren<CargoVisualModel>(),Is.Not.Null);Assert.That(root.GetComponentsInChildren<Camera>().Length,Is.EqualTo(1));var dependencies=AssetDatabase.GetDependencies(NextRoomPartition.Main,true);Assert.That(dependencies.Any(p=>p.StartsWith(NextRoomPartition.Maps+"/",StringComparison.Ordinal)),Is.False);Assert.That(dependencies.Any(p=>p.Contains("/ES_Next_Room")&&p.EndsWith(".asset",StringComparison.Ordinal)),Is.False);Assert.That(dependencies.Any(p=>p.Contains("LightingData.asset")||p.Contains("OcclusionCullingData.asset")&&p!=NextStreamOcclusion.DataPath),Is.False);Assert.That(dependencies,Does.Contain(NextStreamOcclusion.DataPath));}
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        [Test]public void ThreeInactiveVisualScenesHaveNoPhysicsOrForeignSceneReferencesAndAllThirtySevenMeshes()
        {
            var previous=SceneManager.GetActiveScene();int total=0;var paths=new HashSet<string>();for(int index=0;index<3;index++)
            {
                var scene=EditorSceneManager.OpenScene(NextRoomPartition.RoomPath(index),OpenSceneMode.Additive);try{var root=scene.GetRootGameObjects().Single();Assert.That(root.activeSelf,Is.False);var data=root.GetComponent<RoomPackage>();Assert.That(data.Index,Is.EqualTo(index));Assert.That(data.RendererCount,Is.EqualTo(index==0?13:12));total+=data.RendererCount;Assert.That(data.MapCount,Is.EqualTo(2));Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(root.GetComponentsInChildren<NavMeshSurface>(true),Is.Empty);Assert.That(root.GetComponentsInChildren<Camera>(true),Is.Empty);Assert.That(root.GetComponentsInChildren<Light>(true),Is.Empty);Assert.That(root.GetComponent<DistanceShadowCaster>().TargetCamera,Is.Null);var decor=new SerializedObject(root.GetComponent<InstancedRoomDecor>());Assert.That(decor.FindProperty("view").objectReferenceValue,Is.Null);
                    for(int atlas=0;atlas<2;atlas++){Assert.That(paths.Add(AssetDatabase.GetAssetPath(data.ColorMap(atlas))),Is.True);Assert.That(paths.Add(AssetDatabase.GetAssetPath(data.DirectionMap(atlas))),Is.True);}for(int i=0;i<data.RendererCount;i++){Assert.That(data.Renderer(i).gameObject.scene,Is.EqualTo(scene));Assert.That(data.Atlas(i),Is.InRange(0,1));Assert.That(data.Renderer(i).GetComponent<MergedRoomMesh>().Atlas,Is.EqualTo(data.Atlas(i)));}
                    foreach(var component in root.GetComponentsInChildren<Component>(true)){Assert.That(component,Is.Not.Null);var serialized=new SerializedObject(component);var property=serialized.GetIterator();while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference){var reference=property.objectReferenceValue;var gameObject=reference is Component attached?attached.gameObject:reference as GameObject;if(gameObject&&gameObject.scene.IsValid())Assert.That(gameObject.scene,Is.EqualTo(scene),component.name+"/"+property.propertyPath);}}
                }finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
            }Assert.That(total,Is.EqualTo(37));Assert.That(paths.Count,Is.EqualTo(12));
        }
        [Test]public void RoomAtlasCopiesKeepActualReferenceExrBytesAndImportType()
        {
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(scene);var maps=LightmapSettings.lightmaps;for(int room=0;room<3;room++)for(int atlas=0;atlas<2;atlas++)foreach(string channel in new[]{"Color","Direction"}){var source=channel=="Color"?maps[atlas].lightmapColor:maps[atlas].lightmapDir;string sourcePath=AssetDatabase.GetAssetPath(source);string copy=NextRoomPartition.Maps+"/Room"+room+"_Atlas"+atlas+"_"+channel+Path.GetExtension(sourcePath);Assert.That(Hash(copy),Is.EqualTo(Hash(sourcePath)));var a=(TextureImporter)AssetImporter.GetAtPath(sourcePath);var b=(TextureImporter)AssetImporter.GetAtPath(copy);Assert.That(b.textureType,Is.EqualTo(a.textureType));Assert.That(b.sRGBTexture,Is.EqualTo(a.sRGBTexture));Assert.That(b.textureCompression,Is.EqualTo(a.textureCompression));}}
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        Scene old,runtime;bool background;RoomSceneOwner owner;
        IEnumerator AwaitCleanup(){if(owner==null)yield break;owner.Dispose();var timer=System.Diagnostics.Stopwatch.StartNew();while(!owner.IsClosed||Resources.FindObjectsOfTypeAll<RoomScenePump>().Length>0){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(15),"Probe fixture must wait for its real streaming services before ExitPlayMode");yield return null;}}
        [UnityTest]public IEnumerator MainOwnsIndependentRuntimeProbesAndRestoresThemOnExit()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();var prior=LightmapSettings.lightProbes;runtime=EditorSceneManager.LoadSceneInPlayMode(NextRoomPartition.Main,new LoadSceneParameters(LoadSceneMode.Additive));while(!runtime.isLoaded)yield return null;owner=World(runtime).GetComponentInChildren<NextRoomStreaming>().Owner;yield return null;var scope=World(runtime).GetComponent<NextProbeScope>();Assert.That(LightmapSettings.lightProbes,Is.SameAs(scope.Probes));Assert.That(LightmapSettings.lightProbes.count,Is.EqualTo(180));var expected=JObject.Parse(File.ReadAllText(NextProbeAssets.Evidence+"/Prepared_A.json"));float maximum=0;for(int i=0;i<NextProbeAssets.Samples.Length;i++){LightProbes.GetInterpolatedProbe(NextProbeAssets.Samples[i],null,out var sh);for(int c=0;c<3;c++)for(int k=0;k<9;k++)maximum=Mathf.Max(maximum,Mathf.Abs(sh[c,k]-expected["sampleCoefficients"][i][c*9+k].Value<float>()));}Assert.That(maximum,Is.LessThan(.00001f));var unload=SceneManager.UnloadSceneAsync(runtime);while(!unload.isDone)yield return null;runtime=default;yield return AwaitCleanup();Assert.That(LightmapSettings.lightProbes,Is.SameAs(prior));File.WriteAllText(NextRoomPartition.Evidence+"/ProbeRuntime_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"maximumCoefficientDifference",maximum},{"count",180},{"restored",true}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(runtime.IsValid()){var op=SceneManager.UnloadSceneAsync(runtime);while(op!=null&&!op.isDone)yield return null;}yield return AwaitCleanup();Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
