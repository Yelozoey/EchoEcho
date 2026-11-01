using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextMeshMerge
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R02a-1";
        sealed class Part{public MeshRenderer Renderer;public Mesh Mesh;public int Submesh;public Material Material;}
        public static MeshRenderer[] Merge(RoomVisual room,bool persist,string outputFolder=null,string prefix="ES_Next")
        {
            if(room.GetComponentInChildren<MergedRoomMesh>())throw new InvalidOperationException("Already merged.");
            if(persist)foreach(Transform child in room.transform)if(PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))PrefabUtility.UnpackPrefabInstance(child.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            var sources=room.GetComponentsInChildren<MeshRenderer>();var parts=new List<Part>();
            foreach(var renderer in sources)
            {
                var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh||!filter.sharedMesh.isReadable||renderer.HasPropertyBlock()||renderer.additionalVertexStreams||renderer.GetComponentInParent<LODGroup>())throw new InvalidOperationException("Unsupported static renderer "+renderer.name);
                var mesh=filter.sharedMesh;if(mesh.uv2.Length!=mesh.vertexCount||renderer.lightmapIndex<0||renderer.lightmapIndex>=LightmapSettings.lightmaps.Length||renderer.sharedMaterials.Length!=mesh.subMeshCount)throw new InvalidOperationException("Invalid baked geometry "+renderer.name);
                for(int sub=0;sub<mesh.subMeshCount;sub++)if(mesh.GetIndexCount(sub)>0)parts.Add(new Part{Renderer=renderer,Mesh=mesh,Submesh=sub,Material=renderer.sharedMaterials[sub]});
            }
            var groups=parts.GroupBy(p=>new{p.Material,p.Renderer.lightmapIndex,p.Renderer.gameObject.layer,p.Renderer.shadowCastingMode,p.Renderer.receiveShadows,p.Renderer.renderingLayerMask,p.Renderer.reflectionProbeUsage});var outputs=new List<MeshRenderer>();int number=0;
            foreach(var group in groups)
            {
                string name=prefix+"_Room"+room.Index+"_Merged_"+number++;string path=(outputFolder??FacilityAssets.Generated)+"/"+name+".asset";if(persist&&AssetDatabase.LoadMainAssetAtPath(path))throw new InvalidOperationException("Preserve prior merged mesh.");
                var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(group.Select(p=>new CombineInstance{mesh=p.Mesh,subMeshIndex=p.Submesh,transform=room.transform.worldToLocalMatrix*p.Renderer.localToWorldMatrix,lightmapScaleOffset=p.Renderer.lightmapScaleOffset}).ToArray(),true,true,true);mesh.RecalculateBounds();
                if(persist){AssetDatabase.CreateAsset(mesh,path);AssetDatabase.SetLabels(mesh,new[]{"EchoStation-SalvageNext-Generated"});AssetDatabase.SaveAssetIfDirty(mesh);}
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MergedRoomMesh));go.transform.SetParent(room.transform,false);go.layer=group.Key.layer;go.GetComponent<MeshFilter>().sharedMesh=mesh;var target=go.GetComponent<MeshRenderer>();target.sharedMaterial=group.Key.Material;target.shadowCastingMode=group.Key.shadowCastingMode;target.receiveShadows=group.Key.receiveShadows;target.renderingLayerMask=group.Key.renderingLayerMask;target.reflectionProbeUsage=group.Key.reflectionProbeUsage;target.lightProbeUsage=LightProbeUsage.Off;target.receiveGI=ReceiveGI.Lightmaps;GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.ReflectionProbeStatic);go.GetComponent<MergedRoomMesh>().Configure(target,group.Key.lightmapIndex);outputs.Add(target);
            }
            foreach(var source in sources){Object.DestroyImmediate(source.GetComponent<MeshFilter>());Object.DestroyImmediate(source);}
            var shadows=room.GetComponent<DistanceShadowCaster>();if(shadows)shadows.Configure(shadows.TargetCamera,outputs.ToArray(),30,2,.1f);return outputs.ToArray();
        }
        public static void RemoveEmptySourceLinks()
        {
            SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);try{SceneManager.SetActiveScene(scene);foreach(var room in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RoomVisual>()))foreach(var child in room.transform.Cast<Transform>().ToArray()){if(PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))PrefabUtility.UnpackPrefabInstance(child.gameObject,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);if(child.GetComponentsInChildren<Component>(true).All(c=>c is Transform))Object.DestroyImmediate(child.gameObject);}EditorSceneManager.SaveScene(scene);}finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();Directory.CreateDirectory(Evidence);string backup=Evidence+"/BeforeMerge.unity";if(File.Exists(backup))throw new InvalidOperationException("Keep merge baseline.");File.Copy(NextLayout.ScenePath,backup);var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextLayout.ScenePath,OpenSceneMode.Additive);
            try{SceneManager.SetActiveScene(scene);var rooms=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RoomVisual>()).ToArray();int before=rooms.Sum(r=>r.GetComponentsInChildren<MeshRenderer>().Length),after=0;foreach(var room in rooms)after+=Merge(room,true).Length;if(!EditorSceneManager.SaveScene(scene))throw new IOException("Merge scene save failed.");File.WriteAllText(Evidence+"/Prepared_A.json",new JObject{{"before",before},{"after",after},{"sourceComponentsRemoved",true},{"scope","new scene only"}}.ToString());}
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
