using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextLampStreamingSetup
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-P01a-2b3",Group="ES_Next_CeilingLights";
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();Directory.CreateDirectory(Evidence);if(NextAddressables.Settings.groups.Any(g=>g&&g.Name==Group))throw new InvalidOperationException("Lamp group already registered.");foreach(int index in Enumerable.Range(0,3))File.Copy(NextRoomPartition.RoomPath(index),Evidence+"/Room"+index+"_Before_A.unity");var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(NextLampAssets.MeshPath);var materials=new[]{AssetDatabase.LoadAssetAtPath<Material>(NextLampAssets.BodyPath),AssetDatabase.LoadAssetAtPath<Material>(NextLampAssets.LightPath)};var previous=SceneManager.GetActiveScene();
            using(NextSdkSaveScope.Begin(Evidence+"/PrepareSaveScope_A.json"))
            {
                var settings=NextAddressables.Settings;var group=settings.CreateGroup(Group,false,false,false,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));NextAddressables.ConfigureLocal(group);foreach(var path in new[]{NextLampAssets.MeshPath,NextLampAssets.BodyPath,NextLampAssets.LightPath})settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path),group).address="es-next-"+Path.GetFileNameWithoutExtension(path);EditorUtility.SetDirty(settings);NextAddressables.SaveOwned();var rows=new JArray();
                for(int index=0;index<3;index++)
                {
                    var scene=EditorSceneManager.OpenScene(NextRoomPartition.RoomPath(index),OpenSceneMode.Additive);
                    try{var root=scene.GetRootGameObjects().Single();if(root.activeSelf||root.GetComponent<InstancedRoomLamps>())throw new InvalidOperationException("Expected original inactive room.");var positions=Enumerable.Range(0,2).SelectMany(x=>Enumerable.Range(0,3).Select(z=>new Vector3(index*12+3+x*6,2.91f,4+z*8))).ToArray();root.AddComponent<InstancedRoomLamps>().Configure(mesh,materials,positions);if(root.GetComponentsInChildren<MeshRenderer>(true).Length!=(index==0?13:12)||root.GetComponentsInChildren<Light>(true).Length!=0||root.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Instance lights changed static atlas/physics/light budget.");if(!EditorSceneManager.SaveScene(scene))throw new IOException("Lamp room save failed.");rows.Add(new JObject{{"room",index},{"instances",positions.Length},{"newRealtimeLights",0},{"nativeStaticRenderers",index==0?13:12},{"initiallyInactive",true}});}
                    finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
                }
                File.WriteAllText(Evidence+"/Prepared_A.json",new JObject{{"group",Group},{"sharedAssets",3},{"rooms",rows},{"totalLamps",18},{"existingBakedAreaLights",18},{"newRealtimeLights",0}}.ToString());
            }
        }
    }
}
