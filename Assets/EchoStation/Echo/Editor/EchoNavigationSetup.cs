using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.World.Editor;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace EchoStation.Echo.Editor
{
    public static class EchoNavigationSetup
    {
        public const string DataPath="Assets/EchoStation/Echo/Navigation/ES_Cabin_NavMesh.asset";
        public const string Evidence="Docs/Tests/Stage1/Evidence/S1-E02";
        public static int AgentTypeId
        {
            get{for(var i=0;i<NavMesh.GetSettingsCount();i++){var value=NavMesh.GetSettingsByIndex(i);if(NavMesh.GetSettingsNameFromID(value.agentTypeID)=="Echo")return value.agentTypeID;}return -1;}
        }
        [MenuItem("Tools/EchoStation/Stage 1/E02/Bake Cabin Echo Navigation")]
        public static void Bake()=>BakeInternal(false);
        [MenuItem("Tools/EchoStation/Stage 1/E02/Rebuild Owned Cabin Echo Navigation")]
        public static void Rebuild()=>BakeInternal(true);
        private static void BakeInternal(bool rebuild)
        {
            SceneSafetyGuard.RequireClean();if(Lightmapping.isRunning)throw new InvalidOperationException("Wait for lighting bake.");Directory.CreateDirectory(Evidence);
            if(!File.Exists(Evidence+"/NavMeshAreas-Before.asset.txt"))File.Copy("ProjectSettings/NavMeshAreas.asset",Evidence+"/NavMeshAreas-Before.asset.txt");
            ConfigureAgent();CabinGreyboxSetup.EnsureFolder("Assets/EchoStation/Echo/Navigation");
            var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(CabinGreyboxSetup.ScenePath);var opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(CabinGreyboxSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                var root=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox");var surface=root.GetComponent<NavMeshSurface>();if(!surface)surface=root.AddComponent<NavMeshSurface>();
                surface.agentTypeID=AgentTypeId;surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.ignoreNavMeshAgent=true;surface.ignoreNavMeshObstacle=true;
                surface.overrideVoxelSize=true;surface.voxelSize=.08f;surface.overrideTileSize=true;surface.tileSize=128;surface.minRegionArea=.1f;
                var serializedSurface=new SerializedObject(surface);serializedSurface.FindProperty("m_GenerateLinks").boolValue=false;serializedSurface.ApplyModifiedPropertiesWithoutUndo();
                var existing=AssetDatabase.LoadAssetAtPath<NavMeshData>(DataPath);
                if(existing&&!rebuild){surface.navMeshData=existing;surface.AddData();}
                else
                {
                    surface.BuildNavMesh();if(!surface.navMeshData)throw new InvalidOperationException("NavMesh build returned no data.");
                    // Only this explicit owned generated asset may be replaced, after a successful bake.
                    if(existing&&AssetDatabase.GetAssetPath(existing)==DataPath&&!AssetDatabase.DeleteAsset(DataPath))throw new IOException("Cannot replace generated cabin NavMesh.");
                    AssetDatabase.CreateAsset(surface.navMeshData,DataPath);
                }
                EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();var triangles=NavMesh.CalculateTriangulation();
                File.WriteAllText(Evidence+"/Bake.json",JsonUtility.ToJson(new Record{agentTypeId=AgentTypeId,vertices=triangles.vertices.Length,indices=triangles.indices.Length,dataPath=DataPath},true));
            }
            finally{if(previous.IsValid())SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        private static void ConfigureAgent()
        {
            var id=AgentTypeId;if(id==-1)id=NavMesh.CreateSettings().agentTypeID;var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/NavMeshAreas.asset")[0]);settings.Update();
            var agents=settings.FindProperty("m_Settings");var names=settings.FindProperty("m_SettingNames");
            for(var i=0;i<agents.arraySize;i++)
            {
                var entry=agents.GetArrayElementAtIndex(i);if(entry.FindPropertyRelative("agentTypeID").intValue!=id)continue;
                entry.FindPropertyRelative("agentRadius").floatValue=.54f;entry.FindPropertyRelative("agentHeight").floatValue=.9f;entry.FindPropertyRelative("agentClimb").floatValue=.15f;entry.FindPropertyRelative("agentSlope").floatValue=35;
                if(names.arraySize<=i)names.arraySize=i+1;names.GetArrayElementAtIndex(i).stringValue="Echo";settings.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();return;
            }
            throw new InvalidOperationException("Created Echo agent entry was not found.");
        }
        [Serializable]private class Record{public int agentTypeId,vertices,indices;public string dataPath;}
    }
}
