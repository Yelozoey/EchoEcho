using System;
using System.Collections;
using System.Linq;
using EchoStation.Dialogue;
using EchoStation.Player;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EchoStation.Tests.DialogueUi
{
    public sealed class DialoguePanelTests
    {
        private Scene scene, previous;
        private SyntheticInputScope inputs;
        private bool background;
        private CabinDemo demo;
        private DialoguePanel panel;
        private PlayerInputReader reader;
        private IEnumerator Setup()
        {
            background = Application.runInBackground; Application.runInBackground = true; previous = SceneManager.GetActiveScene(); inputs = new SyntheticInputScope();
            scene = EditorSceneManager.LoadSceneInPlayMode("Assets/EchoStation/World/Scenes/ES_Cabin_Demo.unity", new LoadSceneParameters(LoadSceneMode.Additive, LocalPhysicsMode.Physics3D));
            var timer = System.Diagnostics.Stopwatch.StartNew(); while (!scene.isLoaded) { if (timer.Elapsed.TotalSeconds > 5) Assert.Fail("Dialogue cabin load timeout."); yield return null; }
            SceneManager.SetActiveScene(scene); demo = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CabinDemo>()).Single();
            timer.Restart(); while (!demo.Ready) { if (timer.Elapsed.TotalSeconds > 5) Assert.Fail("Actual cabin startup failed: " + demo.Status); yield return R04ShadowProbe.Frame(); }
            reader = demo.GetComponent<PlayerInputReader>(); reader.SetApplicationFocus(true); reader.SetPointerCaptured(true);
            panel = demo.gameObject.AddComponent<DialoguePanel>(); yield return R04ShadowProbe.Frame();
        }
        private IEnumerator PressKeys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(inputs.Keyboard, new KeyboardState()); yield return R04ShadowProbe.Frame();
            InputSystem.QueueStateEvent(inputs.Keyboard, new KeyboardState(keys)); yield return R04ShadowProbe.Frame();
            InputSystem.QueueStateEvent(inputs.Keyboard, new KeyboardState()); yield return R04ShadowProbe.Frame();
        }
        private IEnumerator TypeText(string value) { foreach (var c in value) InputSystem.QueueTextEvent(inputs.Keyboard, c); yield return R04ShadowProbe.Frame(); }
        private IEnumerator Focused()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew(); while (!panel.InputField.isFocused) { if (timer.Elapsed.TotalSeconds > 5) Assert.Fail("Dialogue input focus timeout."); yield return R04ShadowProbe.Frame(); }
        }
        [UnityTest] public IEnumerator D02_B01_RealTChineseEnterAndLiteralBubblesBlockGameplayKeys()
        {
            yield return new EnterPlayMode(); yield return Setup(); yield return PressKeys(Key.T); Assert.That(panel.IsOpen, Is.True); yield return Focused();
            Assert.That(reader.UiBlocked, Is.True); Assert.That(reader.PointerCaptured, Is.False); var submitted = 0; panel.PlayerSubmitted += _ => submitted++;
            yield return PressKeys(Key.Enter); Assert.That(submitted, Is.Zero);
            yield return TypeText("跟随我"); yield return PressKeys(Key.Enter); Assert.That(submitted, Is.EqualTo(1)); Assert.That(panel.Session.Messages[0].Text, Is.EqualTo("跟随我"));
            panel.Session.TryAdd(DialogueSpeaker.Echo, "<b>已收到。请检查记录。</b>", false, out var reply); yield return R04ShadowProbe.Frame();
            Assert.That(panel.DisplayedText(reply.Number), Is.EqualTo(reply.Text)); Assert.That(panel.Canvas.GetComponentsInChildren<Text>(true).All(t => !t.supportRichText), Is.True);
            var torch = demo.GetComponent<PlayerFlashlight>(); torch.SetOn(false);
            yield return PressKeys(Key.W, Key.E, Key.F, Key.Space, Key.J, Key.Tab);
            Assert.That(reader.Move, Is.EqualTo(Vector2.zero)); Assert.That(reader.InteractPressed, Is.False); Assert.That(reader.JumpPressed, Is.False); Assert.That(torch.IsOn, Is.False);
            Assert.That(demo.GetComponent<JournalPanel>().IsOpen, Is.False); Assert.That(demo.GetComponent<CabinControls>().IsOpen, Is.False); Assert.That(panel.IsOpen, Is.True);
            yield return PressKeys(Key.Escape); Assert.That(panel.IsOpen, Is.False); Assert.That(reader.UiBlocked, Is.False); Assert.That(reader.PointerCaptured, Is.False);
        }
        [UnityTest] public IEnumerator D02_B02_ExistingJournalAndControlsAreMutuallyExclusiveAndFocusLossClosesDialogue()
        {
            yield return new EnterPlayMode(); yield return Setup(); var journal = demo.GetComponent<JournalPanel>(); var controls = demo.GetComponent<CabinControls>();
            Assert.That(journal.SetOpen(true), Is.True); Assert.That(panel.SetOpen(true), Is.False); journal.SetOpen(false);
            Assert.That(controls.SetOpen(true), Is.True); Assert.That(panel.SetOpen(true), Is.False); controls.SetOpen(false);
            Assert.That(panel.SetOpen(true), Is.True); Assert.That(journal.SetOpen(true), Is.False); Assert.That(controls.SetOpen(true), Is.False);
            reader.SetApplicationFocus(false); yield return R04ShadowProbe.Frame(); Assert.That(panel.IsOpen, Is.False); Assert.That(reader.UiBlocked, Is.False); Assert.That(panel.SubmitInput(), Is.False);
        }
        [UnityTest] public IEnumerator D02_B03_OneHundredRowsAreReusedAndNewEchoMessageDoesNotJumpOldReading()
        {
            yield return new EnterPlayMode(); yield return Setup();
            for (var i = 0; i < 120; i++) panel.Session.TryAdd(DialogueSpeaker.Echo, "历史记录" + i + "。" + new string('字', 50), false, out _);
            Assert.That(panel.SetOpen(true), Is.True); yield return Focused(); yield return R04ShadowProbe.Frame();
            Assert.That(panel.DisplayedCount, Is.EqualTo(100)); Assert.That(panel.AllocatedRows, Is.EqualTo(100));
            panel.MessageScroll.verticalNormalizedPosition = 1; yield return R04ShadowProbe.Frame(); panel.Session.TryAdd(DialogueSpeaker.Echo, "新增记录保留当前阅读位置。", false, out _); yield return R04ShadowProbe.Frame();
            Assert.That(panel.MessageScroll.verticalNormalizedPosition, Is.EqualTo(1).Within(.01f));
            for (var i = 0; i < 100; i++) panel.Session.TryAdd(DialogueSpeaker.Echo, "后续记录" + i, false, out _);
            yield return R04ShadowProbe.Frame(); Assert.That(panel.AllocatedRows, Is.EqualTo(100)); Assert.That(panel.Session.Messages.Count, Is.EqualTo(100));
        }
        [UnityTest] public IEnumerator D02_B04_BindDisableAndReenableDoNotLeaveOldSessionOrInputHooks()
        {
            yield return new EnterPlayMode(); yield return Setup(); Assert.That(panel.SetOpen(true), Is.True); yield return Focused();
            var old = panel.Session; var replacement = new DialogueSession(); replacement.TryAdd(DialogueSpeaker.Echo, "恢复后的消息。", false, out var message); panel.Bind(replacement);
            old.TryAdd(DialogueSpeaker.Echo, "旧会话不应显示。", false, out _); yield return R04ShadowProbe.Frame(); Assert.That(panel.DisplayedCount, Is.EqualTo(1)); Assert.That(panel.DisplayedText(message.Number), Is.EqualTo("恢复后的消息。"));
            panel.enabled = false; Assert.That(reader.UiBlocked, Is.False); Assert.That(panel.IsOpen, Is.False); var count = replacement.Messages.Count; yield return TypeText("不应提交"); Assert.That(replacement.Messages.Count, Is.EqualTo(count));
            panel.enabled = true; Assert.That(panel.IsOpen, Is.False); Assert.That(panel.SetOpen(true), Is.True); yield return Focused(); yield return PressKeys(Key.Escape); Assert.That(panel.IsOpen, Is.False);
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (panel && panel.IsOpen) panel.SetOpen(false);
            if (inputs != null) { inputs.Dispose(); Assert.That(inputs.IsRestored, Is.True); inputs = null; }
            if (Application.isPlaying)
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous); var unload = scene.IsValid() ? SceneManager.UnloadSceneAsync(scene) : null; var timer = System.Diagnostics.Stopwatch.StartNew();
                while (unload != null && !unload.isDone) { if (timer.Elapsed.TotalSeconds > 5) throw new TimeoutException("Dialogue fixture unload timeout."); yield return null; }
                Application.runInBackground = background; yield return new ExitPlayMode();
            }
        }
    }
}
