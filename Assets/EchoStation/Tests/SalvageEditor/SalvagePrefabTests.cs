using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

namespace EchoStation.Salvage.Tests
{
    public class SalvagePrefabTests
    {
        GameObject root;UnityEngine.SceneManagement.Scene preview;
        [SetUp]public void Before(){preview=EditorSceneManager.NewPreviewScene();}
        [TearDown]public void After(){if(root)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview);}
        GameObject Load(int i)
        {
            var obj=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SalvagePrefabSetup.Path(i)));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj,preview);return obj;
        }
        [TestCase(0)][TestCase(1)]public void PrefabHasIdentityRootAndMaterials(int i)
        {
            root=Load(i);Assert.That(root.transform.position,Is.EqualTo(Vector3.zero));Assert.That(root.transform.rotation,Is.EqualTo(Quaternion.identity));Assert.That(root.transform.localScale,Is.EqualTo(Vector3.one));
            Assert.That(root.GetComponentsInChildren<Rigidbody>(),Is.Empty);Assert.That(root.GetComponentsInChildren<Collider>().Length,Is.EqualTo(1));
            foreach(var r in root.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials)Assert.That(AssetDatabase.GetAssetPath(m),Is.EqualTo(SalvageAssetImport.MaterialPath(i)));
            var c=root.GetComponent<BoxCollider>();var b=SalvageAssetImport.BoundsOf(root);
            Assert.That(c.center.x,Is.EqualTo(b.center.x).Within(.001));Assert.That(c.size.x,Is.EqualTo(b.size.x).Within(.001));
            Assert.That(c.size.z,Is.EqualTo(b.size.z).Within(.001));
            if(i==0){Assert.That(c.center.y+c.size.y/2,Is.EqualTo(b.max.y).Within(.001));Assert.That(c.size.y,Is.EqualTo(.1f).Within(.001));}
            else{Assert.That(c.center.y,Is.EqualTo(b.center.y).Within(.001));Assert.That(c.size.y,Is.EqualTo(b.size.y).Within(.001));}
        }
        [Test]public void FloorRaycastMatchesTopAndOutsideMisses()
        {
            root=Load(0);Physics.SyncTransforms();var c=root.GetComponent<BoxCollider>();
            foreach(var p in new[]{new Vector3(0,2,0),new Vector3(1.8f,2,1.8f),new Vector3(-1.8f,2,-1.8f)})
            {Assert.That(c.Raycast(new Ray(p,Vector3.down),out var hit,5),Is.True);Assert.That(hit.point.y,Is.EqualTo(0).Within(.001));}
            Assert.That(c.Raycast(new Ray(new Vector3(2.1f,2,0),Vector3.down),out _,5),Is.False);
        }
        [Test]public void ToolboxRaycastsWorkFromSixSides()
        {
            root=Load(1);Physics.SyncTransforms();var c=root.GetComponent<BoxCollider>();
            foreach(var d in new[]{Vector3.up,Vector3.down,Vector3.left,Vector3.right,Vector3.forward,Vector3.back})Assert.That(c.Raycast(new Ray(c.center+d*2,-d),out _,4),Is.True,d.ToString());
            Assert.That(c.Raycast(new Ray(c.center+Vector3.right*2+Vector3.up*2,Vector3.down),out _,4),Is.False);
        }
        [Test]public void TwoFloorModulesMeetOnFourMeterGrid()
        {
            root=new GameObject("OwnedTest");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
            var a=Load(0);a.transform.SetParent(root.transform);var b=Load(0);b.transform.SetParent(root.transform);b.transform.position=Vector3.right*4;
            var first=SalvageAssetImport.BoundsOf(a);var second=SalvageAssetImport.BoundsOf(b);
            Assert.That(first.max.x,Is.EqualTo(second.min.x).Within(.001));Assert.That(first.center.y,Is.EqualTo(second.center.y).Within(.001));
        }
    }
}
