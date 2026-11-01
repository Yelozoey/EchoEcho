using System;
using System.Collections;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.Salvage.Tests
{
    public class R2ObstacleTests
    {
        Scene old,scene;bool background;SalvageMotor motor;SalvageEcho echo;SalvageEchoCoordinator brain;SalvageCarry carry;
        IEnumerator Boot(){Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>8)Assert.Fail("Obstacle load timeout");yield return null;}SceneManager.SetActiveScene(scene);yield return null;motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;motor.GetComponent<PlayerInputReader>().SetApplicationFocus(true);motor.GetComponent<PlayerInputReader>().SetPointerCaptured(true);carry=motor.GetComponent<SalvageCarry>();echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;echo.enabled=false;echo.Navigation.Cancel();}
        IEnumerator Seconds(float seconds){float until=Time.time+seconds;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.time<until){if(timer.Elapsed.TotalSeconds>seconds+5)Assert.Fail("Obstacle time timeout");yield return null;}}
        [UnityTest]public IEnumerator AgentActuallyRoutesAroundPlayerAndWorldCargo()
        {
            yield return new EnterPlayMode();yield return Boot();motor.RestorePose(new Vector3(8,.1f,15),0,0);var actor=carry.Actor("cargo_heavy");actor.transform.position=new Vector3(8,0,12);carry.Registry.Get(actor.Id).Position=actor.transform.position;Physics.SyncTransforms();yield return Seconds(.5f);
            Assert.That(motor.GetComponent<NavMeshObstacle>().carving,Is.False);Assert.That(actor.GetComponent<NavMeshObstacle>().enabled,Is.True);Assert.That(echo.Navigation.TryBind(new Vector3(8,0,9)),Is.True);Assert.That(echo.Navigation.TryMove(new Vector3(8,0,18)),Is.True);float side=0;var timer=System.Diagnostics.Stopwatch.StartNew();var previous=echo.transform.position;
            while(echo.transform.position.z<17.7f){if(timer.Elapsed.TotalSeconds>10)Assert.Fail("Dynamic route stalled "+echo.transform.position);yield return null;side=Mathf.Max(side,Mathf.Abs(echo.transform.position.x-8));Assert.That(Vector3.Distance(previous,echo.transform.position),Is.LessThan(.8f),"No teleport");Assert.That(Mathf.Abs(echo.transform.position.x-8)>.5f||Mathf.Abs(echo.transform.position.z-15)>.6f,Is.True,"No player penetration");previous=echo.transform.position;}
            Assert.That(side,Is.GreaterThan(.6f));Assert.That(echo.GetComponent<BoxCollider>().isTrigger,Is.False);TestContext.Out.WriteLine("Actual dynamic sidestep="+side);
        }
        [UnityTest]public IEnumerator HeldObstacleDisablesAndDroppedObstacleReturns()
        {
            yield return new EnterPlayMode();yield return Boot();motor.RestorePose(new Vector3(9,.1f,17),0,35);Physics.SyncTransforms();Assert.That(carry.TryPickFromView(),Is.True);var actor=carry.Actor("cargo_a");actor.GetComponent<SalvageCargoObstacle>().Refresh();Assert.That(actor.GetComponent<NavMeshObstacle>().enabled,Is.False);motor.RestorePose(new Vector3(9,.1f,15),0,0);Physics.SyncTransforms();Assert.That(carry.TryDrop(),Is.True);actor.GetComponent<SalvageCargoObstacle>().Refresh();Assert.That(actor.GetComponent<NavMeshObstacle>().enabled,Is.True);Assert.That(actor.Body.enabled,Is.True);Assert.That(carry.Registry.Get("cargo_a").State,Is.EqualTo(CargoState.World));
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
