using System;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using EchoStation.Salvage.Editor;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EchoStation.SalvageNext.Editor
{
    public static class NextLayout
    {
        public const string ScenePath=FacilityAssets.Root+"/Scenes/ES_Salvage_Next.unity",NavPath=FacilityAssets.Generated+"/ES_Next_Nav.asset";
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-A01e";
        public static Bounds BoundsOf(GameObject go)
        {var renderers=go.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);return bounds;}
        public static GameObject Spawn(string name,Transform parent,Vector3 center,Vector3 size,Quaternion rotation)
        {
            var path=FacilityAssets.Prefab(FacilityAssets.Models().Single(p=>Path.GetFileNameWithoutExtension(p)==name));var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),parent.gameObject.scene);go.transform.SetParent(parent,false);go.transform.rotation=rotation;
            var b=BoundsOf(go);var ratio=Quaternion.Inverse(rotation)*new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);ratio=new Vector3(Mathf.Abs(ratio.x),Mathf.Abs(ratio.y),Mathf.Abs(ratio.z));go.transform.localScale=Vector3.Scale(go.transform.localScale,ratio);b=BoundsOf(go);go.transform.position+=center-b.center;
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>()){GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.ReflectionProbeStatic);renderer.receiveGI=ReceiveGI.Lightmaps;renderer.scaleInLightmap=.5f;}
            return go;
        }
        static void RemoveGeometry(GameObject go)
        {
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>(true))Object.DestroyImmediate(renderer);
            foreach(var filter in go.GetComponentsInChildren<MeshFilter>(true))Object.DestroyImmediate(filter);
        }
        public static void AdaptWallFaces()
        {
            SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var materials=new System.Collections.Generic.Dictionary<Material,Material>();
                foreach(var room in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RoomVisual>()))foreach(Transform child in room.transform)
                {
                    if(!child.name.StartsWith("ES_WallBand_",StringComparison.Ordinal))continue;
                    foreach(var renderer in child.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(source=>
                    {
                        if(materials.TryGetValue(source,out var existing))return existing;string path=FacilityAssets.Generated+"/ES_Next_Wall_"+source.name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                        if(material&&!AssetDatabase.GetLabels(material).Contains("EchoStation-SalvageNext-Generated"))throw new InvalidOperationException("Foreign wall material.");
                        if(!material){material=Object.Instantiate(source);AssetDatabase.CreateAsset(material,path);}material.SetFloat("_Cull",0);material.doubleSidedGI=true;AssetDatabase.SetLabels(material,new[]{"EchoStation-SalvageNext-Generated"});EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);materials.Add(source,material);return material;
                    }).ToArray();
                }
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("Wall adaptation save failed.");
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
        static void Frame(Transform room,float x,float z)
        {
            var frame=Spawn("Door_Frame_Square",room,new Vector3(x,1.5f,z),new Vector3(.35f,3,3.5f),Quaternion.Euler(0,90,0));
            // Persist collision separately from the later streamed visual.
            var collision=new GameObject("DoorCollision");collision.transform.SetParent(room.parent,false);
            foreach(var filter in frame.GetComponentsInChildren<MeshFilter>()){var go=new GameObject("FrameSolid");go.transform.SetParent(collision.transform,false);go.transform.SetPositionAndRotation(filter.transform.position,filter.transform.rotation);go.transform.localScale=filter.transform.lossyScale;go.AddComponent<MeshCollider>().sharedMesh=filter.sharedMesh;}
        }
        [MenuItem("Tools/EchoStation/SalvageNext/R4-A01e/Create New Layout")]
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadMainAssetAtPath(ScenePath)||AssetDatabase.LoadMainAssetAtPath(NavPath))throw new InvalidOperationException("Preserve existing new layout; edit explicitly.");
            if(!AssetDatabase.CopyAsset(SalvageWorldSetup.ScenePath,ScenePath))throw new IOException("Cannot copy protected source scene.");var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var world=scene.GetRootGameObjects().Single(r=>r.name=="ES_Salvage_World");world.name="ES_SalvageNext_World";
                foreach(var instance in world.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)).ToArray())if(instance&&PrefabUtility.IsPartOfPrefabInstance(instance))PrefabUtility.UnpackPrefabInstance(instance.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                var floor=world.transform.Find("Floor_36x24");RemoveGeometry(floor.gameObject);
                var walls=world.transform.Cast<Transform>().Where(t=>new[]{"West","East","South","North","Partition"}.Contains(t.name)).ToArray();foreach(var wall in walls)RemoveGeometry(wall.gameObject);
                var rooms=new Transform[3];for(int i=0;i<3;i++){var room=new GameObject("RoomVisual_"+i);room.transform.SetParent(world.transform,false);room.AddComponent<RoomVisual>().Configure(i);rooms[i]=room.transform;
                    for(int x=0;x<3;x++)for(int z=0;z<6;z++){var center=new Vector3(i*12+2+x*4,0,2+z*4);Spawn((x+z)%3==0?"Platform_CenterPlate":"Platform_Metal2",room.transform,center,new Vector3(4,.0001f,4),Quaternion.identity);Spawn("Platform_Simple",room.transform,center+Vector3.up*3,new Vector3(4,.0001f,4),Quaternion.Euler(180,0,0));}
                    // North/south walls are split at the streaming zone boundary.
                    Spawn("WallBand_Straight",room.transform,new Vector3(i*12+6,1.5f,-.15f),new Vector3(12,3,.3f),Quaternion.Euler(0,90,0));Spawn("WallBand_Straight",room.transform,new Vector3(i*12+6,1.5f,24.15f),new Vector3(12,3,.3f),Quaternion.Euler(0,90,0));
                }
                foreach(var wall in walls.Where(t=>t.name!="South"&&t.name!="North")){int index=Mathf.Clamp(Mathf.FloorToInt((wall.position.x-.1f)/12),0,2);Spawn("WallBand_Straight",rooms[index],wall.position,wall.GetComponent<BoxCollider>().bounds.size,Quaternion.identity);}
                Frame(rooms[0],12,6);Frame(rooms[0],12,18);Frame(rooms[1],24,12);
                var ceiling=world.AddComponent<BoxCollider>();ceiling.center=new Vector3(18,3.1f,12);ceiling.size=new Vector3(36,.2f,24);
                for(int i=0;i<3;i++)
                {
                    var console=Spawn("Prop_Computer",rooms[i],new Vector3(i*12+.8f,.8f,2),new Vector3(.75f,1.6f,.57f),Quaternion.Euler(0,90,0));var c=new GameObject("ConsoleCollision_"+i);c.transform.SetParent(world.transform,false);c.AddComponent<BoxCollider>().center=BoundsOf(console).center;c.GetComponent<BoxCollider>().size=BoundsOf(console).size;
                    for(int z=0;z<3;z++)Spawn("Column_MetalSupport",rooms[i],new Vector3(i*12+6,2.85f,4+8*z),new Vector3(12,.08f,.5f),Quaternion.Euler(0,90,0));
                }
                foreach(var actor in world.GetComponentsInChildren<CargoActor>())CargoAssets.Attach(actor);
                var echo=world.GetComponentInChildren<SalvageEcho>();var appearance=echo.gameObject.AddComponent<RoverAppearance>();appearance.Configure(AssetDatabase.LoadAssetAtPath<GameObject>(RoverAssets.PrefabPath).GetComponent<RoverModelParts>());if(!appearance.Mount())throw new InvalidOperationException("Rover mount failed.");
                world.GetComponentInChildren<SalvagePersistence>().enabled=false;
                var surface=world.GetComponent<NavMeshSurface>();surface.RemoveData();surface.navMeshData=null;surface.BuildNavMesh();if(!surface.navMeshData)throw new InvalidOperationException("New continuous nav bake failed.");AssetDatabase.CreateAsset(surface.navMeshData,NavPath);AssetDatabase.SetLabels(surface.navMeshData,new[]{"EchoStation-SalvageNext-Generated"});AssetDatabase.SaveAssetIfDirty(surface.navMeshData);
                if(!EditorSceneManager.SaveScene(scene))throw new IOException("New layout save failed.");Directory.CreateDirectory(Evidence);File.WriteAllText(Evidence+"/Created.json",new Newtonsoft.Json.Linq.JObject{{"scene",ScenePath},{"zones",3},{"cargo",7},{"independentNav",NavPath},{"oldSaveDisabled",true}}.ToString());
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
