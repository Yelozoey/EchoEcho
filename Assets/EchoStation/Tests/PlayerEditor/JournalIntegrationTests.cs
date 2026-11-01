using System;
using System.Collections;
using System.IO;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using EchoStation.World.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class JournalIntegrationTests
    {
        private Scene scene,previous;private bool background;private SyntheticInputScope inputs;private PlayerJournal journal;private PlayerInteraction interaction;private Camera view;
        private const string Evidence="Docs/Tests/Stage1/Evidence/S1-P05";
        [Serializable]private class Row{public string id,summary,fullText;public ClueKind kind;}
        [Serializable]private class Result{public bool passed;public int count;public Row[] entries;}
        private IEnumerator Setup()
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Cabin load timeout.");yield return null;}SceneManager.SetActiveScene(scene);inputs=new SyntheticInputScope();var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));actor.GetComponent<FirstPersonMotor>().enabled=false;actor.GetComponent<CharacterController>().enabled=false;actor.transform.position=new Vector3(0,.05f,-6);view=actor.GetComponentInChildren<Camera>();view.enabled=false;var input=actor.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);journal=actor.GetComponent<PlayerJournal>();interaction=actor.GetComponent<PlayerInteraction>();scene.GetPhysicsScene().Simulate(.02f);Directory.CreateDirectory(Evidence);
        }
        private void Aim(string id,Vector3 position)
        {
            journal.transform.position=position;Assert.That(WorldTarget.Active.TryGet(id,out var target),Is.True);view.transform.LookAt(target.transform.position);scene.GetPhysicsScene().Simulate(.02f);interaction.RefreshSelection();Assert.That(interaction.Selection.Target,Is.SameAs(target));
        }
        private IEnumerator PressE(){InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.E));yield return R04ShadowProbe.Frame();}
        private EchoScanController EchoAt(Vector3 position)
        {
            var echo=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath));Assert.That(echo.GetComponent<EchoNavigation>().TryBind(position),Is.True);scene.GetPhysicsScene().Simulate(.02f);return echo.GetComponent<EchoScanController>();
        }
        private IEnumerator Scan(EchoScanController source,string id)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();while(source.Scanner.CooldownRemaining>0){if(timer.Elapsed.TotalSeconds>4)Assert.Fail("Scan cooldown stalled.");yield return R04ShadowProbe.Frame();}Assert.That(WorldTarget.Active.TryGet(id,out var target),Is.True);Assert.That(source.Scanner.TryBegin(target),Is.True);timer.Restart();while(source.Scanner.IsBusy){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Actual scanner did not finish.");scene.GetPhysicsScene().Simulate(.02f);yield return R04ShadowProbe.Frame();}Assert.That(source.Scanner.Phase,Is.EqualTo(ScanPhase.Completed),source.Scanner.Failure.ToString());
        }
        private void Record(string name)
        {
            var result=new Result{count=journal.Store.Entries.Count,passed=true,entries=new Row[journal.Store.Entries.Count]};for(var i=0;i<result.entries.Length;i++){var e=journal.Store.Entries[i];result.entries[i]=new Row{id=e.Id,summary=e.Summary,fullText=e.FullText,kind=e.Kind};}File.WriteAllText(Evidence+"/"+name+".json",JsonUtility.ToJson(result,true));
        }
        [UnityTest]public IEnumerator P05_B01_ActualCabinEAutomaticallyArchivesNoteAndDataAndDeduplicates()
        {
            yield return new EnterPlayMode();yield return Setup();Aim("note_maintenance_01",new Vector3(-2.8f,.05f,.6f));yield return PressE();yield return PressE();Assert.That(journal.Store.Entries.Count,Is.EqualTo(1));Assert.That(journal.Store.Entries[0].Kind,Is.EqualTo(ClueKind.Note));Aim("terminal_data_01",new Vector3(-2.1f,.05f,.8f));yield return PressE();Assert.That(journal.Store.Entries.Count,Is.EqualTo(2));Assert.That(journal.Store.Entries[1].Kind,Is.EqualTo(ClueKind.Data));Assert.That(journal.Store.Entries[0].FullText,Is.Not.EqualTo(journal.Store.Entries[0].Summary));Record("ActualEJournal");
        }
        [UnityTest]public IEnumerator P05_B02_ActualHiddenAndEquipmentScansArchiveAuthorizedResultsAndRepeatDoesNotDuplicate()
        {
            yield return new EnterPlayMode();yield return Setup();Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(WorldTarget.Active.TryGet("scan_mark_01",out var hidden),Is.True);Assert.That(hidden.GetComponent<ClueSource>().TryRead(out _),Is.False);var source=EchoAt(new Vector3(8.1f,0,2.1f));journal.BindEcho(source);var added=0;journal.Store.Added+=_=>added++;yield return Scan(source,hidden.Id);Assert.That(journal.Store.TryGet(hidden.Id,out var entry),Is.True);Assert.That(entry.Kind,Is.EqualTo(ClueKind.Scan));Assert.That(hidden.GetComponent<ClueSource>().IsRevealed,Is.True);
            Assert.That(source.GetComponent<EchoNavigation>().TryBind(new Vector3(3.8f,0,-1.5f)),Is.True);scene.GetPhysicsScene().Simulate(.02f);Assert.That(WorldTarget.Active.TryGet("power_panel_01",out var panel),Is.True);var initial=panel.transform.position;yield return Scan(source,panel.Id);Assert.That(journal.Store.TryGet(panel.Id,out var scan),Is.True);Assert.That(scan.Summary,Is.EqualTo(panel.GetComponent<EchoTargetPermissions>().ScanFact));Assert.That(scan.FullText,Is.Not.EqualTo(scan.Summary));yield return Scan(source,panel.Id);journal.BindEcho(source);Assert.That(journal.Store.Entries.Count,Is.EqualTo(2));Assert.That(added,Is.EqualTo(2));Assert.That(panel.transform.position,Is.EqualTo(initial));Record("ActualScanJournal");
        }
        [UnityTest]public IEnumerator P05_B03_CancelledScanDoesNotRevealOrArchiveHiddenMarker()
        {
            yield return new EnterPlayMode();yield return Setup();var source=EchoAt(new Vector3(8.1f,0,2.1f));journal.BindEcho(source);Assert.That(WorldTarget.Active.TryGet("scan_mark_01",out var target),Is.True);Assert.That(source.Scanner.TryBegin(target),Is.True);source.Scanner.Cancel();var start=Time.time;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.time-start<.8f){if(timer.Elapsed.TotalSeconds>4)Assert.Fail("Cancelled observation stalled.");yield return R04ShadowProbe.Frame();}Assert.That(journal.Store.Entries,Is.Empty);Assert.That(target.GetComponent<ClueSource>().IsRevealed,Is.False);
        }
        [UnityTest]public IEnumerator P05_B04_DisabledCollectorUnsubscribesAndReenableReplaysOnlyAuthorizedScanBacklog()
        {
            yield return new EnterPlayMode();yield return Setup();var source=EchoAt(new Vector3(8.1f,0,2.1f));journal.BindEcho(source);journal.enabled=false;yield return Scan(source,"scan_mark_01");Assert.That(journal.Store.Entries,Is.Empty);journal.enabled=true;Assert.That(journal.Store.Entries.Count,Is.EqualTo(1));journal.BindEcho(source);Assert.That(journal.Store.Entries.Count,Is.EqualTo(1));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Journal integration fixture unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
