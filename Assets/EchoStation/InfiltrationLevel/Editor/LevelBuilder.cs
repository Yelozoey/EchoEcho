using System;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt;
using EchoStation.InfiltrationArt.Editor;
using EchoStation.Player;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using Newtonsoft.Json.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EchoStation.InfiltrationLevel.Editor
{
    public static class LevelBuilder
    {
        public const string Root="Assets/EchoStation/InfiltrationLevel",Generated=Root+"/Generated",ScenePath=Root+"/Scenes/ES_Infiltration_Routes.unity";
        public const string Evidence="Docs/Tests/InfiltrationPrototype/Evidence/A01";
        public const string Pipeline="Assets/EchoStation/Rendering/Settings/ES_Gameplay_ForwardHDR.asset";
        static Transform parent;
        public static void Save(Object value){if(!AssetDatabase.GetAssetPath(value).StartsWith(Root+"/",StringComparison.Ordinal))throw new InvalidOperationException("Save only A01 asset");EditorUtility.SetDirty(value);AssetDatabase.SaveAssetIfDirty(value);}
        public static Material MaterialFor(Material original)
        {
            string path=Generated+"/IL_"+original.name.Replace(' ','_')+".mat";var value=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!value){value=Object.Instantiate(original);value.name="IL_"+original.name;if(value.HasProperty("_Cull"))value.SetFloat("_Cull",2);AssetDatabase.CreateAsset(value,path);Save(value);}return value;
        }
        public static GameObject Spawn(string role,string name,Vector3 point,Vector3 scale,float yaw=0,bool collide=true,bool isStatic=true)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(ArtSampleAssets.PathFor(role));if(!source)throw new InvalidOperationException("Missing source "+role);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source,parent.gameObject.scene);go.name=name;go.transform.SetParent(parent,false);go.transform.position=point;go.transform.rotation=Quaternion.Euler(0,yaw,0);go.transform.localScale=scale;
            foreach(var r in go.GetComponentsInChildren<Renderer>()){r.sharedMaterials=r.sharedMaterials.Select(MaterialFor).ToArray();r.lightProbeUsage=LightProbeUsage.BlendProbes;r.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;}
            if(isStatic)foreach(var t in go.GetComponentsInChildren<Transform>())GameObjectUtility.SetStaticEditorFlags(t.gameObject,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic);
            if(collide)
            {
                go.AddComponent<LevelSolid>();
                if(role=="DoorFrame")foreach(var m in go.GetComponentsInChildren<MeshFilter>())m.gameObject.AddComponent<MeshCollider>().sharedMesh=m.sharedMesh;
                else{var b=ArtSampleAssets.LocalBoundsOf(go);var box=go.AddComponent<BoxCollider>();box.center=b.center;box.size=new Vector3(Mathf.Max(.06f,b.size.x),Mathf.Max(.12f,b.size.y),Mathf.Max(.06f,b.size.z));}
            }
            return go;
        }
        static void Wall(string name,Vector3 point,bool vertical,float height=4.5f)
        {
            float yaw=vertical?0:90;Spawn("Wall",name,point,new Vector3(1,height/3,1),yaw);
            Spawn("Wall",name+"_Reverse",point,new Vector3(1,height/3,1),yaw+180,false);
        }
        static void Portal(string name,Vector3 point,bool vertical)
        {
            Spawn("DoorFrame",name,point,new Vector3(.82f,.8f,1),vertical?90:0);
            Spawn("WallCap",name+"_Header",point+Vector3.up*4,new Vector3(1,.51f,1),vertical?0:90,false);
            Spawn("WallCap",name+"_HeaderBack",point+Vector3.up*4,new Vector3(1,.51f,1),vertical?180:270,false);
        }
        public static Transform World(Scene scene)=>scene.GetRootGameObjects().Single(g=>g.name=="IL_World").transform;
        public static void Edit(Action<Transform> edit)
        {
            SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);parent=World(scene);edit(parent);if(!EditorSceneManager.SaveScene(scene))throw new IOException("Owned level save failed");}
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);parent=null;}
        }
        public static void BuildNav(Transform root,string version)
        {
            var player=root.GetComponentInChildren<SalvageMotor>();bool active=player.gameObject.activeSelf;player.gameObject.SetActive(false);
            var surface=root.GetComponent<NavMeshSurface>();surface.RemoveData();surface.navMeshData=null;
            surface.collectObjects=CollectObjects.Volume;surface.center=new Vector3(0,1.1f,0);surface.size=new Vector3(36,2.8f,32);surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.overrideVoxelSize=true;surface.voxelSize=.1f;
            try{surface.BuildNavMesh();if(!surface.navMeshData)throw new InvalidOperationException("Actual navigation output missing");string path=Generated+"/IL_Nav_"+version+".asset";if(File.Exists(path))throw new InvalidOperationException("Preserve nav version");AssetDatabase.CreateAsset(surface.navMeshData,path);Save(surface.navMeshData);}
            finally{player.gameObject.SetActive(active);}
        }
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();if(File.Exists(ScenePath))throw new InvalidOperationException("Preserve existing level");
            ArtSampleAssets.Folder(Generated);ArtSampleAssets.Folder(Root+"/Scenes");Directory.CreateDirectory(Evidence);
            var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);parent=new GameObject("IL_World").transform;parent.gameObject.AddComponent<NavMeshSurface>();
                for(int z=0;z<8;z++)for(int x=0;x<9;x++)
                {
                    int zone=LevelLayout.Zone(x,z);var point=LevelLayout.Cell(x,z);var floor=Spawn("Floor","Floor_"+x+"_"+z,point,Vector3.one,collide:false);floor.AddComponent<LevelFloor>().Zone=zone;
                    var c=floor.AddComponent<BoxCollider>();c.center=Vector3.down*.08f;c.size=new Vector3(4,.16f,4);
                    float height=zone==1?6:4.5f;
                    if(!(x==4&&z==3)){var roof=Spawn("Floor","Ceiling_"+x+"_"+z,point+Vector3.up*height,Vector3.one,collide:false);roof.transform.rotation=Quaternion.Euler(180,0,0);var roofBox=roof.AddComponent<BoxCollider>();roofBox.center=Vector3.down*.06f;roofBox.size=new Vector3(4,.12f,4);}
                }
                for(int z=0;z<8;z++){Wall("West_"+z,new Vector3(-18,0,-14+4*z),true);Wall("East_"+z,new Vector3(18,0,-14+4*z),true);}
                for(int x=0;x<9;x++){Wall("South_"+x,new Vector3(-16+4*x,0,-16),false);Wall("North_"+x,new Vector3(-16+4*x,0,16),false);}
                for(int z=0;z<8;z++)for(int x=1;x<9;x++)if(LevelLayout.Zone(x-1,z)!=LevelLayout.Zone(x,z))
                {var p=new Vector3(-18+4*x,0,-14+4*z);if(LevelLayout.Opening(true,x,z))Portal("SidePortal_"+x+"_"+z,p,true);else Wall("PartitionX_"+x+"_"+z,p,true);}
                for(int z=1;z<8;z++)for(int x=0;x<9;x++)if(LevelLayout.Zone(x,z-1)!=LevelLayout.Zone(x,z))
                {var p=new Vector3(-16+4*x,0,-16+4*z);if(LevelLayout.Opening(false,z,x))Portal("CrossPortal_"+x+"_"+z,p,false);else Wall("PartitionZ_"+x+"_"+z,p,false);}
                var player=new GameObject("IL_Player");player.SetActive(false);player.transform.SetParent(parent,false);player.transform.position=LevelLayout.Start;
                var body=player.AddComponent<CharacterController>();body.height=1.75f;body.radius=.3f;body.center=Vector3.up*.875f;body.stepOffset=.25f;body.skinWidth=.03f;body.minMoveDistance=0;
                var camera=new GameObject("IL_Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.SetParent(player.transform,false);camera.transform.localPosition=Vector3.up*1.6f;camera.allowHDR=true;camera.nearClipPlane=.05f;camera.farClipPlane=80;camera.fieldOfView=65;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.11f,.19f,.28f);camera.gameObject.AddComponent<AudioListener>();
                var data=camera.GetUniversalAdditionalCameraData();data.allowHDROutput=false;data.renderPostProcessing=true;data.requiresDepthTexture=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                var input=player.AddComponent<PlayerInputReader>();player.AddComponent<SalvageMotor>().Configure(camera.transform);player.AddComponent<LevelPreview>().Configure(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(Pipeline),input,ArtSampleScene.ResolveFont());player.SetActive(true);
                var sun=new GameObject("IL_Skylight").AddComponent<Light>();sun.transform.SetParent(parent,false);sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(68,-25,0);sun.color=new Color(.88f,.94f,1);sun.intensity=1.2f;sun.shadows=LightShadows.Soft;
                RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.3f,.4f,.53f);RenderSettings.ambientEquatorColor=new Color(.13f,.17f,.21f);RenderSettings.ambientGroundColor=new Color(.06f,.08f,.10f);RenderSettings.fog=false;
                RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom;RenderSettings.customReflectionTexture=AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/EchoStation/SalvageNext/Generated/ES_Next_StudioReflection.asset");RenderSettings.reflectionIntensity=.65f;
                BuildNav(parent,"A");if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Save A01 scene");
                File.WriteAllText(Evidence+"/Created_A.json",new JObject{{"scene",ScenePath},{"grossArea",1152},{"conservativeNetArea",LevelLayout.ConservativeNetArea(parent)},{"floors",72},{"ai",false}}.ToString());
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);parent=null;}
        }
    }
}
