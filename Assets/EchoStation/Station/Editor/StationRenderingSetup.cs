using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Player;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using EchoStation.World;
using EchoStation.World.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace EchoStation.Station.Editor
{
 public static class StationRenderingSetup
 {
  public const string LightingPath=StationLayout.DataFolder+"/ES_Station_Lighting.asset";
  [MenuItem("Tools/EchoStation/Stage 2/W06/Configure Static Rendering")]
  public static void Configure()=>StationWorldSetup.Edit((scene,root)=>{
   if(root.transform.Find("Station_BakeSetup"))return;
   CabinGreyboxSetup.EnsureFolder(StationLayout.DataFolder+"/Meshes");
   var rooms=new List<Transform>();foreach(var id in new[]{"valley","lab_a","vent","core"})rooms.Add(root.transform.Find("Zone_"+id));
   var cabin=root.transform.Find("Zone_cabin");var copy=cabin.Find("ES_Cabin_S2_Copy");foreach(Transform child in copy)if(child.name.StartsWith("Cabin_Room_"))rooms.Add(child);
   var connectors=new GameObject("Cabin_Connectors").transform;connectors.SetParent(cabin,false);foreach(var t in cabin.Cast<Transform>().Where(t=>t.name.StartsWith("Cabin_North_")).ToArray())t.SetParent(connectors,true);rooms.Add(connectors);
   root.transform.Find("Descent_Ramp").SetParent(root.transform.Find("Zone_lab_a"),true);
   foreach(var room in rooms){var sources=room.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&GameObjectUtility.AreStaticEditorFlagsSet(r.gameObject,StaticEditorFlags.ContributeGI)&&!r.GetComponent<ClueSource>()&&!r.GetComponent<StationDevice>()).ToArray();
    foreach(var r in sources){r.lightmapIndex=-1;r.receiveGI=ReceiveGI.Lightmaps;r.lightProbeUsage=LightProbeUsage.Off;r.reflectionProbeUsage=ReflectionProbeUsage.Off;r.probeAnchor=null;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
    var index=0;foreach(var group in sources.GroupBy(r=>r.sharedMaterial)){
     var batch=group.ToArray();MeshRenderer result;Mesh mesh;
     if(batch.Length>1){result=StaticRoomMeshCombiner.Combine(room,batch);mesh=result.GetComponent<MeshFilter>().sharedMesh;result.name="S2_Combined_"+room.name+"_"+index;}
     else{result=batch[0];mesh=Object.Instantiate(result.GetComponent<MeshFilter>().sharedMesh);}
     UnwrapParam.SetDefaults(out var settings);settings.packMargin=4f/1024;
     if(!Unwrapping.GenerateSecondaryUVSet(mesh,settings)||!LightmapUvValidator.IsValidNonOverlapping(mesh,out var error))throw new InvalidOperationException("S2 UV2 generation failed: "+room.name);
     var path=StationLayout.DataFolder+"/Meshes/"+room.name+"_"+(index++)+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
     if(existing){EditorUtility.CopySerialized(mesh,existing);result.GetComponent<MeshFilter>().sharedMesh=existing;Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);}else{AssetDatabase.CreateAsset(mesh,path);result.GetComponent<MeshFilter>().sharedMesh=mesh;}
    }
   }
   foreach(var clue in root.GetComponentsInChildren<ClueSource>(true)){var r=clue.GetComponent<MeshRenderer>();if(r){r.shadowCastingMode=ShadowCastingMode.Off;r.lightProbeUsage=LightProbeUsage.BlendProbes;}}
   if(!AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath)&&!AssetDatabase.CopyAsset(BakedLightingSetup.SettingsPath,LightingPath))throw new InvalidOperationException("Cannot create S2 lighting settings.");
   var lighting=AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath);lighting.name="ES_Station_Lighting";lighting.lightmapResolution=12;lighting.indirectSampleCount=128;lighting.mixedBakeMode=MixedLightingMode.Shadowmask;EditorUtility.SetDirty(lighting);Lightmapping.lightingSettings=lighting;
   var sun=scene.GetRootGameObjects().Single(r=>r.name=="Station_DayFill").GetComponent<Light>();sun.lightmapBakeType=LightmapBakeType.Mixed;sun.intensity=.7f;
   var setup=new GameObject("Station_BakeSetup").transform;setup.SetParent(root.transform,false);
   foreach(var region in new[]{("lab_a",new[]{25f,34f}), ("vent",new[]{42f,50f,56f}), ("core",new[]{62f,69f,74f})})foreach(var z in region.Item2){var go=new GameObject("Baked_Cool_"+z,typeof(Light));go.transform.SetParent(setup,false);go.transform.position=new Vector3(0,-.8f,z);var l=go.GetComponent<Light>();l.type=LightType.Point;l.lightmapBakeType=LightmapBakeType.Baked;l.color=new Color(.7f,.82f,1);l.intensity=8;l.range=9;l.shadows=LightShadows.Soft;}
   foreach(var old in root.GetComponentsInChildren<LightProbeGroup>())Object.DestroyImmediate(old.gameObject);
   var probe=new GameObject("Station_LightProbes",typeof(LightProbeGroup)).GetComponent<LightProbeGroup>();probe.transform.SetParent(setup,false);var positions=new List<Vector3>();
   for(var z=-37f;z<75;z+=3){var y=z<4?0:z<22?-(z-4)*4/18:-4;var width=z<-7?5:z<4?1:z<22?.8f:z<38?4:z<58?1.4f:5;foreach(var x in new[]{-width,0,width})foreach(var h in new[]{.4f,1.1f,2.2f})positions.Add(new Vector3(x,y+h,z));}
   foreach(var x in new[]{6f,7.5f,8.5f})foreach(var z in new[]{-1f,1f,2.6f})foreach(var y in new[]{.4f,1.1f,2.3f})positions.Add(new Vector3(x,y,z));
   var solids=root.GetComponentsInChildren<Collider>().Where(c=>!c.isTrigger&&GameObjectUtility.AreStaticEditorFlagsSet(c.gameObject,StaticEditorFlags.ContributeGI)).ToArray();probe.probePositions=positions.Where(p=>!solids.Any(c=>c.bounds.Contains(p))).ToArray();
   var fault=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RealtimeLightTemplates.Folder+"/ES_FaultLight.prefab"),scene);fault.name="Station_LabFault";fault.transform.position=new Vector3(-4.7f,-1.3f,28.6f);
   var area=new GameObject("Station_OcclusionArea",typeof(OcclusionArea)).GetComponent<OcclusionArea>();area.transform.SetParent(setup,false);area.center=new Vector3(0,-.5f,18);area.size=new Vector3(18,9,120);
   var camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<FirstPersonMotor>()).Single().View.GetComponent<Camera>();camera.useOcclusionCulling=true;root.GetComponent<DistanceShadowCaster>().Configure(camera,root.GetComponentsInChildren<MeshRenderer>(true).Cast<Renderer>().ToArray());
  });
 }
}
