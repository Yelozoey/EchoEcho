using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using EchoStation.Dialogue;
using EchoStation.Dialogue.Commands;
using EchoStation.Dialogue.Integration;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Memory.Integration;
using EchoStation.Player;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Player;
using EchoStation.World;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.Tests.DialogueExecution
{
    public sealed class DialogueExecutionTests
    {
        private Scene scene, previous;
        private SyntheticInputScope inputs;
        private bool background;
        private CabinDemo demo;
        private DialoguePanel panel;
        private ScriptDialogueController controller;
        private MemorySession memories;
        private JournalMemoryBridge knowledge;
        private IEnumerator Setup()
        {
            background = Application.runInBackground; Application.runInBackground = true; previous = SceneManager.GetActiveScene(); inputs = new SyntheticInputScope();
            scene = EditorSceneManager.LoadSceneInPlayMode("Assets/EchoStation/World/Scenes/ES_Cabin_Demo.unity", new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D));
            var timer = System.Diagnostics.Stopwatch.StartNew(); while (!scene.isLoaded) { if (timer.Elapsed.TotalSeconds > 5) Assert.Fail("Execution fixture load timeout."); yield return null; }
            SceneManager.SetActiveScene(scene); demo = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CabinDemo>()).Single();
            yield return Until(() => demo.Ready, 5, "Demo startup timeout"); demo.Echo.AutomaticWarningsEnabled = false; demo.Echo.TrySubmit(ScriptCommands.Wait(), out _);
            var input = demo.GetComponent<PlayerInputReader>(); input.SetApplicationFocus(true); input.SetPointerCaptured(true);
            panel = demo.gameObject.AddComponent<DialoguePanel>(); memories = new MemorySession(); knowledge = new JournalMemoryBridge(demo.GetComponent<PlayerJournal>().Store, memories.Store);
            controller = demo.gameObject.AddComponent<ScriptDialogueController>();
            controller.Configure(panel, demo.Echo, memories, knowledge, () => new[] {
                new ScriptTarget("note_maintenance_01", "主室维护记录", true, false, "线索"),
                new ScriptTarget("scan_mark_01", "储藏室可疑点", true, false, "线索"),
                new ScriptTarget("power_panel_01", "供电面板", true, true) }, gameClock: () => Time.time);
            Assert.That(panel.SetOpen(true), Is.True); yield return Until(() => panel.InputField.isFocused, 5, "Input focus timeout");
        }
        private static IEnumerator Until(Func<bool> condition, double seconds, string label)
        { var timer = System.Diagnostics.Stopwatch.StartNew(); while (!condition()) { if (timer.Elapsed.TotalSeconds > seconds) Assert.Fail(label); yield return R04ShadowProbe.Frame(); } }
        private IEnumerator Send(string message)
        {
            foreach (var c in message) InputSystem.QueueTextEvent(inputs.Keyboard, c); yield return R04ShadowProbe.Frame();
            InputSystem.QueueStateEvent(inputs.Keyboard, new KeyboardState(Key.Enter)); yield return R04ShadowProbe.Frame();
            InputSystem.QueueStateEvent(inputs.Keyboard, new KeyboardState()); yield return R04ShadowProbe.Frame();
        }
        private void MovePlayer(Vector3 point)
        {
            var collider = demo.GetComponent<CharacterController>(); collider.enabled = false; demo.transform.position = point; collider.enabled = true; scene.GetPhysicsScene().Simulate(.02f);
        }
        [UnityTest] public IEnumerator D04_T01_ActualTextFollowMovesAndWaitStopsWithSummaryMemoriesOnly()
        {
            yield return new EnterPlayMode(); yield return Setup(); yield return Send("等在这里"); Assert.That(demo.Echo.CurrentMode, Is.EqualTo(IntentKind.Wait));
            MovePlayer(new Vector3(0, .05f, 1)); var start = demo.Echo.transform.position; yield return Send("跟随我");
            yield return Until(() => Vector3.Distance(start, demo.Echo.transform.position) > .15f, 6, "Actual follow did not move.");
            yield return Send("原地待命"); Assert.That(demo.Echo.CurrentMode, Is.EqualTo(IntentKind.Wait)); var stopped = demo.Echo.transform.position; yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Vector3.Distance(stopped, demo.Echo.transform.position), Is.LessThan(.02f));
            Assert.That(memories.Store.Events.Count, Is.EqualTo(3)); Assert.That(memories.Store.Characters.Single(c => c.Kind == CharacterMemoryKind.Agreement).Summary, Does.Contain("待命"));
            yield return Send("任意信息USER_RAW_ONLY_42？"); Assert.That(MemoryJson.Encode(memories.Store.CaptureSnapshot()), Does.Not.Contain("USER_RAW_ONLY_42"));
        }
        [UnityTest] public IEnumerator D04_T02_ActualInvestigateReachesScansAndFeedsKnownSummary()
        {
            yield return new EnterPlayMode(); yield return Setup(); MovePlayer(new Vector3(7.7f, .05f, 1));
            Assert.That(WorldTarget.Active.TryGet("scan_mark_01", out var target), Is.True); Assert.That(target.GetComponent<ClueSource>().IsRevealed, Is.False);
            yield return Send("调查储藏室可疑点"); Assert.That(controller.LastDispatchedKind, Is.EqualTo(IntentKind.Investigate));
            yield return Until(() => demo.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered(target.Id), 18, "Text investigate did not finish actual scan.");
            Assert.That(target.GetComponent<ClueSource>().IsRevealed, Is.True); Assert.That(memories.Store.TryGetClue(target.Id, out var known), Is.True); Assert.That(known.Kind, Is.EqualTo(MemoryClueKind.Scan));
        }
        [UnityTest] public IEnumerator D04_T03_ActualWarningRequiresPerceptionAndStoresKnownDangerWithoutDeviceChange()
        {
            yield return new EnterPlayMode(); yield return Setup(); Assert.That(WorldTarget.Active.TryGet("power_panel_01", out var target), Is.True); var pose = target.transform.localToWorldMatrix;
            var navigation = demo.Echo.GetComponent<EchoNavigation>(); Assert.That(navigation.TryBind(new Vector3(-3, 0, -2)), Is.True); scene.GetPhysicsScene().Simulate(.02f);
            yield return Send("警告供电面板"); Assert.That(controller.LastRejection, Is.EqualTo(IntentRejection.ExecutorRejected)); Assert.That(demo.Echo.CurrentMode, Is.EqualTo(IntentKind.Wait));
            Assert.That(memories.Store.Characters.Any(c => c.Kind == CharacterMemoryKind.KnownDanger), Is.False);
            Assert.That(navigation.TryBind(new Vector3(3.8f, 0, -1.5f)), Is.True);
            var toward = target.transform.position - demo.Echo.transform.position; toward.y = 0; demo.Echo.transform.rotation = Quaternion.LookRotation(toward); scene.GetPhysicsScene().Simulate(.02f);
            yield return Send("附近有危险吗"); Assert.That(controller.LastDispatchedKind, Is.EqualTo(IntentKind.Warn)); Assert.That(demo.Echo.CurrentMode, Is.EqualTo(IntentKind.Warn));
            Assert.That(memories.Store.Characters.Any(c => c.Kind == CharacterMemoryKind.KnownDanger && c.TargetId == target.Id), Is.True); Assert.That(target.transform.localToWorldMatrix, Is.EqualTo(pose));
        }
        [UnityTest] public IEnumerator D04_T04_ClarificationDoesNotMoveAndMemoryEpochInvalidatesOldChoice()
        {
            yield return new EnterPlayMode(); yield return Setup(); yield return Send("调查线索"); Assert.That(controller.PendingAction, Is.EqualTo(IntentKind.Investigate)); Assert.That(demo.Echo.CurrentMode, Is.EqualTo(IntentKind.Wait));
            memories.Reset(); Assert.That(knowledge.Synchronize(memories.Store), Is.True); yield return Send("储藏室可疑点"); Assert.That(demo.Echo.CurrentMode, Is.EqualTo(IntentKind.Wait)); Assert.That(controller.PendingAction, Is.Null);
            yield return Send("等待"); yield return Send("调查线索"); panel.Bind(new DialogueSession()); yield return Send("储藏室可疑点");
            Assert.That(demo.Echo.CurrentMode, Is.EqualTo(IntentKind.Wait)); Assert.That(controller.PendingAction, Is.Null);
            yield return Send("调查线索"); yield return Send("主室维护记录"); Assert.That(controller.LastDispatchedKind, Is.EqualTo(IntentKind.Investigate));
            yield return Send("等待"); yield return Send("帮我开门"); Assert.That(demo.Echo.CurrentMode, Is.EqualTo(IntentKind.Wait)); Assert.That(panel.Session.Messages.Last().IsRefusal, Is.True);
            Assert.That(controller.LastMemoryError, Is.Empty);
        }
        [UnityTest] public IEnumerator D04_T05_InformationProviderCannotInjectActionAndDisableUnsubscribes()
        {
            yield return new EnterPlayMode(); yield return Setup(); controller.AnswerProvider = _ => ScriptCommands.Investigate("scan_mark_01");
            yield return Send("给我提示"); Assert.That(demo.Echo.CurrentMode, Is.EqualTo(IntentKind.Wait)); Assert.That(panel.Session.Messages.Last().IsRefusal, Is.True);
            typeof(JournalStore).GetMethod("Replace", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(demo.GetComponent<PlayerJournal>().Store, new object[] { Array.Empty<JournalEntry>() });
            controller.AnswerProvider = _ => ScriptCommands.Answer("MUST_NOT_LEAK_ANSWER"); yield return Send("给我提示"); Assert.That(panel.Session.Messages.Last().Text, Does.Not.Contain("MUST_NOT_LEAK_ANSWER"));
            Assert.That(knowledge.Synchronize(memories.Store), Is.True); controller.enabled = false; yield return Send("跟随我"); Assert.That(demo.Echo.CurrentMode, Is.EqualTo(IntentKind.Wait));
            controller.enabled = true; yield return Send("跟随我"); Assert.That(controller.LastDispatchedKind, Is.EqualTo(IntentKind.Follow));
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (controller) controller.enabled = false; knowledge?.Dispose(); knowledge = null; if (panel && panel.IsOpen) panel.SetOpen(false);
            if (inputs != null) { inputs.Dispose(); Assert.That(inputs.IsRestored, Is.True); inputs = null; }
            if (Application.isPlaying)
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous); var unload = scene.IsValid() ? SceneManager.UnloadSceneAsync(scene) : null; var timer = System.Diagnostics.Stopwatch.StartNew();
                while (unload != null && !unload.isDone) { if (timer.Elapsed.TotalSeconds > 5) throw new TimeoutException("Execution fixture unload timeout."); yield return null; }
                Application.runInBackground = background; yield return new ExitPlayMode();
            }
        }
    }
}
