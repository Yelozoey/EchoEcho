using System.Collections;
using System.Linq;
using EchoStation.World;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace EchoStation.Tests.World
{
    public class TargetIdentityTests
    {
        private static WorldTarget Create(string id)
        {
            var go=new GameObject("IdentityFixture");go.SetActive(false);var target=go.AddComponent<WorldTarget>();target.Configure(id,"测试目标");return target;
        }
        [Test]public void W03_01_ExactLookupRejectsDuplicateWithoutReplacingOwner()
        {
            var registry=new TargetRegistry();var a=Create("target_a");var b=Create("target_b");var duplicate=Create("target_a");
            try
            {
                Assert.That(registry.TryRegister(a,out _)&&registry.TryRegister(b,out _),Is.True);
                Assert.That(registry.TryGet("target_a",out var found)&&ReferenceEquals(found,a),Is.True);
                Assert.That(registry.TryGet("target_b",out found)&&ReferenceEquals(found,b),Is.True);
                Assert.That(registry.TryRegister(duplicate,out var failure),Is.False);Assert.That(failure,Is.EqualTo(RegistrationFailure.DuplicateId));
                Assert.That(registry.Unregister(duplicate),Is.False);Assert.That(registry.TryGet("target_a",out found)&&ReferenceEquals(found,a),Is.True);
                Assert.That(registry.TryGet("Target_A",out _)||registry.TryGet("target_",out _),Is.False);
                Assert.That(registry.Unregister(a),Is.True);Assert.That(registry.TryGet("target_a",out _),Is.False);
            }
            finally{Object.DestroyImmediate(a.gameObject);Object.DestroyImmediate(b.gameObject);Object.DestroyImmediate(duplicate.gameObject);}
        }
        [Test]public void W03_02_InvalidIdsAndNullTargetsCannotEnterRegistry()
        {
            foreach(var id in new[]{null,""," ","Target_a","a-b","a b","1abc","目标","a\n",new string('a',65)})Assert.That(TargetRegistry.IsValidId(id),Is.False,id);
            Assert.That(TargetRegistry.IsValidId("a")&&TargetRegistry.IsValidId(new string('a',64)),Is.True);
            Assert.That(new TargetRegistry().TryRegister(null,out var failure),Is.False);Assert.That(failure,Is.EqualTo(RegistrationFailure.MissingTarget));
            var a=Create("valid_id");try{Assert.Throws<System.ArgumentException>(()=>a.Configure("bad id","测试"));Assert.That(a.Id,Is.EqualTo("valid_id"));}finally{Object.DestroyImmediate(a.gameObject);}
        }
        [UnityTest]public IEnumerator W03_03_ActualEnableDisableDestroyUpdatesRegistry()
        {
            yield return new EnterPlayMode();var a=Create("lifecycle_id");
            Assert.That(WorldTarget.Active.TryGet(a.Id,out _),Is.False);a.gameObject.SetActive(true);
            Assert.That(a.Registered&&WorldTarget.Active.TryGet(a.Id,out var found)&&ReferenceEquals(found,a),Is.True);
            Assert.Throws<System.InvalidOperationException>(()=>a.Configure("other_id","改名"));a.enabled=false;
            Assert.That(WorldTarget.Active.TryGet(a.Id,out _),Is.False);a.enabled=true;Assert.That(WorldTarget.Active.TryGet(a.Id,out _),Is.True);
            Object.Destroy(a.gameObject);yield return null;Assert.That(WorldTarget.Active.TryGet("lifecycle_id",out _),Is.False);yield return new ExitPlayMode();
        }
        [Test]public void W03_04_CabinSavedTargetsAreValidDistinctAndDisplayNamesIndependent()
        {
            var scene=EditorSceneManager.OpenPreviewScene(CabinGreyboxSetup.ScenePath);
            try
            {
                var targets=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<WorldTarget>()).ToArray();Assert.That(targets.Length,Is.EqualTo(5));
                Assert.That(targets.Select(t=>t.Id).Distinct().Count(),Is.EqualTo(5));Assert.That(targets.All(t=>TargetRegistry.IsValidId(t.Id)&&!string.IsNullOrWhiteSpace(t.DisplayName)),Is.True);
                foreach(var id in new[]{"note_maintenance_01","note_roster_01","terminal_data_01","scan_mark_01","power_panel_01"})Assert.That(targets.Any(t=>t.Id==id),Is.True);
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        [UnityTearDown]public IEnumerator LeaveRuntime(){if(Application.isPlaying)yield return new ExitPlayMode();}
    }
}
