using System.Linq;
using EchoStation.Echo;
using EchoStation.Salvage.Editor;
using EchoStation.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Salvage.Tests
{
    public class R2TargetSceneTests
    {
        Scene old,scene;GameObject root;
        [SetUp]public void Before(){old=SceneManager.GetActiveScene();scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);root=scene.GetRootGameObjects().Single(r=>r.name=="ES_Salvage_World");}
        [TearDown]public void After(){SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        [Test]public void EightOwnedTargetsMatchCanonicalSensorPermissions()
        {var targets=root.GetComponentsInChildren<SalvageTargetView>();Assert.That(targets.Length,Is.EqualTo(8));Assert.That(targets.Select(t=>t.Id).Distinct().Count(),Is.EqualTo(8));foreach(var t in targets){Assert.That(t.World.Id,Is.EqualTo(t.Id));var p=t.GetComponent<EchoTargetPermissions>();Assert.That(p.ScanFact,Is.EqualTo(t.Definition.Summary));Assert.That(p.ApproachPoint.IsChildOf(t.transform),Is.True);Assert.That(t.Allows(IntentKind.Investigate),Is.True,t.Id);Assert.That(t.Allows(IntentKind.Warn),Is.EqualTo(t.Definition.Kind==SalvageTargetKind.Danger));}}
        [Test]public void ReadOnlyHazardsHaveNoStateChangingOrDamageComponents()
        {var targets=root.GetComponentsInChildren<SalvageTargetView>().Where(t=>t.Definition.Kind==SalvageTargetKind.Danger).ToArray();Assert.That(targets.Length,Is.EqualTo(2));foreach(var t in targets){Assert.That(t.GetComponentsInChildren<Collider>().Length,Is.EqualTo(1));Assert.That(t.GetComponentsInChildren<Rigidbody>(),Is.Empty);Assert.That(t.GetComponentsInChildren<MonoBehaviour>().Select(m=>m.GetType()).OrderBy(v=>v.Name),Is.EqualTo(new[]{typeof(WorldTarget),typeof(EchoTargetPermissions),typeof(SalvageTargetView)}.OrderBy(v=>v.Name)));Assert.That(t.GetComponentInChildren<Light>().color.r,Is.GreaterThan(t.GetComponentInChildren<Light>().color.b));}}
        [Test]public void ExistingCargoAndMissionRemainUnchanged()
        {var carry=root.GetComponentInChildren<SalvageCarry>();Assert.That(carry.Registry.Count,Is.EqualTo(7));Assert.That(carry.Registry.Records.All(r=>r.State==CargoState.World),Is.True);Assert.That(root.GetComponentInChildren<SalvageDelivery>().Count,Is.Zero);foreach(var actor in root.GetComponentsInChildren<CargoActor>()){var index=SalvageCatalog.Items.ToList().FindIndex(d=>d.Id==actor.Id);Assert.That(Vector3.Distance(actor.transform.position,SalvageCargoSetup.Positions[index]),Is.LessThan(.001));}Assert.That(root.GetComponentInChildren<SalvageMission>().CanComplete,Is.False);}
    }
}
