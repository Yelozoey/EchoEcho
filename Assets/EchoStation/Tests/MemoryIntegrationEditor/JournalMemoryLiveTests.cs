using System;
using System.Collections;
using System.IO;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
using EchoStation.Memory;
using EchoStation.Memory.Integration;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Player;
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
using Object = UnityEngine.Object;

namespace EchoStation.Tests.MemoryIntegration
{
    public sealed class JournalMemoryLiveTests
    {
        private Scene scene, previous;
        private bool background;
        private SyntheticInputScope inputs;
        private PlayerJournal journal;
        private PlayerInteraction interaction;
        private Camera view;
        private MemoryStore memory;
        private JournalMemoryBridge bridge;
        private const string Evidence = "Docs/Tests/Stage2/Evidence/S2-M04";

        private IEnumerator Setup()
        {
            background = Application.runInBackground; Application.runInBackground = true; previous = SceneManager.GetActiveScene();
            scene = EditorSceneManager.LoadSceneInPlayMode(CabinGreyboxSetup.ScenePath, new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D));
            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (!scene.isLoaded) { if (timer.Elapsed.TotalSeconds > 5) Assert.Fail("Memory fixture cabin load timed out."); yield return null; }
            SceneManager.SetActiveScene(scene); inputs = new SyntheticInputScope();
            var actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));
            actor.GetComponent<FirstPersonMotor>().enabled = false; actor.GetComponent<CharacterController>().enabled = false;
            actor.transform.position = new Vector3(0, .05f, -6); view = actor.GetComponentInChildren<Camera>(); view.enabled = false;
            var input = actor.GetComponent<PlayerInputReader>(); input.SetApplicationFocus(true); input.SetPointerCaptured(true);
            journal = actor.GetComponent<PlayerJournal>(); interaction = actor.GetComponent<PlayerInteraction>();
            memory = new MemoryStore(); bridge = new JournalMemoryBridge(journal.Store, memory);
            scene.GetPhysicsScene().Simulate(.02f); Directory.CreateDirectory(Evidence);
        }
        private void Aim(string id, Vector3 position)
        {
            journal.transform.position = position; Assert.That(WorldTarget.Active.TryGet(id, out var target), Is.True);
            view.transform.LookAt(target.transform.position); scene.GetPhysicsScene().Simulate(.02f); interaction.RefreshSelection();
            Assert.That(interaction.Selection.Target, Is.SameAs(target));
        }
        private IEnumerator PressE()
        {
            InputSystem.QueueStateEvent(inputs.Keyboard, new KeyboardState()); yield return R04ShadowProbe.Frame();
            InputSystem.QueueStateEvent(inputs.Keyboard, new KeyboardState(Key.E)); yield return R04ShadowProbe.Frame();
        }
        private EchoScanController EchoAt(Vector3 point)
        {
            var robot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath));
            Assert.That(robot.GetComponent<EchoNavigation>().TryBind(point), Is.True); scene.GetPhysicsScene().Simulate(.02f);
            return robot.GetComponent<EchoScanController>();
        }
        private IEnumerator Scan(EchoScanController source, string id)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            while (source.Scanner.CooldownRemaining > 0) { if (timer.Elapsed.TotalSeconds > 5) Assert.Fail("Scan cooldown stalled."); yield return R04ShadowProbe.Frame(); }
            Assert.That(WorldTarget.Active.TryGet(id, out var target), Is.True); Assert.That(source.Scanner.TryBegin(target), Is.True);
            timer.Restart();
            while (source.Scanner.IsBusy) { if (timer.Elapsed.TotalSeconds > 5) Assert.Fail("Actual memory scan timed out."); scene.GetPhysicsScene().Simulate(.02f); yield return R04ShadowProbe.Frame(); }
            Assert.That(source.Scanner.Phase, Is.EqualTo(ScanPhase.Completed), source.Scanner.Failure.ToString());
        }
        [UnityTest] public IEnumerator M04_B01_ActualEArchivesOnlySummaryAndRepeatedReadIsIdempotent()
        {
            yield return new EnterPlayMode(); yield return Setup();
            var changes = 0; memory.Changed += _ => changes++;
            Aim("note_maintenance_01", new Vector3(-2.8f, .05f, .6f)); yield return PressE(); yield return PressE();
            Assert.That(bridge.TryGetKnownClue("note_maintenance_01", out var note), Is.True);
            Assert.That(journal.Store.TryGet(note.Id, out var original), Is.True); Assert.That(note.Summary, Is.EqualTo(original.Summary)); Assert.That(note.Summary, Is.Not.EqualTo(original.FullText));
            Aim("terminal_data_01", new Vector3(-2.1f, .05f, .8f)); yield return PressE();
            Assert.That(memory.Clues.Count, Is.EqualTo(2)); Assert.That(memory.Clues[1].Kind, Is.EqualTo(MemoryClueKind.Data)); Assert.That(changes, Is.EqualTo(2));
            File.WriteAllText(Evidence + "/ActualE-Memory.json", MemoryJson.Encode(memory.CaptureSnapshot()));
        }
        [UnityTest] public IEnumerator M04_B02_CancelledHiddenScanStaysUnknownAndCompletedActualScanAddsFact()
        {
            yield return new EnterPlayMode(); yield return Setup();
            var source = EchoAt(new Vector3(8.1f, 0, 2.1f)); journal.BindEcho(source);
            Assert.That(WorldTarget.Active.TryGet("scan_mark_01", out var target), Is.True); Assert.That(target.GetComponent<ClueSource>().TryRead(out _), Is.False);
            Assert.That(bridge.TryGetKnownClue(target.Id, out _), Is.False); Assert.That(source.Scanner.TryBegin(target), Is.True); source.Scanner.Cancel();
            yield return R04ShadowProbe.Frame(); Assert.That(memory.Clues, Is.Empty); Assert.That(target.GetComponent<ClueSource>().IsRevealed, Is.False);
            yield return Scan(source, target.Id);
            Assert.That(bridge.TryGetKnownClue(target.Id, out var fact), Is.True); Assert.That(fact.Kind, Is.EqualTo(MemoryClueKind.Scan));
            Assert.That(target.GetComponent<ClueSource>().IsRevealed, Is.True); Assert.That(journal.Store.Entries.Count, Is.EqualTo(1));
            File.WriteAllText(Evidence + "/ActualHiddenScan-Memory.json", MemoryJson.Encode(memory.CaptureSnapshot()));
        }
        [UnityTest] public IEnumerator M04_B03_EquipmentScanIsReadOnlyAndRepeatedScansDoNotDuplicate()
        {
            yield return new EnterPlayMode(); yield return Setup();
            var source = EchoAt(new Vector3(3.8f, 0, -1.5f)); journal.BindEcho(source);
            Assert.That(WorldTarget.Active.TryGet("power_panel_01", out var panel), Is.True);
            var before = panel.transform.localToWorldMatrix; var active = panel.gameObject.activeSelf; var changes = 0; memory.Changed += _ => changes++;
            yield return Scan(source, panel.Id); yield return Scan(source, panel.Id); journal.BindEcho(source);
            Assert.That(memory.Clues.Count, Is.EqualTo(1)); Assert.That(changes, Is.EqualTo(1)); Assert.That(bridge.TryGetKnownClue(panel.Id, out var fact), Is.True);
            Assert.That(fact.Summary, Is.EqualTo(panel.GetComponent<EchoTargetPermissions>().ScanFact));
            Assert.That(panel.transform.localToWorldMatrix, Is.EqualTo(before)); Assert.That(panel.gameObject.activeSelf, Is.EqualTo(active)); Assert.That(GameRuntimeOptions.UseLLM, Is.False);
            File.WriteAllText(Evidence + "/ActualEquipmentScan-Memory.json", MemoryJson.Encode(memory.CaptureSnapshot()));
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            bridge?.Dispose(); bridge = null;
            if (inputs != null) { inputs.Dispose(); Assert.That(inputs.IsRestored, Is.True); inputs = null; }
            if (Application.isPlaying)
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                var operation = scene.IsValid() ? SceneManager.UnloadSceneAsync(scene) : null; var timer = System.Diagnostics.Stopwatch.StartNew();
                while (operation != null && !operation.isDone) { if (timer.Elapsed.TotalSeconds > 5) throw new TimeoutException("Memory fixture unload stalled."); yield return null; }
                Application.runInBackground = background; yield return new ExitPlayMode();
            }
        }
    }
}
