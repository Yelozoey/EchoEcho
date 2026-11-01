using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.World
{
    public class CabinLayoutTests
    {
        private AsyncOperation pendingUnload;
        [Test]public void A01_RoomGeometryAnchorsAndFullCollisionsAreExplicit()
        {
            var scene=EditorSceneManager.OpenPreviewScene(CabinGreyboxSetup.ScenePath);
            try
            {
                var root=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox");var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.name.StartsWith(CabinStaticOptimization.Prefix)&&!r.GetComponent<EchoStation.World.ClueSource>()).ToArray();
                Assert.That(renderers.Length,Is.EqualTo(30));Assert.That(root.GetComponentsInChildren<BoxCollider>().Count(c=>!c.GetComponent<EchoStation.World.ClueSource>()),Is.EqualTo(30));
                foreach(var renderer in renderers)
                {
                    Assert.That(renderer.transform.lossyScale,Is.EqualTo(Vector3.one));Assert.That(renderer.sharedMaterial,Is.Not.Null);
                    Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount,Is.GreaterThan(0));Assert.That(renderer.GetComponent<BoxCollider>().enabled,Is.True);
                }
                foreach(var name in new[]{"Cabin_Room_Main","Cabin_Room_Storage","Cabin_Room_Foyer","Cabin_Anchors"})Assert.That(root.transform.Find(name),Is.Not.Null);
                Assert.That(root.transform.Find("Cabin_Anchors/Spawn_Player").position,Is.EqualTo(new Vector3(0,.05f,-6)));
                Assert.That(root.transform.Find("Cabin_Anchors/Spawn_Echo").position,Is.EqualTo(new Vector3(1.5f,0,-2)));
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [Test]public void A02_ProtectedAssetsAndRepeatCreationAreUnchanged()
        {
            var snapshot=JsonUtility.FromJson<ForwardHdrBaselineSetup.SetupRecord>(File.ReadAllText(Path.Combine(CabinGreyboxSetup.EvidencePath,"ProtectedFiles.json")));
            foreach(var item in snapshot.templates)Assert.That(ForwardHdrBaselineSetup.Hash(item.path),Is.EqualTo(item.sha256),item.path);
            var hash=ForwardHdrBaselineSetup.Hash(CabinGreyboxSetup.ScenePath);CabinGreyboxSetup.Create();Assert.That(ForwardHdrBaselineSetup.Hash(CabinGreyboxSetup.ScenePath),Is.EqualTo(hash));
        }
        [UnityTest]public IEnumerator A03_ActualCapsuleSweepsTraverseDoorwaysAndBlockBoundaries()
        {
            yield return new EnterPlayMode();var background=Application.runInBackground;Application.runInBackground=true;
            var scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));
            while(!scene.isLoaded)yield return null;
            try
            {
                var report=CabinCollisionProbe.Run(scene.GetPhysicsScene());File.WriteAllText(Path.Combine(CabinGreyboxSetup.EvidencePath,"A-Collision.json"),JsonUtility.ToJson(report,true));
                Assert.That(report.checks.Length,Is.EqualTo(23));Assert.That(report.passed,Is.True,JsonUtility.ToJson(report,true));
            }
            finally{pendingUnload=SceneManager.UnloadSceneAsync(scene);Application.runInBackground=background;}
        }
        [UnityTearDown]public IEnumerator LeaveRuntime()
        {
            var background=Application.runInBackground;Application.runInBackground=true;
            try
            {
                var timer=System.Diagnostics.Stopwatch.StartNew();while(pendingUnload!=null&&!pendingUnload.isDone)
                {if(timer.Elapsed.TotalSeconds>5)throw new System.TimeoutException("Cabin scene unload timed out.");yield return null;}
            }
            finally{pendingUnload=null;Application.runInBackground=background;}
            if(Application.isPlaying)yield return new ExitPlayMode();
        }
    }
}
