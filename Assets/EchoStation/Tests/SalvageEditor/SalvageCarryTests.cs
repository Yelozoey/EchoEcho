using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EchoStation.Salvage.Tests
{
    public class SalvageCarryTests
    {
        Scene old,scene;SalvageCarry carry;SalvageMotor motor;PlayerInputReader input;CargoActor actor;GameObject obstacle;
        [SetUp]public void Before()
        {
            old=SceneManager.GetActiveScene();scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();carry=motor.GetComponent<SalvageCarry>();input=motor.GetComponent<PlayerInputReader>();
            input.SetApplicationFocus(true);input.SetPointerCaptured(true);actor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CargoActor>()).Single(a=>a.Id=="cargo_a");
            motor.RestorePose(new Vector3(9,.1f,17),0,35);Physics.SyncTransforms();
        }
        [TearDown]public void After(){if(obstacle)Object.DestroyImmediate(obstacle);SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        [Test]public void PickOnceHoldAndDropUsesActualColliders()
        {
            Assert.That(carry.TryPickFromView(),Is.True,carry.LastMessage);Assert.That(carry.TryPickFromView(),Is.False);Assert.That(carry.HeldId,Is.EqualTo("cargo_a"));Assert.That(actor.Body.enabled,Is.False);
            Assert.That(carry.Registry.Get("cargo_a").State,Is.EqualTo(CargoState.Held));Assert.That(actor.transform.IsChildOf(motor.View),Is.True);
            Assert.That(carry.TryDrop(),Is.True,carry.LastMessage);Assert.That(carry.HeldId,Is.Null);Assert.That(actor.Body.enabled,Is.True);Assert.That(carry.Registry.Get("cargo_a").Position,Is.EqualTo(actor.transform.position));
        }
        [Test]public void MenuBlocksPickAndDropWithoutMutatingState()
        {input.SetUiBlocked(true);Assert.That(carry.TryPickFromView(),Is.False);Assert.That(carry.HeldId,Is.Null);input.SetUiBlocked(false);input.SetPointerCaptured(true);Assert.That(carry.TryPickFromView(),Is.True);input.SetUiBlocked(true);Assert.That(carry.TryDrop(),Is.False);Assert.That(carry.HeldId,Is.EqualTo("cargo_a"));}
        [Test]public void WallOcclusionPreventsPicking()
        {obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=new Vector3(9,1,18);obstacle.transform.localScale=new Vector3(2,2,.2f);Physics.SyncTransforms();Assert.That(carry.TryPickFromView(),Is.False);Assert.That(carry.HeldId,Is.Null);}
        [Test]public void OutOfReachCannotPick(){motor.RestorePose(new Vector3(9,.1f,14),0,15);Physics.SyncTransforms();Assert.That(carry.TryPickFromView(),Is.False);}
        [Test]public void NoGroundDropPreservesHeldItem()
        {Assert.That(carry.TryPickFromView(),Is.True);motor.RestorePose(new Vector3(9,8,17),0,0);Physics.SyncTransforms();Assert.That(carry.TryDrop(),Is.False);Assert.That(carry.HeldId,Is.EqualTo("cargo_a"));Assert.That(actor.Body.enabled,Is.False);}
        [Test]public void OccupiedDropPreservesHeldItem()
        {Assert.That(carry.TryPickFromView(),Is.True);obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);obstacle.transform.position=new Vector3(9,.25f,18.15f);obstacle.transform.localScale=Vector3.one*.5f;Physics.SyncTransforms();Assert.That(carry.TryDrop(),Is.False);Assert.That(carry.HeldId,Is.EqualTo("cargo_a"));}
    }
}
