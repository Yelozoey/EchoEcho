using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using Newtonsoft.Json.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextDecor
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R02a-2",ShaderPath=FacilityAssets.Root+"/Shaders/NextInstancedDecor.shader";
        public static string PathFor(string name)=>FacilityAssets.Generated+"/ES_Next_Decor_"+name;
        static void Save(Object asset,string path){if(AssetDatabase.LoadMainAssetAtPath(path))throw new InvalidOperationException("Keep generated decor "+path);AssetDatabase.CreateAsset(asset,path);AssetDatabase.SetLabels(asset,new[]{"EchoStation-SalvageNext-Generated"});AssetDatabase.SaveAssetIfDirty(asset);}
        static Mesh Quad(bool cross)
        {
            var mesh=new Mesh();var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();for(int plane=0;plane<(cross?2:1);plane++){int n=vertices.Count;var q=Quaternion.Euler(0,plane*90,0);foreach(var p in new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)})vertices.Add(q*p);uv.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});}mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static Material Material(string name,Texture texture,Color tint,float wind)
        {var material=new Material(AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath)){name=name,enableInstancing=true};material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",tint);material.SetFloat("_Wind",wind);Save(material,PathFor(name+".mat"));return material;}
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();Directory.CreateDirectory(Evidence);if(File.Exists(Evidence+"/BeforeDecor.unity"))throw new InvalidOperationException("Decor already prepared.");File.Copy(NextLayout.ScenePath,Evidence+"/BeforeDecor.unity");File.Copy(NextLayout.NavPath,Evidence+"/BeforeDecorNav.asset");File.Copy(NextLayout.NavPath+".meta",Evidence+"/BeforeDecorNav.asset.meta");
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(FacilityAssets.Prefabs+"/ES_Prop_Crate3.prefab");var filter=source.GetComponentsInChildren<MeshFilter>().Single();var bounds=filter.sharedMesh.bounds;float factor=1/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);var original=Object.Instantiate(filter.sharedMesh);original.name="Crate_LOD0";var v=original.vertices;for(int i=0;i<v.Length;i++)v[i]=(v[i]-bounds.center)*factor;original.vertices=v;original.RecalculateBounds();Save(original,PathFor("LOD0.asset"));var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var simple=Object.Instantiate(cube.GetComponent<MeshFilter>().sharedMesh);Object.DestroyImmediate(cube);simple.name="Crate_LOD1";Save(simple,PathFor("LOD1.asset"));var cross=Quad(true);cross.name="Crate_LOD2";Save(cross,PathFor("LOD2.asset"));var billboard=Quad(false);billboard.name="Crate_Impostor";Save(billboard,PathFor("Impostor.asset"));var strip=Quad(false);strip.name="SoftStrip";Save(strip,PathFor("Strip.asset"));
            var old=SceneManager.GetActiveScene();var bench=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);Texture2D image=null;
            try
            {
                SceneManager.SetActiveScene(bench);var instance=(GameObject)PrefabUtility.InstantiatePrefab(source,bench);var initial=NextLayout.BoundsOf(instance);instance.transform.localScale*=1/Mathf.Max(initial.size.x,initial.size.y,initial.size.z);var b=NextLayout.BoundsOf(instance);instance.transform.position-=b.center;var light=new GameObject("Main Light").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-25,0);light.intensity=1.5f;RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.gray;var camera=new GameObject("ImpostorCamera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=.58f;camera.aspect=1;camera.transform.position=new Vector3(0,.12f,-3);camera.transform.LookAt(Vector3.zero);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.allowHDR=false;camera.allowMSAA=false;camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;camera.scene=bench;var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);target.Create();var previous=RenderTexture.active;image=new Texture2D(256,256,TextureFormat.RGBA32,false);
                try{RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;image.ReadPixels(new Rect(0,0,256,256),0,0);image.Apply();File.WriteAllBytes(PathFor("Impostor.png"),image.EncodeToPNG());}finally{RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);}
            }
            finally{if(image)Object.DestroyImmediate(image);SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(bench,true);}
            AssetDatabase.ImportAsset(PathFor("Impostor.png"),ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(PathFor("Impostor.png"));importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();var texture=AssetDatabase.LoadAssetAtPath<Texture>(PathFor("Impostor.png"));var solid=Material("Solid",filter.GetComponent<MeshRenderer>().sharedMaterial.GetTexture("_BaseMap"),new Color(.65f,.75f,.85f),0);var far=Material("Billboard",texture,Color.white,0);var fabric=Material("Fabric",null,new Color(.22f,.38f,.48f),1);
            var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var camera=root.GetComponentInChildren<Camera>();var collision=new GameObject("DecorCollision");collision.transform.SetParent(root.transform,false);
                foreach(var room in root.GetComponentsInChildren<RoomVisual>()){var positions=new List<Vector3>();for(int z=0;z<2;z++)for(int x=0;x<6;x++){var p=new Vector3(12*room.Index+1+x*2,.47f,z==0?.65f:23.35f);positions.Add(p);var box=new GameObject("FixedEquipment").AddComponent<BoxCollider>();box.transform.SetParent(collision.transform,false);box.center=p;box.size=new Vector3(.77f,.78f,.77f);}var strips=Enumerable.Range(0,6).Select(i=>new Vector3(room.Index*12+2+i*1.6f,2.35f,23.65f)).ToArray();room.gameObject.AddComponent<InstancedRoomDecor>().Configure(camera,new[]{original,simple,cross,billboard},solid,far,strip,fabric,positions.ToArray(),strips);}
                var surface=root.GetComponent<NavMeshSurface>();var stored=surface.navMeshData;surface.RemoveData();surface.navMeshData=null;surface.BuildNavMesh();var rebuilt=surface.navMeshData;if(!rebuilt)throw new InvalidOperationException("Decor nav rebuild failed.");surface.RemoveData();EditorUtility.CopySerialized(rebuilt,stored);surface.navMeshData=stored;surface.AddData();EditorUtility.SetDirty(stored);AssetDatabase.SaveAssetIfDirty(stored);Object.DestroyImmediate(rebuilt);EditorSceneManager.SaveScene(scene);File.WriteAllText(Evidence+"/Prepared_A.json",new JObject{{"rooms",3},{"instances",36},{"strips",18},{"triangles",new JArray(original.triangles.Length/3,simple.triangles.Length/3,cross.triangles.Length/3,billboard.triangles.Length/3)},{"navRebuiltNewOnly",true}}.ToString());
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
