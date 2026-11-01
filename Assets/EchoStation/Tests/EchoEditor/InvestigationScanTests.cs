using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Echo
{
    public class InvestigationScanTests
    {
        private Scene scene,previous;private PhysicsScene physics;private bool background;private EchoNavigation motor;private EchoScanEngine scanner;private IntentBehaviorTree tree;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-E08";
        [Serializable]private class Row{public string id,tree,phase,failure,occluder,summary;public bool passed,newDiscovery;public float seconds;}
        [Serializable]private class Result{public bool passed,hiddenVisible;public int discoveries;public Row[] rows;}
        private IEnumerator Setup(Vector3 position)
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();
            scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));var timer=System.Diagnostics.Stopwatch.StartNew();
            while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Cabin load timed out.");yield return null;}SceneManager.SetActiveScene(scene);physics=scene.GetPhysicsScene();
            var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath));motor=actor.GetComponent<EchoNavigation>();Assert.That(motor.TryBind(position),Is.True);scanner=actor.GetComponent<EchoScanController>().Scanner;physics.Simulate(.02f);
            tree=new IntentBehaviorTree(motor,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Investigate,()=>new InvestigateScanNode(scanner)}});
        }
        [UnityTest]public IEnumerator E08_C01_ActualFiveTargetInvestigationScansRevealOnlyInformationAndDeduplicate()
        {
            yield return new EnterPlayMode();yield return Setup(new Vector3(1.5f,0,-2));var dispatcher=new IntentDispatcher(WorldTarget.Active,tree);var report=new Result();var rows=new List<Row>();scanner.Ledger.Discovered+=receipt=>report.discoveries++;Directory.CreateDirectory(Evidence);
            var originals=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<WorldTarget>()).ToDictionary(t=>t.Id,t=>t.transform.position);
            Assert.That(WorldTarget.Active.TryGet("scan_mark_01",out var hidden),Is.True);Assert.That(hidden.GetComponent<ClueSource>().TryRead(out _),Is.False);
            foreach(var id in new[]{"note_maintenance_01","terminal_data_01","note_roster_01","scan_mark_01","power_panel_01","scan_mark_01"})
            {
                var began=Time.time;Assert.That(dispatcher.TryDispatch(ScriptCommands.Investigate(id),out _),Is.True);var timer=System.Diagnostics.Stopwatch.StartNew();
                while(tree.Status==BehaviorTreeStatus.Running)
                {if(timer.Elapsed.TotalSeconds>20)Assert.Fail("Investigation scan timed out: "+id);tree.Tick(Time.deltaTime);yield return R04ShadowProbe.Frame();physics.Simulate(.02f);}
                var receipt=scanner.LastReceipt;var row=new Row{id=id,tree=tree.Status.ToString(),phase=scanner.Phase.ToString(),failure=(scanner.Failure!=ScanFailure.None?scanner.Failure:scanner.LastRequestFailure).ToString(),occluder=scanner.LastOccluderName,
                    seconds=Time.time-began,passed=tree.Status==BehaviorTreeStatus.Success&&receipt!=null&&receipt.TargetId==id,newDiscovery=receipt!=null&&receipt.IsNewDiscovery,summary=receipt?.Summary};
                rows.Add(row);report.rows=rows.ToArray();File.WriteAllText(Evidence+"/ActualScans.json",JsonUtility.ToJson(report,true));Assert.That(row.passed,Is.True,JsonUtility.ToJson(row,true));
            }
            report.hiddenVisible=hidden.GetComponent<ClueSource>().IsRevealed&&hidden.GetComponent<MeshRenderer>().enabled;report.passed=report.hiddenVisible&&report.discoveries==5&&rows.Last().newDiscovery==false;
            foreach(var target in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<WorldTarget>()))Assert.That(target.transform.position,Is.EqualTo(originals[target.Id]));
            File.WriteAllText(Evidence+"/ActualScans.json",JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True,JsonUtility.ToJson(report,true));
            var camera=scene.GetRootGameObjects().Single(o=>o.name=="Cabin_ValidationCamera").GetComponent<Camera>();camera.scene=scene;camera.aspect=16f/9f;camera.transform.position=new Vector3(5.7f,1.65f,-1.6f);camera.transform.LookAt(new Vector3(8.97f,1.4f,2.8f));yield return R04ShadowProbe.Frame();using var frames=new SceneFrameCapture(camera);frames.Capture(Evidence+"/HiddenMarkerRevealed.png");
        }
        [UnityTest]public IEnumerator E08_C02_CancellingBehaviorDuringScanCannotRevealHiddenMarker()
        {
            yield return new EnterPlayMode();yield return Setup(new Vector3(8.1f,0,2.1f));var dispatcher=new IntentDispatcher(WorldTarget.Active,tree);Assert.That(dispatcher.TryDispatch(ScriptCommands.Investigate("scan_mark_01"),out _),Is.True);var timer=System.Diagnostics.Stopwatch.StartNew();
            while(scanner.Phase!=ScanPhase.Scanning)
            {if(timer.Elapsed.TotalSeconds>8)Assert.Fail("Scan never reached scanning phase: "+scanner.LastRequestFailure+" / "+scanner.LastOccluderName);tree.Tick(Time.deltaTime);yield return R04ShadowProbe.Frame();physics.Simulate(.02f);}
            tree.Cancel();var start=Time.time;timer.Restart();while(Time.time-start<.8f){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Cancellation observation timed out.");yield return R04ShadowProbe.Frame();}
            Assert.That(WorldTarget.Active.TryGet("scan_mark_01",out var hidden),Is.True);Assert.That(hidden.GetComponent<ClueSource>().IsRevealed||scanner.Ledger.HasDiscovered(hidden.Id),Is.False);Assert.That(scanner.Phase,Is.EqualTo(ScanPhase.Cancelled));Assert.That(motor.State,Is.EqualTo(NavigationState.Idle));
        }
        [UnityTearDown]public IEnumerator After()
        {
            tree?.Dispose();tree=null;
            if(Application.isPlaying)
            {
                if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();
                while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Investigation scan fixture unload timed out.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();
            }
        }
    }
}
