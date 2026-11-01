using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Salvage.Tests
{
    public class SalvageMissionTests
    {
        Scene old,scene;SalvageMotor motor;SalvageCarry carry;SalvageDelivery delivery;SalvageMission mission;
        [SetUp]public void Before(){old=SceneManager.GetActiveScene();scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();carry=motor.GetComponent<SalvageCarry>();delivery=motor.GetComponent<SalvageDelivery>();mission=motor.GetComponent<SalvageMission>();var input=motor.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);}
        [TearDown]public void After(){SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        void Recover(string id){var p=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CargoActor>()).Single(a=>a.Id==id).transform.position;motor.RestorePose(p+new Vector3(0,.1f,-2),0,35);Physics.SyncTransforms();Assert.That(carry.TryPickFromView(),Is.True,carry.LastMessage);motor.RestorePose(new Vector3(5,.1f,12),0,0);Assert.That(delivery.Request(),Is.True);Assert.That(delivery.Confirm(),Is.True);}
        [TestCase("cargo_a","cargo_b","cargo_d")][TestCase("cargo_f","cargo_c","cargo_b")]
        public void TwoOrdersCompleteOnlyAfterExplicitConfirmation(string a,string b,string c)
        {Recover(a);Recover(b);Assert.That(mission.CanComplete,Is.False);mission.OpenBoard();Assert.That(mission.RequestCompletion(),Is.False);mission.CloseBoard();Recover(c);Assert.That(mission.CanComplete,Is.True);Assert.That(mission.Completed,Is.False);mission.OpenBoard();Assert.That(mission.RequestCompletion(),Is.True);Assert.That(mission.Completed,Is.False);Assert.That(mission.ConfirmCompletion(),Is.True);Assert.That(mission.Completed,Is.True);}
        [Test]public void ClosingBoardKeepsProgressAndRestartRestoresAllActors()
        {Recover("cargo_a");mission.OpenBoard();mission.CloseBoard();Assert.That(delivery.Count,Is.EqualTo(1));mission.OpenBoard();Assert.That(mission.RequestRestart(),Is.True);Assert.That(delivery.Count,Is.EqualTo(1));Assert.That(mission.ConfirmRestart(),Is.True);Assert.That(delivery.Count,Is.Zero);Assert.That(carry.Registry.Records.All(r=>r.State==CargoState.World),Is.True);Assert.That(motor.Speed,Is.EqualTo(3.5f));Assert.That(mission.Completed||mission.BoardOpen,Is.False);}
    }
}
