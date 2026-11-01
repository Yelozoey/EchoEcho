using System.Linq;
using UnityEditor;
using UnityEngine;
using EchoStation.World;
using EchoStation.Story;
namespace EchoStation.Station.Editor
{
 public static class StationPuzzleSetup
 {
  [MenuItem("Tools/EchoStation/Stage 2/F03/Connect Session Lifecycle")]
  public static void Lifecycle()=>StationWorldSetup.Edit((scene,root)=>{var player=scene.GetRootGameObjects().Single(r=>r.GetComponent<StationSession>());if(!player.GetComponent<StationLifecycle>())player.AddComponent<StationLifecycle>();});
  [MenuItem("Tools/EchoStation/Stage 2/F03/Connect Unified Persistence")]
  public static void Persistence()=>StationWorldSetup.Edit((scene,root)=>{var player=scene.GetRootGameObjects().Single(r=>r.GetComponent<StationSession>());if(!player.GetComponent<StationPersistence>())player.AddComponent<StationPersistence>();});
  [MenuItem("Tools/EchoStation/Stage 2/F02/Connect Ending Panel")]
  public static void Endings()=>StationWorldSetup.Edit((scene,root)=>{var player=scene.GetRootGameObjects().Single(r=>r.GetComponent<StationSession>());if(!player.GetComponent<StationEndingPanel>())player.AddComponent<StationEndingPanel>();});
  [MenuItem("Tools/EchoStation/Stage 2/P01/Connect Evidence Actions")]
  public static void Evidence()=>StationWorldSetup.Edit((scene,root)=>{var player=scene.GetRootGameObjects().Single(r=>r.GetComponent<StationSession>());if(!player.GetComponent<StationEvidencePanel>())player.AddComponent<StationEvidencePanel>();});
  [MenuItem("Tools/EchoStation/Stage 2/P02/Connect Terminals")]
  public static void Terminals()=>StationWorldSetup.Edit((scene,root)=>{var player=scene.GetRootGameObjects().Single(r=>r.GetComponent<StationSession>());if(!player.GetComponent<StationTerminalPanel>())player.AddComponent<StationTerminalPanel>();});
  [MenuItem("Tools/EchoStation/Stage 2/P03/Connect Power Panel")]
  public static void Power()=>StationWorldSetup.Edit((scene,root)=>{var player=scene.GetRootGameObjects().Single(r=>r.GetComponent<StationSession>());if(!player.GetComponent<StationPowerPanel>())player.AddComponent<StationPowerPanel>();});
  [MenuItem("Tools/EchoStation/Stage 2/P04/Connect Scan Credential And Arrow")]
  public static void Scans()=>StationWorldSetup.Edit((scene,root)=>{
   foreach(var source in StationClues.All){var definition=AssetDatabase.LoadAssetAtPath<ClueDefinition>(StationLayout.DataFolder+"/"+source.Id+".asset");if(definition){definition.Configure(source.Id,source.Title,source.FullText,source.Summary,(ClueKind)(int)source.Kind);EditorUtility.SetDirty(definition);}}
   var player=scene.GetRootGameObjects().Single(r=>r.GetComponent<StationSession>());if(!player.GetComponent<StationScanProgress>())player.AddComponent<StationScanProgress>();
   var target=root.GetComponentsInChildren<WorldTarget>().Single(t=>t.Id=="scan_mark_01");var path=StationLayout.DataFolder+"/ES_ScanArrow.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh{name="ES_ScanArrow"};mesh.vertices=new[]{new Vector3(-.12f,.5f,-.5f),new Vector3(.12f,.5f,-.5f),new Vector3(.12f,-.05f,-.5f),new Vector3(.4f,-.05f,-.5f),new Vector3(0,-.5f,-.5f),new Vector3(-.4f,-.05f,-.5f),new Vector3(-.12f,-.05f,-.5f)};mesh.triangles=new[]{0,1,2,0,2,6,5,3,4};mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);}target.GetComponent<MeshFilter>().sharedMesh=mesh;
   var material=StationWorldSetup.Material("ES_Marker_Glow",new Color(.1f,.5f,1));material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(.02f,.25f,.8f));EditorUtility.SetDirty(material);target.GetComponent<MeshRenderer>().sharedMaterial=material;
  });
 }
}
