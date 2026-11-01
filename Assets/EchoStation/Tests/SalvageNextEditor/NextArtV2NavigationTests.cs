using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Salvage;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextArtV2NavigationTests
    {
        Scene previous,scene;bool background;NextRoomStreaming stream;
        [UnityTest,Timeout(180000)]public IEnumerator ActualRoverCrossesEveryDoorWithStreamingActiveAndReturnsByShortcut()
        {
            yield return new EnterPlayMode();previous=SceneManager.GetActiveScene();background=Application.runInBackground;Application.runInBackground=true;scene=EditorSceneManager.LoadSceneInPlayMode(NextArtV2Stream.Main,new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;
            var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var motor=root.GetComponentInChildren<SalvageMotor>();stream=motor.GetComponent<NextRoomStreaming>();motor.enabled=false;motor.RestorePose(new Vector3(3,.1f,4),0,0);var echo=root.GetComponentInChildren<SalvageEcho>();echo.GetComponent<SalvageEchoCoordinator>().enabled=false;echo.enabled=false;Assert.That(echo.Navigation.TryBind(new Vector3(10,0,18)),Is.True);echo.Navigation.Agent.speed=3;var rows=new JArray();
            foreach(var goal in new[]{new Vector3(14,0,18),new Vector3(22,0,12),new Vector3(26,0,12),new Vector3(22,0,12),new Vector3(14,0,6),new Vector3(10,0,6)})
            {
                Assert.That(echo.Navigation.TryMove(goal),Is.True);var timer=System.Diagnostics.Stopwatch.StartNew();while(Vector3.Distance(Vector3.ProjectOnPlane(echo.transform.position,Vector3.up),goal)>.6f){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(25),"Actual doorway motion stalled");yield return null;}rows.Add(new JObject{{"goal",goal.ToString("F2")},{"seconds",timer.Elapsed.TotalSeconds}});
            }
            Assert.That(stream.Owner.Failures,Is.Zero,stream.Owner.LastError);File.WriteAllText(NextArtV2Stream.Evidence+"/ActualNavigation_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"steps",rows},{"streamingActive",true},{"failures",stream.Owner.Failures}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;if(!stream&&scene.IsValid()&&scene.isLoaded)stream=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<NextRoomStreaming>()).SingleOrDefault();var owner=stream?stream.Owner:null;if(previous.IsValid())SceneManager.SetActiveScene(previous);if(scene.IsValid()&&scene.isLoaded){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}var timer=System.Diagnostics.Stopwatch.StartNew();while(owner!=null&&!owner.IsClosed){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(15));yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();
        }
    }
}
