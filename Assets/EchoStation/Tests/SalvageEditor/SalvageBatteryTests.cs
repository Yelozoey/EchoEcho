using System;
using System.Collections;
using System.Linq;
using EchoStation.Salvage.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Salvage.Tests
{
    public class SalvageBatteryTests
    {
        Scene previous,loaded;bool background;
        [Test]public void DrainScanPauseAndChargeMatchRules(){var b=new EchoBattery();b.Step(10,false,false,false);Assert.That(b.Charge,Is.EqualTo(99.5f).Within(.001));b.Step(10,true,false,false);Assert.That(b.Charge,Is.EqualTo(97.5f).Within(.001));Assert.That(b.SpendSuccessfulScan(),Is.True);b.Step(100,true,false,true);Assert.That(b.Charge,Is.EqualTo(89.5f).Within(.001));b.Step(1,true,true,false);Assert.That(b.Charge,Is.EqualTo(100));}
        [Test]public void EmptyBatteryRecoversAndWarnsOnlyOnce(){var b=new EchoBattery();b.Step(600,true,false,false);Assert.That(b.Charge,Is.Zero);Assert.That(b.FollowSpeed,Is.EqualTo(1.5f));Assert.That(b.SpendSuccessfulScan(),Is.False);Assert.That(b.LowWarningCount,Is.EqualTo(1));b.Step(5,false,true,false);Assert.That(b.Charge,Is.EqualTo(100));b.Step(600,true,false,false);Assert.That(b.LowWarningCount,Is.EqualTo(1));}
        [Test]public void ExactScanBoundaryAndInvalidSnapshotsAreGuarded(){var b=new EchoBattery();b.Restore(8,true);Assert.That(b.SpendSuccessfulScan(),Is.True);Assert.That(b.Charge,Is.Zero);Assert.Throws<ArgumentException>(()=>b.Restore(float.NaN,false));Assert.Throws<ArgumentException>(()=>b.Restore(10,false));Assert.Throws<ArgumentException>(()=>b.Step(-1,false,false,false));}
        [UnityTest]public IEnumerator ZeroChargeUsesRealNavPathAndCanChargeAtDock()
        {
                yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();
                loaded=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));
                while(!loaded.isLoaded)yield return null;SceneManager.SetActiveScene(loaded);yield return null;
                var roots=loaded.GetRootGameObjects();var echo=roots.SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();echo.enabled=false;var player=roots.SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();player.enabled=false;Assert.That(echo.Initialize(),Is.True);
                player.RestorePose(new Vector3(18,.1f,18),0,0);Assert.That(echo.Navigation.TryBind(new Vector3(8,0,12)),Is.True);echo.Battery.Restore(0,true);echo.Advance(.1f,false);Assert.That(echo.Navigation.Agent.speed,Is.EqualTo(1.5f));Assert.That(echo.Navigation.Agent.pathStatus,Is.EqualTo(NavMeshPathStatus.PathComplete));Assert.That(echo.Navigation.Goal.x,Is.GreaterThan(12));
                var start=echo.transform.position;var timer=System.Diagnostics.Stopwatch.StartNew();while(Vector3.Distance(start,echo.transform.position)<.3f){if(timer.Elapsed.TotalSeconds>6)Assert.Fail("Zero-charge robot never moved.");yield return null;}
                echo.Navigation.Cancel();
                Assert.That(echo.Navigation.TryBind(SalvageDelivery.Dock+Vector3.right),Is.True);echo.Advance(1,false);Assert.That(echo.Battery.Charge,Is.EqualTo(20).Within(.001));echo.Advance(1,true);Assert.That(echo.Battery.Charge,Is.EqualTo(20).Within(.001));
                TestContext.Out.WriteLine("Real zero-charge displacement="+Vector3.Distance(start,echo.transform.position));
        }
        [UnityTearDown]public IEnumerator CleanupPlay()
        {
            if(!Application.isPlaying)yield break;
            if(previous.IsValid())SceneManager.SetActiveScene(previous);if(loaded.IsValid()){var op=SceneManager.UnloadSceneAsync(loaded);while(op!=null&&!op.isDone)yield return null;}
            Application.runInBackground=background;yield return new ExitPlayMode();
        }
        [UnityTest]public IEnumerator ApproachingPlayerCanPassRobotWithoutPhasing()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();loaded=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var loading=System.Diagnostics.Stopwatch.StartNew();while(!loaded.isLoaded){if(loading.Elapsed.TotalSeconds>8)Assert.Fail("Scene load timeout");yield return null;}SceneManager.SetActiveScene(loaded);yield return null;
            var roots=loaded.GetRootGameObjects();var echo=roots.SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();var player=roots.SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();echo.enabled=false;player.enabled=false;player.RestorePose(new Vector3(8,.1f,13),180,0);echo.transform.position=new Vector3(8,0,10);Assert.That(echo.Initialize(),Is.True);Physics.SyncTransforms();
            var timer=System.Diagnostics.Stopwatch.StartNew();float side=0;while(player.transform.position.z>9.2f){if(timer.Elapsed.TotalSeconds>7)Assert.Fail("Robot blocks approaching player.");yield return null;var dt=Mathf.Min(Time.deltaTime,.05f);player.ApplyInput(Vector2.up,Vector2.zero,dt);echo.Advance(dt,false);side=Mathf.Max(side,Mathf.Abs(echo.transform.position.x-8));}
            Assert.That(side,Is.GreaterThan(.25f));Assert.That(echo.GetComponent<BoxCollider>().isTrigger,Is.False);TestContext.Out.WriteLine("Robot sidestep="+side+" player z="+player.transform.position.z);
        }
        [UnityTest]public IEnumerator TouchingPlayerCanPassRobotInDoorway()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();loaded=EditorSceneManager.LoadSceneInPlayMode(SalvageWorldSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var loading=System.Diagnostics.Stopwatch.StartNew();while(!loaded.isLoaded){if(loading.Elapsed.TotalSeconds>8)Assert.Fail("Scene load timeout");yield return null;}SceneManager.SetActiveScene(loaded);yield return null;
            var roots=loaded.GetRootGameObjects();var echo=roots.SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();var player=roots.SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();echo.enabled=false;player.enabled=false;player.RestorePose(new Vector3(14.16f,.1f,18),270,0);echo.transform.position=new Vector3(13.5f,0,18);Assert.That(echo.Initialize(),Is.True);Physics.SyncTransforms();
            var timer=System.Diagnostics.Stopwatch.StartNew();while(player.transform.position.x>10.5f){if(timer.Elapsed.TotalSeconds>8)Assert.Fail("Contact doorway remains blocked. Robot="+echo.transform.position);yield return null;var dt=Mathf.Min(Time.deltaTime,.05f);player.ApplyInput(Vector2.up,Vector2.zero,dt);echo.Advance(dt,false);}
            Assert.That(echo.GetComponent<BoxCollider>().isTrigger,Is.False);TestContext.Out.WriteLine("Doorway physical passage player="+player.transform.position+" robot="+echo.transform.position);
        }
    }
}
