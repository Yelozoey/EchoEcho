using System.Linq;
using EchoStation.Echo;
using EchoStation.Station;
using EchoStation.Story;
using EchoStation.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

namespace EchoStation.Station.Editor
{
    public static class StationRegionSetup
    {
        public static GameObject Device(string id,string title,StationDeviceKind kind,Vector3 position,Vector3 size,Transform parent,string fact,bool gate=false)
        {
            var go=StationWorldSetup.Box("Device_"+id,position,size,parent);go.SetActive(false);GameObjectUtility.SetStaticEditorFlags(go,0);
            go.AddComponent<WorldTarget>().Configure(id,title);go.AddComponent<StationDevice>().Configure(kind);var p=go.AddComponent<EchoTargetPermissions>();p.Configure(true,kind==StationDeviceKind.Power);p.SetScanFact(fact);
            var a=new GameObject("Approach_"+id).transform;a.SetParent(parent,false);a.position=new Vector3(position.x,position.z>10?-4:0,position.z-1.5f);p.SetApproachPoint(a);
            if(gate){var obstacle=go.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=Vector3.zero;obstacle.size=Vector3.one;obstacle.carving=true;obstacle.carveOnlyStationary=false;go.AddComponent<StationGate>();}
            go.SetActive(true);return go;
        }
        public static GameObject Clue(string id,Vector3 point,Transform parent,Vector3 approach,bool active=true)
        {
            StationClues.TryGet(id,out var c);var path=StationLayout.DataFolder+"/"+id+".asset";var data=AssetDatabase.LoadAssetAtPath<ClueDefinition>(path);
            if(!data){data=ScriptableObject.CreateInstance<ClueDefinition>();data.Configure(c.Id,c.Title,c.FullText,c.Summary,(ClueKind)(int)c.Kind);AssetDatabase.CreateAsset(data,path);}
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.SetActive(false);go.name="Clue_"+id;go.transform.SetParent(parent,false);go.transform.position=point;go.transform.localScale=new Vector3(.65f,.4f,.07f);
            var visual=go.GetComponent<MeshRenderer>();visual.sharedMaterial=StationWorldSetup.Material(c.Kind==EchoStation.Memory.MemoryClueKind.Scan?"ES_Scan_Blue":"ES_Record_Gray",c.Kind==EchoStation.Memory.MemoryClueKind.Scan?new Color(.1f,.5f,1):new Color(.64f,.69f,.7f));
            go.AddComponent<WorldTarget>().Configure(c.Id,c.Title);go.AddComponent<ClueSource>().Configure(data,visual);
            var p=go.AddComponent<EchoTargetPermissions>();p.Configure(true,false);p.SetScanFact(c.Summary);var a=new GameObject("Approach_"+id).transform;a.SetParent(parent,false);a.position=approach;p.SetApproachPoint(a);
            go.SetActive(active);return go;
        }
        [MenuItem("Tools/EchoStation/Stage 2/W02/Build Valley")]
        public static void Valley()=>StationWorldSetup.Edit((scene,root)=>{
            var region=root.transform.Find("Zone_valley");if(region.Find("Valley_West"))return;
            StationWorldSetup.Box("Valley_West",new Vector3(-8,2,-23.5f),new Vector3(.4f,4,33),region);
            StationWorldSetup.Box("Valley_East",new Vector3(8,2,-23.5f),new Vector3(.4f,4,33),region);
            StationWorldSetup.Box("Valley_South",new Vector3(0,2,-40),new Vector3(16,4,.4f),region);
            foreach(var x in new[]{-4.8f,4.8f})StationWorldSetup.Box("Valley_North_"+x,new Vector3(x,1.5f,-7.1f),new Vector3(6.4f,3,.2f),region);
            foreach(var p in new[]{new Vector3(-4,1.25f,-30),new Vector3(4,1.25f,-14)}){
                StationWorldSetup.Box("Record_Post_"+p.z,p+new Vector3(0,-.6f,.1f),new Vector3(.15f,1.3f,.15f),region);
                Clue(p.x<0?"note_entry_inspection_01":"note_evacuation_01",p,region,new Vector3(p.x,0,p.z-1.3f));
            }
        });
        [MenuItem("Tools/EchoStation/Stage 2/W03/Integrate Cabin Copy")]
        public static void Cabin()=>StationWorldSetup.Edit((scene,root)=>{
            var region=root.transform.Find("Zone_cabin");if(region.Find("ES_Cabin_S2_Copy"))return;
            var source=SceneManager.GetSceneByPath(EchoStation.World.Editor.CabinGreyboxSetup.ScenePath);var opened=!source.IsValid()||!source.isLoaded;if(opened)source=EditorSceneManager.OpenScene(EchoStation.World.Editor.CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try{
                var original=source.GetRootGameObjects().Single(r=>r.name=="ES_Cabin_Greybox");var copy=Object.Instantiate(original);copy.name="ES_Cabin_S2_Copy";SceneManager.MoveGameObjectToScene(copy,scene);copy.transform.SetParent(region,false);
                var surface=copy.GetComponent<NavMeshSurface>();if(surface){surface.RemoveData();Object.DestroyImmediate(surface);}
                foreach(var shadow in copy.GetComponentsInChildren<EchoStation.Rendering.DistanceShadowCaster>())Object.DestroyImmediate(shadow);
                foreach(var t in copy.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("ES_Combined_")).ToArray())Object.DestroyImmediate(t.gameObject);
                foreach(var id in new[]{"Cabin_Anchors","Cabin_InvestigationPoints"}){var child=copy.transform.Find(id);if(child)Object.DestroyImmediate(child.gameObject);}
                foreach(var target in copy.GetComponentsInChildren<WorldTarget>()){var permission=target.GetComponent<EchoTargetPermissions>();if(permission)Object.DestroyImmediate(permission);Object.DestroyImmediate(target);}
                foreach(var r in copy.GetComponentsInChildren<MeshRenderer>()){r.enabled=true;r.lightmapIndex=-1;}
                foreach(var name in new[]{"Main_WallNorth","Foyer_WallSouth"}){var part=copy.GetComponentsInChildren<Transform>().Single(t=>t.name==name);Object.DestroyImmediate(part.gameObject);}
                Object.DestroyImmediate(region.Find("Foundation_cabin").gameObject);
                foreach(var x in new[]{-3.3f,3.3f})StationWorldSetup.Box("Cabin_North_"+x,new Vector3(x,1.6f,4.1f),new Vector3(3.4f,3.2f,.2f),region);
                StationWorldSetup.Box("Cabin_North_Header",new Vector3(0,2.95f,4.1f),new Vector3(3.2f,.5f,.2f),region);
                Clue("note_maintenance_01",new Vector3(-2.8f,1.15f,2.2f),region,new Vector3(-2.8f,0,1.15f));
                Clue("note_roster_01",new Vector3(7.8f,1.2f,3.05f),region,new Vector3(7.7f,0,2.1f));
                var mark=Clue("scan_mark_01",new Vector3(8.97f,1.4f,2.8f),region,new Vector3(8.1f,0,2.1f));mark.transform.rotation=Quaternion.Euler(0,90,0);
                Device("cabin_terminal_01","小屋维护终端",StationDeviceKind.CabinTerminal,new Vector3(-2.1f,1.15f,2.45f),new Vector3(.7f,.5f,.35f),region,"维护终端需要现场人员输入并确认；扫描不提供锁定数据。");
                Clue("terminal_data_01",new Vector3(-2.1f,1.2f,2.2f),region,new Vector3(-2.1f,0,1.15f),false);
                Device("cabin_door_01","地下通路门",StationDeviceKind.CabinDoor,new Vector3(0,1.2f,5.2f),new Vector3(3.2f,3,.3f),root.transform,"通路门需要维护授权与现场人工确认。",true);
            }finally{SceneManager.SetActiveScene(scene);if(opened)EditorSceneManager.CloseScene(source,true);}
        });
        public static void Room(Transform region,string prefix,float width,float start,float end,bool northOpen=true,float opening=5)
        {
            var middle=(start+end)*.5f;var length=end-start;
            StationWorldSetup.Box(prefix+"_West",new Vector3(-width*.5f,-2,middle),new Vector3(.25f,4,length),region);
            StationWorldSetup.Box(prefix+"_East",new Vector3(width*.5f,-2,middle),new Vector3(.25f,4,length),region);
            StationWorldSetup.Box(prefix+"_Ceiling",new Vector3(0,.1f,middle),new Vector3(width,.2f,length),region);
            if(northOpen){var side=(width-opening)*.5f;foreach(var sign in new[]{-1,1})StationWorldSetup.Box(prefix+"_North_"+sign,new Vector3(sign*(opening*.5f+side*.5f),-2,end),new Vector3(side,4,.25f),region);}
            else StationWorldSetup.Box(prefix+"_North",new Vector3(0,-2,end),new Vector3(width,4,.25f),region);
        }
        [MenuItem("Tools/EchoStation/Stage 2/W04/Build Laboratory")]
        public static void Laboratory()=>StationWorldSetup.Edit((scene,root)=>{
            var region=root.transform.Find("Zone_lab_a");if(region.Find("Lab_West"))return;Room(region,"Lab",12,22,38);
            foreach(var sign in new[]{-1,1})StationWorldSetup.Box("Lab_South_"+sign,new Vector3(sign*3.8f,-2,22),new Vector3(4.4f,4,.25f),region);
            foreach(var x in new[]{-1.73f,1.73f}){var rail=StationWorldSetup.Box("Descent_Rail_"+x,new Vector3(x,-.9f,13),new Vector3(.25f,2.4f,Mathf.Sqrt(18*18+16)),region);rail.transform.rotation=Quaternion.Euler(Mathf.Atan2(4,18)*Mathf.Rad2Deg,0,0);}
            Clue("note_lab_circuit_01",new Vector3(-4.8f,-2.75f,25),region,new Vector3(-4.8f,-4,23.7f));
            Clue("data_lab_maintenance_01",new Vector3(-3.5f,-2.75f,34.5f),region,new Vector3(-3.5f,-4,33.2f));
            Clue("note_lab_sensor_01",new Vector3(2.5f,-2.75f,25),region,new Vector3(2.5f,-4,23.7f));
            StationWorldSetup.Box("Lab_CalibrationRack",new Vector3(4.4f,-3,33.5f),new Vector3(1.8f,2,.6f),region);
            Clue("scan_lab_calibration_01",new Vector3(4.4f,-2.6f,33.1f),region,new Vector3(4.4f,-4,31.7f));
            Device("power_panel_01","A区通风供电面板",StationDeviceKind.Power,new Vector3(-4.5f,-2.7f,28.8f),new Vector3(1.3f,.8f,.2f),region,"面板控制排风、内循环和实验支路，接线异常，必须由现场人员核对并确认。");
            var barrier=StationWorldSetup.Box("Vent_Isolation",new Vector3(0,-2,38.1f),new Vector3(5,4,.25f),root.transform);GameObjectUtility.SetStaticEditorFlags(barrier,0);var obstacle=barrier.AddComponent<NavMeshObstacle>();obstacle.carving=true;obstacle.carveOnlyStationary=false;obstacle.size=Vector3.one;barrier.AddComponent<StationGate>();
        });
        [MenuItem("Tools/EchoStation/Stage 2/W05/Build Ventilation Passage")]
        public static void Vent()=>StationWorldSetup.Edit((scene,root)=>{
            var region=root.transform.Find("Zone_vent");if(region.Find("Vent_West"))return;Room(region,"Vent",5,38,58,false);
            Object.DestroyImmediate(region.Find("Vent_North").gameObject);
            Clue("note_vent_route_01",new Vector3(1.65f,-2.75f,44),region,new Vector3(1.65f,-4,42.7f));
            Device("vent_terminal_01","通道维护终端",StationDeviceKind.VentTerminal,new Vector3(-1.5f,-2.75f,53),new Vector3(.9f,.7f,.3f),region,"维护终端需要人工提交校准凭证与维护码；扫描不提供锁定数据。");
            Clue("terminal_vent_data_01",new Vector3(-1.5f,-2.7f,52.7f),region,new Vector3(-1.5f,-4,51.3f),false);
            Device("core_door_01","核心通路门",StationDeviceKind.CoreDoor,new Vector3(0,-2,58),new Vector3(5,4,.3f),root.transform,"核心通路需要通道维护授权与人工开门确认。",true);
        });
        [MenuItem("Tools/EchoStation/Stage 2/W06/Build Core Room")]
        public static void Core()=>StationWorldSetup.Edit((scene,root)=>{
            var region=root.transform.Find("Zone_core");if(region.Find("Core_West"))return;Room(region,"Core",14,58,76,false);
            foreach(var sign in new[]{-1,1})StationWorldSetup.Box("Core_South_"+sign,new Vector3(sign*4.75f,-2,58),new Vector3(4.5f,4,.25f),region);
            Clue("data_core_sensor_01",new Vector3(-4.4f,-2.7f,68.5f),region,new Vector3(-4.4f,-4,67.2f));
            Clue("note_core_shutdown_01",new Vector3(4.4f,-2.7f,68.5f),region,new Vector3(4.4f,-4,67.2f));
            Device("core_terminal_01","核心处置终端",StationDeviceKind.CoreTerminal,new Vector3(0,-2.7f,72),new Vector3(2,.8f,.4f),region,"处置选项需要现场人员核对资料并明确确认；Echo不代替玩家选择。");
        });
    }
}
