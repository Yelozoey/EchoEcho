using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.SalvageNext.Tests
{
    public sealed class RecoveryCaptureSceneTests
    {
        Scene old,scene;bool background;
        [UnityTest] public IEnumerator CaptureUsesRestoredSensorFactsAndRealNavMeshWithoutUnknownLocationRead()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;old=SceneManager.GetActiveScene();
            scene=EditorSceneManager.LoadSceneInPlayMode("Assets/EchoStation/Salvage/Scenes/ES_Salvage_Demo.unity",new LoadSceneParameters(LoadSceneMode.Additive));
            var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8));yield return null;}SceneManager.SetActiveScene(scene);yield return null;
            var hub=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageCommandHub>()).Single();hub.GetComponent<SalvageMotor>().enabled=false;
            var brain=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEchoCoordinator>()).Single();brain.enabled=false;brain.GetComponent<SalvageEcho>().enabled=false;Assert.That(brain.Initialize(),Is.True);brain.AutomaticWarnings=false;brain.ResetBase(IntentKind.Wait);
            var capture=new RecoveryKnowledgeCapture();Assert.That(capture.Capture(hub,1,RecoveryPreference.Nearby).Facts,Is.Empty);
            var persistence=hub.GetComponent<SalvagePersistence>();var data=persistence.Capture();data.Collaboration.Discovered=new[]{"cargo_a","cargo_b"};Assert.That(persistence.TryRestore(data,out var error),Is.True,error);
            var result=capture.Capture(hub,1,RecoveryPreference.HighScore);Assert.That(result.Facts.Count,Is.EqualTo(2));Assert.That(result.Facts.All(f=>f.PathMeters>0),Is.True);
            Assert.That(result.Suggestions(RecoveryPreference.HighScore)[0].Id,Is.EqualTo("cargo_b"));
            var unknown=hub.Targets.Single(t=>t.Id=="cargo_f");var prior=unknown.transform.position;unknown.transform.position=new Vector3(9999,0,9999);
            try{var unchanged=capture.Capture(hub,1,RecoveryPreference.HighScore);Assert.That(unchanged.Facts.Select(f=>f.Id),Is.EqualTo(new[]{"cargo_a","cargo_b"}));Assert.That(unchanged.Revision,Is.EqualTo(result.Revision));}finally{unknown.transform.position=prior;}
            Assert.That(capture.Capture(hub,1,RecoveryPreference.Lightweight).Revision,Is.GreaterThan(result.Revision));
            var task=Task.Run(()=>{try{capture.Capture(hub,1,RecoveryPreference.Nearby);return false;}catch(InvalidOperationException){return true;}});
            while(!task.IsCompleted)yield return null;Assert.That(task.Result,Is.True);Assert.That(hub.Known.Count,Is.EqualTo(2));
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);
            if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}
            Application.runInBackground=background;yield return new ExitPlayMode();
        }
    }
}
