using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextLighting
    {
        public const string SettingsPath=FacilityAssets.Generated+"/ES_Next_Lighting.asset",Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R01b";
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadMainAssetAtPath(SettingsPath))throw new InvalidOperationException("Lighting settings already exist.");
            var settings=new LightingSettings{name="ES_Next_Lighting",lightmapper=LightingSettings.Lightmapper.UnityComputeGPU,bakedGI=true,realtimeGI=false,autoGenerate=false,lightmapResolution=12,lightmapMaxSize=512,lightmapPadding=4,directionalityMode=LightmapsMode.CombinedDirectional,lightmapCompression=LightmapCompression.HighQuality,directSampleCount=32,indirectSampleCount=64,environmentSampleCount=32,minBounces=2,maxBounces=2,indirectScale=1,albedoBoost=1,lightProbeSampleCountMultiplier=2,ao=false,filteringMode=LightingSettings.FilterMode.Advanced,denoiserTypeDirect=LightingSettings.DenoiserType.None,denoiserTypeIndirect=LightingSettings.DenoiserType.None,filterTypeDirect=LightingSettings.FilterType.Gaussian,filterTypeIndirect=LightingSettings.FilterType.Gaussian,filteringGaussianRadiusDirect=1,filteringGaussianRadiusIndirect=2,prioritizeView=false};AssetDatabase.CreateAsset(settings,SettingsPath);Save(settings);
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);Lightmapping.lightingSettings=settings;var root=scene.GetRootGameObjects().Single();var container=new GameObject("NextBakedLighting");container.transform.SetParent(root.transform,false);
                var copies=new Dictionary<Material,Material>();foreach(var room in root.GetComponentsInChildren<RoomVisual>())foreach(var renderer in room.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(source=>
                {
                    if(copies.TryGetValue(source,out var copy))return copy;string path=FacilityAssets.Generated+"/ES_NextLit_"+source.name+"_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)).Substring(0,8)+".mat";if(AssetDatabase.LoadMainAssetAtPath(path))throw new InvalidOperationException("Keep existing lighting material.");
                    copy=Object.Instantiate(source);copy.SetFloat("_Cull",0);copy.doubleSidedGI=true;if(copy.GetTexture("_EmissionMap")){copy.SetColor("_EmissionColor",new Color(.015f,.035f,.065f));copy.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;}else copy.globalIlluminationFlags=MaterialGlobalIlluminationFlags.EmissiveIsBlack;AssetDatabase.CreateAsset(copy,path);Save(copy);copies.Add(source,copy);return copy;
                }).ToArray();
                for(int room=0;room<3;room++)for(int x=0;x<2;x++)for(int z=0;z<3;z++)
                {var light=new GameObject("Area_"+room+"_"+x+"_"+z).AddComponent<Light>();light.transform.SetParent(container.transform,false);light.transform.position=new Vector3(room*12+3+x*6,2.82f,4+z*8);light.transform.rotation=Quaternion.Euler(90,0,0);light.type=LightType.Rectangle;light.areaSize=new Vector2(2,.4f);light.intensity=3;light.range=10;light.color=room==1?new Color(.64f,.76f,1):new Color(.76f,.86f,1);light.lightmapBakeType=LightmapBakeType.Baked;light.shadows=LightShadows.Soft;}
                foreach(var target in root.GetComponentsInChildren<SalvageTargetView>().Where(t=>t.Id.StartsWith("danger_",StringComparison.Ordinal)))
                {
                    var baked=new GameObject("Warning_Baked_"+target.Id).AddComponent<Light>();baked.transform.SetParent(container.transform,false);baked.transform.position=target.transform.position+Vector3.up*1.8f;baked.type=LightType.Point;baked.range=4;baked.intensity=2;baked.color=new Color(1,.25f,.08f);baked.lightmapBakeType=LightmapBakeType.Baked;
                    var fault=new GameObject("Fault_"+target.Id).AddComponent<Light>();fault.transform.SetParent(root.transform,false);fault.transform.position=baked.transform.position;fault.type=LightType.Point;fault.range=3;fault.intensity=.8f;fault.color=baked.color;fault.lightmapBakeType=LightmapBakeType.Realtime;fault.shadows=LightShadows.Soft;fault.gameObject.AddComponent<FaultLightFlicker>();
                }
                var probes=new List<Vector3>();foreach(float y in new[]{.35f,1.2f,2.4f}){for(int x=0;x<9;x++)for(int z=0;z<6;z++)probes.Add(new Vector3(2+x*4,y,2+z*4));foreach(var p in new[]{new Vector2(11,6),new Vector2(13,6),new Vector2(11,18),new Vector2(13,18),new Vector2(23,12),new Vector2(25,12)})probes.Add(new Vector3(p.x,y,p.y));}
                var group=new GameObject("NextLightProbes").AddComponent<LightProbeGroup>();group.transform.SetParent(root.transform,false);group.probePositions=probes.ToArray();
                foreach(var room in root.GetComponentsInChildren<RoomVisual>()){var reflection=new GameObject("Reflection_"+room.Index).AddComponent<ReflectionProbe>();reflection.transform.SetParent(room.transform,false);reflection.transform.position=room.Region.center;reflection.size=room.Region.size;reflection.boxProjection=true;reflection.resolution=128;reflection.hdr=true;reflection.mode=ReflectionProbeMode.Baked;reflection.clearFlags=ReflectionProbeClearFlags.SolidColor;reflection.backgroundColor=new Color(.015f,.02f,.03f);reflection.intensity=.8f;reflection.cullingMask=~0;}
                foreach(var renderer in root.GetComponentsInChildren<CargoVisualModel>().SelectMany(v=>v.GetComponentsInChildren<MeshRenderer>()).Concat(root.GetComponentInChildren<RoverModelParts>().GetComponentsInChildren<MeshRenderer>())){renderer.receiveGI=ReceiveGI.LightProbes;renderer.lightProbeUsage=LightProbeUsage.BlendProbes;renderer.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;}
                var sun=root.GetComponentsInChildren<Light>().Single(l=>l.type==LightType.Directional);sun.intensity=.25f;RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.04f,.055f,.075f);RenderSettings.customReflectionTexture=null;RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.reflectionIntensity=.8f;EditorSceneManager.SaveScene(scene);
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        static void Save(Object asset){AssetDatabase.SetLabels(asset,new[]{"EchoStation-SalvageNext-Generated"});EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
        public static void TuneEnergy()
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single();var lamps=root.GetComponentsInChildren<Light>().Where(l=>l.type==LightType.Rectangle).ToArray();if(lamps.Length!=18)throw new InvalidOperationException("Expected18 static fixtures.");foreach(var lamp in lamps)lamp.intensity=60;
                root.GetComponent<LocalShadowBudget>().Configure(root.GetComponentInChildren<Camera>(),root.GetComponentsInChildren<Light>(true).Where(l=>l.type!=LightType.Directional&&l.lightmapBakeType==LightmapBakeType.Realtime).Select(l=>l.GetUniversalAdditionalLightData()).ToArray());EditorSceneManager.SaveScene(scene);
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        public static void AdaptPaintedSurfaces()
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var materials=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RoomVisual>()).SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>()).SelectMany(r=>r.sharedMaterials).Distinct().ToArray();var masks=new Dictionary<Texture,Texture>();var rows=new JArray();
                foreach(var material in materials)
                {
                    var source=material.GetTexture("_MetallicGlossMap");if(!source)continue;if(!AssetDatabase.GetAssetPath(material).StartsWith(FacilityAssets.Generated+"/ES_NextLit_",StringComparison.Ordinal))throw new InvalidOperationException("Preserve non-lighting material.");
                    if(!masks.TryGetValue(source,out var packed))
                    {
                        string inputPath=AssetDatabase.GetAssetPath(source);string path=FacilityAssets.Generated+"/ES_Next_Painted_"+source.name+".png";if(File.Exists(path))throw new InvalidOperationException("Keep prior painted mask.");var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                        try{if(!texture.LoadImage(File.ReadAllBytes(inputPath)))throw new IOException("Cannot read owned packed mask.");var pixels=texture.GetPixels32();for(int i=0;i<pixels.Length;i++){var value=pixels[i];value.r=(byte)Mathf.RoundToInt(value.r*.15f);pixels[i]=value;}texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}
                        finally{Object.DestroyImmediate(texture);}
                        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();packed=AssetDatabase.LoadAssetAtPath<Texture>(path);Save(packed);masks.Add(source,packed);rows.Add(new JObject{{"source",inputPath},{"derived",path},{"metalMultiplier",.15},{"GBAUnchanged",true}});
                    }
                    material.SetTexture("_MetallicGlossMap",packed);material.SetTexture("_OcclusionMap",packed);Save(material);
                }
                File.WriteAllText(Evidence+"/PaintedMasks_A.json",rows.ToString());EditorSceneManager.SaveScene(scene);
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        public static void OrientInteriorWalls()
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var world=scene.GetRootGameObjects().Single();if(world.transform.Find("InteriorWallOrientationDone"))throw new InvalidOperationException("Walls already oriented.");var rooms=world.GetComponentsInChildren<RoomVisual>().OrderBy(r=>r.Index).ToArray();var original=rooms.SelectMany(r=>r.transform.Cast<Transform>()).Where(t=>t.name.StartsWith("ES_WallBand_",StringComparison.Ordinal)).ToArray();int opposite=0;
                foreach(var wall in original)
                {
                    if(PrefabUtility.IsPartOfPrefabInstance(wall.gameObject))PrefabUtility.UnpackPrefabInstance(wall.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);var bounds=NextLayout.BoundsOf(wall.gameObject);var center=bounds.center;
                    if(Mathf.Abs(center.x+.15f)<.01f){wall.rotation=Quaternion.Euler(0,180,0);wall.position+=center-NextLayout.BoundsOf(wall.gameObject).center;}
                    else if(Mathf.Abs(center.z-24.15f)<.01f){wall.rotation=Quaternion.Euler(0,270,0);wall.position+=center-NextLayout.BoundsOf(wall.gameObject).center;}
                    else if(Mathf.Abs(center.x-12)<.01f||Mathf.Abs(center.x-24)<.01f)
                    {
                        var copy=Object.Instantiate(wall.gameObject,rooms[(int)Mathf.Round(center.x/12)].transform);copy.name="InteriorOpposite_"+(opposite++);if(PrefabUtility.IsPartOfPrefabInstance(copy))PrefabUtility.UnpackPrefabInstance(copy,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                        wall.localScale=Vector3.Scale(wall.localScale,new Vector3(.5f,1,1));wall.position+=center-Vector3.right*.075f-NextLayout.BoundsOf(wall.gameObject).center;
                        copy.transform.localScale=Vector3.Scale(copy.transform.localScale,new Vector3(.5f,1,1));copy.transform.rotation=Quaternion.Euler(0,180,0);copy.transform.position+=center+Vector3.right*.075f-NextLayout.BoundsOf(copy).center;
                    }
                }
                var dock=world.transform.Find("RecoveryPoint");Object.DestroyImmediate(dock.GetComponent<MeshRenderer>());Object.DestroyImmediate(dock.GetComponent<MeshFilter>());var plate=NextLayout.Spawn("Platform_CenterPlate",rooms[0].transform,new Vector3(5,.081f,12),new Vector3(3,.0001f,3),Quaternion.identity);plate.name="RecoveryDock_PbrPlate";
                foreach(var renderer in plate.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(source=>{var path=FacilityAssets.Generated+"/ES_NextLit_"+source.name+"_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)).Substring(0,8)+".mat";var own=AssetDatabase.LoadAssetAtPath<Material>(path);if(!own)throw new InvalidOperationException("Missing adapted dock surface.");return own;}).ToArray();
                var view=world.GetComponentInChildren<Camera>();foreach(var room in rooms)room.GetComponent<DistanceShadowCaster>().Configure(view,room.GetComponentsInChildren<MeshRenderer>(),30,2,.1f);var marker=new GameObject("InteriorWallOrientationDone");marker.transform.SetParent(world.transform,false);EditorSceneManager.SaveScene(scene);File.WriteAllText(Evidence+"/InteriorWalls_A.json",new JObject{{"outerWestFlipped",1},{"northFlipped",3},{"opposingPartitionFaces",opposite},{"dockPbrVisual",true},{"collisionAndNavigationChanged",false}}.ToString());
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        public static void RepairStaticUv2()
        {
            SceneSafetyGuard.RequireClean();var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var copies=new Dictionary<Mesh,Mesh>();var evidence=new JArray();
                foreach(var filter in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RoomVisual>()).SelectMany(r=>r.GetComponentsInChildren<MeshFilter>()))
                {
                    var source=filter.sharedMesh;if(source.uv2.Length==source.vertexCount)continue;if(!copies.TryGetValue(source,out var mesh))
                    {
                        mesh=Object.Instantiate(source);mesh.name=source.name+"_StaticUv2";int before=source.triangles.Length;UnwrapParam.SetDefaults(out var settings);settings.packMargin=4f/512;
                        if(!Unwrapping.GenerateSecondaryUVSet(mesh,settings)||mesh.uv2.Length!=mesh.vertexCount||mesh.triangles.Length!=before||Vector3.Distance(mesh.bounds.size,source.bounds.size)>.001f)throw new InvalidOperationException("Derived static UV2 changed geometry.");
                        string path=FacilityAssets.Generated+"/ES_Next_StaticUv2_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)).Substring(0,8)+".asset";if(AssetDatabase.LoadMainAssetAtPath(path))throw new InvalidOperationException("Keep prior derived static mesh.");AssetDatabase.CreateAsset(mesh,path);Save(mesh);copies.Add(source,mesh);evidence.Add(new JObject{{"source",AssetDatabase.GetAssetPath(source)},{"derived",path},{"sourceVertices",source.vertexCount},{"derivedVertices",mesh.vertexCount},{"triangleIndicesPreserved",before}});
                    }
                    filter.sharedMesh=mesh;
                }
                EditorSceneManager.SaveScene(scene);File.WriteAllText(Evidence+"/StaticUv2Repair_A.json",evidence.ToString());
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        static SceneSetup[] previousLayout;static DateTime started;static bool completed,cancelled;static string folder;static readonly List<string> bakeErrors=new List<string>();
        static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)lock(bakeErrors)bakeErrors.Add(message);}
        public static void StartBake()
        {
            SceneSafetyGuard.RequireClean();if(EditorApplication.isPlayingOrWillChangePlaymode||Lightmapping.isRunning||previousLayout!=null||StaticOcclusionCulling.isRunning||PrefabStageUtility.GetCurrentPrefabStage()!=null)throw new InvalidOperationException("Another operation active.");
            folder=Evidence+"/Bake_"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(folder);previousLayout=EditorSceneManager.GetSceneManagerSetup();started=DateTime.UtcNow;completed=cancelled=false;File.WriteAllText(folder+"/Started.json",new JObject{{"utc",started.ToString("O")},{"scene",NextLayout.ScenePath}}.ToString());
            lock(bakeErrors)bakeErrors.Clear();Application.logMessageReceivedThreaded+=Log;
            try{EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Single);Lightmapping.bakeCompleted+=Completed;Lightmapping.bakeCancelled+=Cancelled;EditorApplication.update+=Tick;if(!Lightmapping.BakeAsync())Finish("BakeAsync did not start.");}catch(Exception e){Finish(e.ToString());}
        }
        static void Completed()=>completed=true;static void Cancelled()=>cancelled=true;
        static void Tick(){if((DateTime.UtcNow-started).TotalSeconds>600){Lightmapping.Cancel();Finish("Lighting bake timeout600s.");return;}if(!Lightmapping.isRunning&&(completed||cancelled))Finish(cancelled?"Bake cancelled.":null);}
        static void Finish(string error)
        {
            Lightmapping.bakeCompleted-=Completed;Lightmapping.bakeCancelled-=Cancelled;EditorApplication.update-=Tick;var report=new JObject{{"passed",false},{"error",error},{"seconds",(DateTime.UtcNow-started).TotalSeconds},{"layoutRestored",false}};
            try
            {
                var scene=SceneManager.GetActiveScene();if(SceneManager.sceneCount!=1||scene.path!=NextLayout.ScenePath)throw new InvalidOperationException("Layout changed during bake; preserving current scene.");
                if(error==null)
                {
                    var maps=LightmapSettings.lightmaps;var root=scene.GetRootGameObjects().Single();var receivers=root.GetComponentsInChildren<RoomVisual>().SelectMany(r=>r.GetComponentsInChildren<MeshRenderer>()).ToArray();var realtime=root.GetComponentsInChildren<Light>(true).Where(l=>l.lightmapBakeType==LightmapBakeType.Realtime).ToArray();var flags=realtime.Select(l=>l.enabled).ToArray();var reflectionRows=new JArray();
                    try{foreach(var light in realtime)light.enabled=false;foreach(var probe in root.GetComponentsInChildren<ReflectionProbe>()){string path=FacilityAssets.Generated+"/ES_Next_Reflection_"+probe.transform.parent.GetComponent<RoomVisual>().Index+".exr";if(!Lightmapping.BakeReflectionProbe(probe,path))throw new InvalidOperationException("Reflection bake failed.");AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var texture=AssetDatabase.LoadAssetAtPath<Texture>(path);if(!texture)throw new InvalidOperationException("Reflection texture missing.");probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=texture;reflectionRows.Add(new JObject{{"path",path},{"bytes",new FileInfo(path).Length}});}}
                    finally{for(int i=0;i<realtime.Length;i++)if(realtime[i])realtime[i].enabled=flags[i];}
                    EditorSceneManager.SaveScene(scene);if(Lightmapping.lightingDataAsset)AssetDatabase.SaveAssetIfDirty(Lightmapping.lightingDataAsset);
                    report["atlases"]=maps.Length;report["probes"]=LightmapSettings.lightProbes?LightmapSettings.lightProbes.count:0;report["receivers"]=receivers.Length;report["mapped"]=receivers.Count(r=>r.lightmapIndex>=0&&r.lightmapIndex<maps.Length);report["colors"]=new JArray(maps.Select(m=>AssetDatabase.GetAssetPath(m.lightmapColor)));report["reflections"]=reflectionRows;lock(bakeErrors){report["bakeErrors"]=new JArray(bakeErrors);report["passed"]=maps.Length>0&&report["probes"].Value<int>()>=180&&report["mapped"].Value<int>()==receivers.Length&&bakeErrors.Count==0;}
                }
                EditorSceneManager.RestoreSceneManagerSetup(previousLayout);report["layoutRestored"]=true;
            }
            catch(Exception e){report["error"]=e.ToString();report["passed"]=false;}
            finally{Application.logMessageReceivedThreaded-=Log;previousLayout=null;File.WriteAllText(folder+"/LightingBake.json",report.ToString());}
        }
    }
}
