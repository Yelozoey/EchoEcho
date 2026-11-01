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
    public class InvestigationMovementTests
    {
        private Scene scene,previous;private bool background;private IntentBehaviorTree tree;private EchoNavigation motor;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-E07";
        [Serializable]private class Arrival{public string id;public Vector3 position,approvedPoint;public float error;}
        [Serializable]private class Result{public bool passed,hiddenStillLocked;public int checkedFrames;public float maximumPenetration;public Arrival[] arrivals;}
        [UnityTest]public IEnumerator E07_B01_ActualRobotInvestigatesFiveTargetsWithoutRevealingScanOrChangingMechanisms()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();
            scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));var loading=System.Diagnostics.Stopwatch.StartNew();
            while(!scene.isLoaded){if(loading.Elapsed.TotalSeconds>5)Assert.Fail("Cabin load timed out.");yield return null;}SceneManager.SetActiveScene(scene);
            var cabin=scene.GetRootGameObjects().Single(o=>o.name=="ES_Cabin_Greybox");var solids=cabin.GetComponentsInChildren<BoxCollider>().Where(c=>!c.isTrigger).ToArray();var originalPositions=cabin.GetComponentsInChildren<WorldTarget>().ToDictionary(t=>t.Id,t=>t.transform.position);
            motor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath)).GetComponent<EchoNavigation>();Assert.That(motor.TryBind(new Vector3(1.5f,0,-2)),Is.True);var box=motor.GetComponent<BoxCollider>();
            var report=new Result();var arrivals=new List<Arrival>();tree=new IntentBehaviorTree(motor,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Investigate,()=>new InvestigationNode(target=>{
                var point=target.GetComponent<EchoTargetPermissions>().ApproachPoint.position;var position=motor.Position;var delta=position-point;delta.y=0;arrivals.Add(new Arrival{id=target.Id,position=position,approvedPoint=point,error=delta.magnitude});})}});
            var dispatcher=new IntentDispatcher(WorldTarget.Active,tree);Directory.CreateDirectory(Evidence);
            foreach(var id in new[]{"note_maintenance_01","terminal_data_01","note_roster_01","scan_mark_01","power_panel_01"})
            {
                var count=arrivals.Count;Assert.That(dispatcher.TryDispatch(ScriptCommands.Investigate(id),out _),Is.True);var timer=System.Diagnostics.Stopwatch.StartNew();
                while(tree.Status==BehaviorTreeStatus.Running)
                {
                    if(timer.Elapsed.TotalSeconds>15)Assert.Fail("Investigation timed out: "+id);tree.Tick(Time.deltaTime);yield return R04ShadowProbe.Frame();report.checkedFrames++;
                    foreach(var solid in solids)if(Physics.ComputePenetration(box,box.transform.position,box.transform.rotation,solid,solid.transform.position,solid.transform.rotation,out _,out var depth))report.maximumPenetration=Mathf.Max(report.maximumPenetration,depth);
                }
                Assert.That(tree.Status,Is.EqualTo(BehaviorTreeStatus.Success),id+": "+tree.LastError);Assert.That(arrivals.Count,Is.EqualTo(count+1));Assert.That(arrivals.Last().id,Is.EqualTo(id));Assert.That(arrivals.Last().error,Is.LessThanOrEqualTo(.2f));
            }
            Assert.That(WorldTarget.Active.TryGet("scan_mark_01",out var hidden),Is.True);var source=hidden.GetComponent<ClueSource>();report.hiddenStillLocked=!source.IsRevealed&&!source.TryRead(out _);
            foreach(var target in cabin.GetComponentsInChildren<WorldTarget>())Assert.That(target.transform.position,Is.EqualTo(originalPositions[target.Id]));
            var oldGoal=motor.Goal;Assert.That(dispatcher.TryDispatch(ScriptCommands.Investigate("unknown_target"),out _),Is.False);Assert.That(motor.Goal,Is.EqualTo(oldGoal));
            report.arrivals=arrivals.ToArray();report.passed=report.hiddenStillLocked&&report.maximumPenetration<.015f&&arrivals.Count==5;File.WriteAllText(Evidence+"/ActualInvestigations.json",JsonUtility.ToJson(report,true));Assert.That(report.passed,Is.True,JsonUtility.ToJson(report,true));
        }
        [UnityTearDown]public IEnumerator After()
        {
            tree?.Dispose();tree=null;
            if(Application.isPlaying)
            {
                if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();
                while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Investigation fixture unload timed out.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();
            }
        }
    }
}
