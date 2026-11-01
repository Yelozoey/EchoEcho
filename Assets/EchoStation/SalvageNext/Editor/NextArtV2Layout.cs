using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextArtV2Layout
    {
        public const string Root=FacilityAssets.Root+"/ArtV2",Generated=Root+"/Generated",Authoring=Root+"/ES_ArtV2_Authoring.unity";
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/Feedback02/V01";
        public static float Height(int room)=>room==2?5f:4.5f;
        static bool Wall(Transform t)=>t.name.StartsWith("ES_WallBand_",StringComparison.Ordinal)||t.name.StartsWith("InteriorOpposite_",StringComparison.Ordinal);
        public static bool IsWall(Transform t)=>Wall(t);
        static void Unpack(GameObject go){if(PrefabUtility.IsPartOfPrefabInstance(go))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(go),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);}
        public static void Fit(GameObject go,Bounds wanted)
        {
            Unpack(go);var b=NextLayout.BoundsOf(go);var ratio=new Vector3(wanted.size.x/b.size.x,wanted.size.y/b.size.y,wanted.size.z/b.size.z);
            // All inherited wall faces have only orthogonal yaw; size is specified in world axes.
            var local=Quaternion.Inverse(go.transform.rotation)*ratio;go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z)));go.transform.position+=wanted.center-NextLayout.BoundsOf(go).center;
        }
        public static void RaiseCollision(GameObject world)
        {
            foreach(var ceiling in world.GetComponents<BoxCollider>().Where(c=>c.size.x>35&&c.center.y>2).ToArray())Object.DestroyImmediate(ceiling);
            for(int i=0;i<3;i++){var c=new GameObject("ArtV2_CeilingCollision_"+i).AddComponent<BoxCollider>();c.transform.SetParent(world.transform,false);c.center=new Vector3(6+12*i,Height(i)+.1f,12);c.size=new Vector3(12,.2f,24);}
            foreach(Transform wall in world.transform)
            {
                if(!new[]{"West","East","South","North","Partition"}.Contains(wall.name))continue;var box=wall.GetComponent<BoxCollider>();if(!box)throw new InvalidOperationException("Missing original wall collision");
                var b=box.bounds;float h=wall.name=="East"||wall.name=="South"||wall.name=="North"||b.center.x>23.5f?5:4.5f;
                float bottom=b.min.y;var s=box.size;s.y=(h-bottom)/Mathf.Abs(wall.lossyScale.y);box.size=s;var c=box.center;c.y=wall.InverseTransformPoint(new Vector3(b.center.x,(h+bottom)/2,b.center.z)).y;box.center=c;
            }
        }
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();if(File.Exists(Authoring))throw new InvalidOperationException("Keep existing ArtV2 authoring scene");FacilityAssets.Folder(Generated);Directory.CreateDirectory(Evidence);
            var text=File.ReadAllText(NextMeshMerge.Evidence+"/BeforeMerge.unity");text=Regex.Replace(text,@"m_LightingDataAsset: \{[^\r\n]*\}","m_LightingDataAsset: {fileID: 0}");text=Regex.Replace(text,@"m_OcclusionCullingData: \{[^\r\n]*\}","m_OcclusionCullingData: {fileID: 0}");File.WriteAllText(Authoring,text);AssetDatabase.ImportAsset(Authoring,ImportAssetOptions.ForceSynchronousImport);
            var old=SceneManager.GetActiveScene();
            using(NextSdkSaveScope.Begin(Evidence+"/CreateSaveScope_A.json"))
            {
                var scene=EditorSceneManager.OpenScene(Authoring,OpenSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);var world=scene.GetRootGameObjects().Single();var rooms=world.GetComponentsInChildren<RoomVisual>().OrderBy(r=>r.Index).ToArray();var rows=new JArray();
                    foreach(var room in rooms)
                    {
                        float delta=Height(room.Index)-3;var children=room.transform.Cast<Transform>().ToArray();int ceilings=0,walls=0,beams=0;
                        foreach(var child in children)
                        {
                            if(child.name.StartsWith("ES_Platform_Simple",StringComparison.Ordinal)){Unpack(child.gameObject);child.position+=Vector3.up*delta;ceilings++;}
                            else if(child.name.StartsWith("ES_Column_MetalSupport",StringComparison.Ordinal)){Unpack(child.gameObject);child.position+=Vector3.up*delta;beams++;}
                            else if(Wall(child))
                            {
                                var b=NextLayout.BoundsOf(child.gameObject);var upper=Object.Instantiate(child.gameObject,room.transform);upper.name="ArtV2_UpperWall_"+walls++;Fit(upper,new Bounds(new Vector3(b.center.x,3+delta/2,b.center.z),new Vector3(b.size.x,delta,b.size.z)));
                            }
                        }
                        room.Configure(room.Index,Height(room.Index));var reflection=room.GetComponentInChildren<ReflectionProbe>();reflection.transform.position=room.Region.center;reflection.size=room.Region.size;reflection.mode=UnityEngine.Rendering.ReflectionProbeMode.Baked;reflection.customBakedTexture=null;
                        rows.Add(new JObject{{"room",room.Index},{"height",Height(room.Index)},{"ceilings",ceilings},{"upperWalls",walls},{"beams",beams}});
                        if(ceilings!=18||beams!=3||walls<4)throw new InvalidOperationException("Unexpected premerge geometry for room "+room.Index);
                    }
                    foreach(var door in new[]{new Vector3(12,0,6),new Vector3(12,0,18),new Vector3(24,0,12)})
                    {
                        int west=(int)(door.x/12)-1;
                        for(int side=0;side<2;side++)
                        {
                            int room=west+side;float delta=Height(room)-3;var source=rooms[room].transform.Cast<Transform>().First(Wall);var lintel=Object.Instantiate(source.gameObject,rooms[room].transform);lintel.name="ArtV2_DoorLintel_"+door.z+"_"+side;Unpack(lintel);lintel.transform.rotation=Quaternion.Euler(0,side==0?0:180,0);
                            Fit(lintel,new Bounds(new Vector3(door.x+(side==0?-.075f:.075f),3+delta/2,door.z),new Vector3(.15f,delta,3.5f)));
                        }
                    }
                    RaiseCollision(world);
                    foreach(var light in world.GetComponentsInChildren<Light>())if(light.name.StartsWith("Area_",StringComparison.Ordinal)){int room=int.Parse(light.name.Split('_')[1]);light.transform.position+=Vector3.up*(Height(room)-3);}
                    foreach(var shadows in rooms.Select(r=>r.GetComponent<DistanceShadowCaster>()))shadows.Configure(world.GetComponentInChildren<Camera>(),shadows.GetComponentsInChildren<MeshRenderer>(),30,2,.1f);
                    if(!EditorSceneManager.SaveScene(scene))throw new IOException("Authoring save failed");File.WriteAllText(Evidence+"/Created_A.json",new JObject{{"scene",Authoring},{"rooms",rows},{"sourceUnmodified",true},{"bakeNotYetRun",true}}.ToString());
                }
                finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
            }
        }
    }
}
