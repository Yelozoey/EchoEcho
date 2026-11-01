using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextLampAssets
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-P01a-2b1",MeshPath=FacilityAssets.Generated+"/ES_Next_CeilingLamp.asset",BodyPath=FacilityAssets.Generated+"/ES_Next_CeilingLamp_Body.mat",LightPath=FacilityAssets.Generated+"/ES_Next_CeilingLamp_Light.mat";
        public static string SourcePath=>FacilityAssets.Prefabs+"/ES_Prop_Light_Wide.prefab";
        public static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static void Save(Object asset){AssetDatabase.SetLabels(asset,new[]{"EchoStation-SalvageNext-Generated"});EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();if(AssetDatabase.LoadMainAssetAtPath(MeshPath)||AssetDatabase.LoadMainAssetAtPath(BodyPath)||AssetDatabase.LoadMainAssetAtPath(LightPath))throw new InvalidOperationException("Preserve existing lamp assets.");Directory.CreateDirectory(Evidence);string sourceHash=Hash(SourcePath);
            using(NextSdkSaveScope.Begin(Evidence+"/PrepareSaveScope_A.json"))
            {
                var prefab=PrefabUtility.LoadPrefabContents(SourcePath);
                try
                {
                    var renderers=prefab.GetComponentsInChildren<MeshRenderer>();if(renderers.Length!=1||renderers[0].sharedMaterials.Length!=2)throw new InvalidOperationException("Expected Wide Light mesh with two slots.");var filter=renderers[0].GetComponent<MeshFilter>();var original=filter.sharedMesh;var bounds=NextLayout.BoundsOf(prefab);var ratio=new Vector3(2.2f/bounds.size.x,.16f/bounds.size.y,.4f/bounds.size.z);var matrix=Matrix4x4.Scale(ratio)*Matrix4x4.Translate(-bounds.center)*filter.transform.localToWorldMatrix;var combines=new CombineInstance[original.subMeshCount];for(int s=0;s<combines.Length;s++)combines[s]=new CombineInstance{mesh=original,subMeshIndex=s,transform=matrix};var mesh=new Mesh{name="ES_Next_CeilingLamp",indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(combines,false,true);mesh.RecalculateBounds();var slot=Array.FindIndex(renderers[0].sharedMaterials,m=>m.name=="ES_Light");if(slot<0)throw new InvalidOperationException("Emissive material slot missing.");var indices=mesh.GetIndices(slot);var normals=mesh.normals;Vector3 average=Vector3.zero;foreach(int index in indices)average+=normals[index];bool flipped=average.y>0;
                    if(flipped){var rotation=Quaternion.Euler(180,0,0);var vertices=mesh.vertices;var tangents=mesh.tangents;for(int i=0;i<vertices.Length;i++){vertices[i]=rotation*vertices[i];normals[i]=rotation*normals[i];if(tangents.Length==vertices.Length){var t=rotation*new Vector3(tangents[i].x,tangents[i].y,tangents[i].z);tangents[i]=new Vector4(t.x,t.y,t.z,tangents[i].w);}}mesh.vertices=vertices;mesh.normals=normals;if(tangents.Length==vertices.Length)mesh.tangents=tangents;mesh.RecalculateBounds();}
                    AssetDatabase.CreateAsset(mesh,MeshPath);Save(mesh);var painted=AssetDatabase.FindAssets("t:Material ES_NextLit_ES_Trim01",new[]{FacilityAssets.Generated}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).Single();var body=Object.Instantiate(painted);body.name="ES_Next_CeilingLamp_Body";body.enableInstancing=true;body.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;body.SetColor("_EmissionColor",new Color(.015f,.03f,.045f));body.EnableKeyword("_EMISSION");AssetDatabase.CreateAsset(body,BodyPath);Save(body);var light=Object.Instantiate(renderers[0].sharedMaterials[slot]);light.name="ES_Next_CeilingLamp_Light";light.enableInstancing=true;light.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;light.SetColor("_EmissionColor",new Color(.45f,1.5f,3));light.EnableKeyword("_EMISSION");AssetDatabase.CreateAsset(light,LightPath);Save(light);
                    var emission=light.GetColor("_EmissionColor");if(Hash(SourcePath)!=sourceHash)throw new InvalidOperationException("Source light prefab changed.");File.WriteAllText(Evidence+"/Geometry_A.json",new JObject{{"source",SourcePath},{"sourceShaBefore",sourceHash},{"sourceShaAfter",Hash(SourcePath)},{"sourceCorners",Enumerable.Range(0,original.subMeshCount).Sum(s=>(long)original.GetIndexCount(s))},{"derivedCorners",Enumerable.Range(0,mesh.subMeshCount).Sum(s=>(long)mesh.GetIndexCount(s))},{"subMeshes",mesh.subMeshCount},{"emissiveSlot",slot},{"flippedDown",flipped},{"size",new JArray(mesh.bounds.size.x,mesh.bounds.size.y,mesh.bounds.size.z)},{"nativeEmissionColor",new JArray(emission.r,emission.g,emission.b)},{"newRealtimeLights",0}}.ToString());
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
        }
    }
}
