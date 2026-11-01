using System;
using System.Collections;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.World;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class SaveRestoreValidationTests
    {
        private Scene scene,previous;private bool background;
        public static SavePayload CabinData()=>new SavePayload{sceneId="cabin_s1",playerPosition=new Vector3(0,.05f,-6),echoPosition=new Vector3(1.5f,0,-2),echoMode="wait",journalIds=Array.Empty<string>(),scanIds=Array.Empty<string>()};
        [UnityTest]public IEnumerator P08_A01_ActualCabinRejectsUnknownUnscannedAndBlockedPosesWithoutMutation()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Load timeout.");yield return null;}SceneManager.SetActiveScene(scene);var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));actor.transform.position=CabinData().playerPosition;actor.GetComponent<FirstPersonMotor>().enabled=false;actor.GetComponentInChildren<Camera>().enabled=false;var robot=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath));var navigation=robot.GetComponent<EchoNavigation>();Assert.That(navigation.TryBind(CabinData().echoPosition),Is.True);var brain=robot.GetComponent<EchoBrain>();Assert.That(brain.Initialize(actor.transform,IntentKind.Wait),Is.True);brain.AutomaticWarningsEnabled=false;var adapter=actor.AddComponent<SaveGameAdapter>();adapter.Configure(brain);scene.GetPhysicsScene().Simulate(.02f);Assert.That(adapter.CanRestore(CabinData(),out var error),Is.True,error);
            Action<SavePayload>[] mutations={p=>p.journalIds=new[]{"unknown_clue"},p=>p.journalIds=new[]{"scan_mark_01"},p=>p.scanIds=new[]{"scan_mark_01"},p=>{p.journalIds=new[]{"power_panel_01"};},p=>p.playerPosition=new Vector3(30,.05f,0),p=>p.playerPosition=new Vector3(-5,.05f,0),p=>p.echoPosition=new Vector3(30,0,0),p=>p.echoPosition=p.playerPosition};foreach(var mutate in mutations){var data=CabinData();mutate(data);Assert.That(adapter.CanRestore(data,out error),Is.False,"Must reject invalid fixture");Assert.That(actor.transform.position,Is.EqualTo(CabinData().playerPosition));Assert.That(actor.GetComponent<PlayerJournal>().Store.Entries,Is.Empty);}
            var authorized=CabinData();authorized.journalIds=new[]{"note_maintenance_01","scan_mark_01","power_panel_01"};authorized.scanIds=new[]{"scan_mark_01","power_panel_01"};Assert.That(adapter.CanRestore(authorized,out error),Is.True,error);Assert.That(WorldTarget.Active.TryGet("scan_mark_01",out var hidden),Is.True);Assert.That(hidden.GetComponent<ClueSource>().IsRevealed,Is.False,"Validation must not reveal");Assert.That(robot.GetComponent<EchoScanController>().Scanner.Ledger.GetDiscoveredIds(),Is.Empty);
        }
        [UnityTearDown]public IEnumerator After(){if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var op=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(op!=null&&!op.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}}
    }
}
