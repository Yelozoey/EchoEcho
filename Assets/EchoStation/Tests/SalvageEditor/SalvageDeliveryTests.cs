using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Salvage.Tests
{
    public class SalvageDeliveryTests
    {
        Scene old,scene;SalvageMotor motor;SalvageCarry carry;SalvageDelivery delivery;
        [SetUp]public void Before(){old=SceneManager.GetActiveScene();scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();carry=motor.GetComponent<SalvageCarry>();delivery=motor.GetComponent<SalvageDelivery>();var input=motor.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);}
        [TearDown]public void After(){SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        void Aim(string id){var p=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CargoActor>()).Single(a=>a.Id==id).transform.position;motor.RestorePose(p+new Vector3(0,.1f,-2),0,35);Physics.SyncTransforms();}
        [TestCase("cargo_a",3.3f)][TestCase("cargo_b",3.1f)][TestCase("cargo_d",2.9f)][TestCase("cargo_e",2.7f)]
        public void WeightChangesRealMotorAndDropRestores(string id,float speed){Aim(id);Assert.That(carry.TryPickFromView(),Is.True,carry.LastMessage);Assert.That(motor.Speed,Is.EqualTo(speed).Within(.001));Assert.That(carry.TryDrop(),Is.True,carry.LastMessage);Assert.That(motor.Speed,Is.EqualTo(3.5f));}
        [Test]public void OverweightStaysInWorld(){Aim("cargo_heavy");Assert.That(carry.TryPickFromView(),Is.False);Assert.That(carry.HeldId,Is.Null);Assert.That(carry.Registry.Get("cargo_heavy").State,Is.EqualTo(CargoState.World));}
        [Test]public void CancelThenConfirmScoresExactlyOnce()
        {Aim("cargo_a");Assert.That(carry.TryPickFromView(),Is.True);Assert.That(delivery.Request(),Is.False);motor.RestorePose(new Vector3(5,.1f,12),0,0);Assert.That(delivery.Request(),Is.True);delivery.Cancel();Assert.That(carry.HeldId,Is.EqualTo("cargo_a"));Assert.That(delivery.Count,Is.Zero);Assert.That(delivery.Request(),Is.True);Assert.That(delivery.Confirm(),Is.True);Assert.That(delivery.Count,Is.EqualTo(1));Assert.That(delivery.Points,Is.EqualTo(2));Assert.That(delivery.Confirm(),Is.False);Assert.That(delivery.Points,Is.EqualTo(2));Assert.That(carry.Registry.Get("cargo_a").State,Is.EqualTo(CargoState.Delivered));Assert.That(motor.Speed,Is.EqualTo(3.5f));}
        [Test]public void MovingAwayInvalidatesConfirmation(){Aim("cargo_a");Assert.That(carry.TryPickFromView(),Is.True);motor.RestorePose(new Vector3(5,.1f,12),0,0);Assert.That(delivery.Request(),Is.True);motor.RestorePose(new Vector3(9,.1f,12),0,0);Assert.That(delivery.Confirm(),Is.False);Assert.That(carry.HeldId,Is.EqualTo("cargo_a"));Assert.That(delivery.Count,Is.Zero);}
    }
}
