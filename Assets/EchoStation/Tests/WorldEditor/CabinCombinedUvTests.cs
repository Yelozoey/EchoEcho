using System.Linq;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EchoStation.Tests.World
{
    public class CabinCombinedUvTests
    {
        [Test]public void B101_CombinedRoomMeshesHaveUniqueBakedUvsAndKeepAllColliders()
        {
            var scene=EditorSceneManager.OpenPreviewScene(CabinGreyboxSetup.ScenePath);
            try
            {
                var root=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox");var all=root.GetComponentsInChildren<MeshRenderer>();
                var merged=all.Where(r=>r.name.StartsWith(CabinStaticOptimization.Prefix)).ToArray();Assert.That(merged.Length,Is.EqualTo(8));
                Assert.That(all.Count(r=>r.enabled&&!r.GetComponent<EchoStation.World.ClueSource>()),Is.EqualTo(8));Assert.That(root.GetComponentsInChildren<BoxCollider>().Count(c=>!c.GetComponent<EchoStation.World.ClueSource>()),Is.EqualTo(30));
                foreach(var renderer in merged)
                {
                    Assert.That(renderer.transform.parent.name,Does.StartWith("Cabin_Room_"));
                    Assert.That(LightmapUvValidator.IsValidNonOverlapping(renderer.GetComponent<MeshFilter>().sharedMesh,out var error),Is.True,renderer.name+" "+error);
                }
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test]public void B102_ValidatorRejectsActualOverlappingAndNonfiniteUvs()
        {
            var mesh=new Mesh();
            try
            {
                mesh.vertices=new[]{Vector3.zero,Vector3.right,Vector3.up,Vector3.forward,Vector3.right+Vector3.forward,Vector3.up+Vector3.forward};
                mesh.triangles=new[]{0,1,2,3,4,5};mesh.uv2=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.zero,Vector2.right,Vector2.up};
                Assert.That(LightmapUvValidator.IsValidNonOverlapping(mesh,out _),Is.False);
                var uv=mesh.uv2;uv[0]=new Vector2(float.NaN,0);mesh.uv2=uv;
                Assert.That(LightmapUvValidator.IsValidNonOverlapping(mesh,out _),Is.False);
            }
            finally{Object.DestroyImmediate(mesh);}
        }
    }
}
