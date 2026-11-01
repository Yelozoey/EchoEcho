using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Rendering
{
    public class StaticRoomMeshTests
    {
        private Scene scene;
        private GameObject room;
        private MeshRenderer[] sources;
        private Mesh mesh;
        private Material material;
        private MeshRenderer combined;
        [SetUp] public void Setup()
        {
            scene=EditorSceneManager.NewPreviewScene(); room=new GameObject("R05_TestRoom"); SceneManager.MoveGameObjectToScene(room,scene);
            room.transform.SetPositionAndRotation(new Vector3(7,2,-3),Quaternion.Euler(0,27,0)); room.transform.localScale=new Vector3(1.2f,.8f,1.1f);
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")); sources=new MeshRenderer[3];
            for(var i=0;i<3;i++)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="R05_Source_"+i;go.transform.SetParent(room.transform,false);
                go.transform.localPosition=new Vector3(i*2,.5f,i*.3f);go.transform.localRotation=Quaternion.Euler(0,i*17,0);go.transform.localScale=new Vector3(1,1+i*.2f,1);
                if(!mesh){mesh=Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);mesh.uv2=mesh.uv;}
                go.GetComponent<MeshFilter>().sharedMesh=mesh;sources[i]=go.GetComponent<MeshRenderer>();sources[i].sharedMaterial=material;
                GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.OccludeeStatic|StaticEditorFlags.ContributeGI);
            }
        }
        [TearDown] public void Cleanup()
        {
            if(combined && combined.GetComponent<MeshFilter>().sharedMesh && !AssetDatabase.Contains(combined.GetComponent<MeshFilter>().sharedMesh))
                Object.DestroyImmediate(combined.GetComponent<MeshFilter>().sharedMesh);
            if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
            if(mesh)Object.DestroyImmediate(mesh);if(material)Object.DestroyImmediate(material);
        }
        [Test] public void A01_WorldGeometryAndCollisionsSurviveTransformedRoomMerge()
        {
            var expected=sources.SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>r.transform.TransformPoint(v))).ToArray();
            combined=StaticRoomMeshCombiner.Combine(room.transform,sources);var result=combined.GetComponent<MeshFilter>().sharedMesh;
            var actual=result.vertices.Select(v=>combined.transform.TransformPoint(v)).ToArray();
            Assert.That(actual.Length,Is.EqualTo(expected.Length));foreach(var vertex in expected) Assert.That(actual.Min(v=>(v-vertex).sqrMagnitude),Is.LessThan(1e-7f));
            Assert.That(result.triangles.Length,Is.EqualTo(mesh.triangles.Length*3));Assert.That(result.subMeshCount,Is.EqualTo(1));
            Assert.That(sources.All(r=>!r.enabled && r.gameObject.activeSelf && r.GetComponent<Collider>().enabled),Is.True);
            Assert.That(combined.GetComponent<Collider>(),Is.Null);
        }
        [Test] public void A02_BakedUv2UsesEachInputAtlasRegion()
        {
            for(var i=0;i<3;i++){sources[i].lightmapIndex=0;sources[i].lightmapScaleOffset=new Vector4(.2f,.3f,i*.3f,.4f);}
            var expected=sources.SelectMany(r=>mesh.uv2.Select(uv=>new Vector2(uv.x*.2f+r.lightmapScaleOffset.z,uv.y*.3f+.4f))).ToArray();
            combined=StaticRoomMeshCombiner.Combine(room.transform,sources);var actual=combined.GetComponent<MeshFilter>().sharedMesh.uv2;
            Assert.That(actual.Length,Is.EqualTo(expected.Length));foreach(var uv in expected)Assert.That(actual.Min(v=>(v-uv).sqrMagnitude),Is.LessThan(1e-7f));
            Assert.That(combined.lightmapIndex,Is.Zero);Assert.That(combined.lightmapScaleOffset,Is.EqualTo(new Vector4(1,1,0,0)));
        }
        [TestCase("other_room")] [TestCase("material")] [TestCase("lightmap")] [TestCase("dynamic")] [TestCase("duplicate")]
        [TestCase("lod")] [TestCase("negative_scale")] [TestCase("property_block")] [TestCase("layer")]
        public void A03_InvalidMergeIsAtomicAndLeavesSourceObjectsIntact(string fault)
        {
            var changed=sources[1];Material alternate=null;
            try
            {
                if(fault=="other_room")changed.transform.SetParent(null);
                if(fault=="material"){alternate=new Material(material);changed.sharedMaterial=alternate;}
                if(fault=="lightmap")changed.lightmapIndex=1;
                if(fault=="dynamic")GameObjectUtility.SetStaticEditorFlags(changed.gameObject,0);
                if(fault=="duplicate")sources[1]=sources[0];
                if(fault=="lod")changed.gameObject.AddComponent<LODGroup>();
                if(fault=="negative_scale")changed.transform.localScale=new Vector3(-1,1,1);
                if(fault=="property_block"){var block=new MaterialPropertyBlock();block.SetFloat("_Smoothness",.1f);changed.SetPropertyBlock(block);}
                if(fault=="layer")changed.gameObject.layer=2;
                var count=room.transform.childCount;
                Assert.Throws<ArgumentException>(()=>StaticRoomMeshCombiner.Combine(room.transform,sources));
                Assert.That(sources.All(r=>r.enabled),Is.True);Assert.That(room.transform.childCount,Is.EqualTo(count));
            }
            finally { if(alternate)Object.DestroyImmediate(alternate); }
        }
        [Test] public void A04_PersistentMeshCanBeReimportedWithoutOverwrite()
        {
            var path="Assets/EchoStation/Tests/Fixtures/R05_"+Guid.NewGuid().ToString("N")+".asset";
            try
            {
                combined=StaticRoomMeshCombiner.Combine(room.transform,sources,path);AssetDatabase.SaveAssetIfDirty(combined.GetComponent<MeshFilter>().sharedMesh);
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                Assert.That(AssetDatabase.LoadAssetAtPath<Mesh>(path).vertexCount,Is.EqualTo(mesh.vertexCount*3));
                var hash=ForwardHdrBaselineSetup.Hash(path);
                foreach(var source in sources)source.enabled=true;
                Assert.Throws<ArgumentException>(()=>StaticRoomMeshCombiner.Combine(room.transform,sources,path));
                Assert.That(ForwardHdrBaselineSetup.Hash(path),Is.EqualTo(hash));Assert.That(sources.All(r=>r.enabled),Is.True);
            }
            finally { if(AssetDatabase.LoadAssetAtPath<Mesh>(path))AssetDatabase.DeleteAsset(path); }
        }
    }
}
