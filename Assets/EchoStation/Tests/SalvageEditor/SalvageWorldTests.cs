using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Salvage.Tests
{
    public class SalvageWorldTests
    {
        Scene original,scene;SalvageMotor motor;
        [SetUp]public void Before(){original=SceneManager.GetActiveScene();scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();Physics.SyncTransforms();}
        [TearDown]public void After(){SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);}
        [Test]public void SceneAndInputBaselineArePresent(){Assert.That(scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MeshFilter>()).Count(),Is.GreaterThan(60));Assert.That(motor.Speed,Is.EqualTo(3.5f));Assert.That(motor.GetComponent<PlayerInputReader>(),Is.Not.Null);Assert.That(motor.View.GetComponent<Camera>().allowHDR,Is.True);}
        void Walk(Vector3 point)
        {
            for(int i=0;i<600;i++){var delta=point-motor.transform.position;delta.y=0;if(delta.magnitude<.12f)return;motor.ApplyInput(new Vector2(delta.x,delta.z).normalized,Vector2.zero,.02f);}
            Assert.Fail("Route blocked toward "+point+" at "+motor.transform.position);
        }
        [TestCase(false)][TestCase(true)]public void RealControllerTraversesMainAndShortcut(bool shortcut)
        {float z=shortcut?6:18;motor.RestorePose(new Vector3(6,.1f,z),0,0);Walk(new Vector3(14.5f,0,z));float row=shortcut?6:15;Walk(new Vector3(14.5f,0,row));Walk(new Vector3(21,0,row));Walk(new Vector3(21,0,12));Walk(new Vector3(26,0,12));Walk(new Vector3(26,0,9));Walk(new Vector3(30,0,9));Assert.That(motor.transform.position.x,Is.GreaterThan(29.8f));}
        [Test]public void ControllerWallJumpAndLookAreReal()
        {
            motor.RestorePose(new Vector3(11,.1f,12),0,0);for(int i=0;i<100;i++)motor.ApplyInput(Vector2.right,Vector2.zero,.02f);Assert.That(motor.transform.position.x,Is.LessThan(11.6f));
            motor.RestorePose(new Vector3(6,.1f,10),0,0);for(int i=0;i<40;i++)motor.ApplyInput(Vector2.zero,Vector2.zero,.02f);
            float y=motor.transform.position.y;motor.ApplyInput(Vector2.zero,Vector2.zero,.02f,true);for(int i=0;i<12;i++)motor.ApplyInput(Vector2.zero,Vector2.zero,.02f);Assert.That(motor.transform.position.y-y,Is.GreaterThan(.35f));
            motor.ApplyInput(Vector2.zero,new Vector2(90,100),0);Assert.That(motor.View.localEulerAngles.x,Is.GreaterThan(340));
        }
        [Test]public void FlashlightSwitchesActualLight(){var torch=motor.GetComponent<PlayerFlashlight>();torch.SetOn(true);Assert.That(torch.Source.enabled,Is.True);torch.SetOn(false);Assert.That(torch.Source.enabled,Is.False);}
    }
}
