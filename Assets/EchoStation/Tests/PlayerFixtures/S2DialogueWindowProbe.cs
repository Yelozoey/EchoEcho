using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Dialogue;
using EchoStation.Echo;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace EchoStation.Tests.PlayerFixtures
{
    public sealed class S2DialogueWindowProbe : MonoBehaviour
    {
        private CabinDemo demo;
        private DialoguePanel panel;
        private Keyboard keyboard;
        private Mouse mouse;
        private string phase;
        private bool running;
        [Serializable] private sealed class Result
        {
            public bool passed, textRouted, emptyRejected, modal, rowBudget, pointerReleased, overrideRestored, inputRestored, hdr, displayHdr, useLlm, startupEventSystem, startupInputModule, cameraPresent;
            public int width, height, textEvents, submitted, rows;
            public string unity, api, pipeline, cpu, gpu, os, phase, error, input, inputEvidence;
        }
        private static string Argument(string flag)
        { var args = Environment.GetCommandLineArgs(); var index = Array.IndexOf(args, flag); return index >= 0 && index + 1 < args.Length ? args[index + 1] : null; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        { if (!Application.isEditor && Argument("--s2-dialogue-report") != null) new GameObject("S2_Dialogue_CLI_Probe").AddComponent<S2DialogueWindowProbe>(); }
        private void Update() { if (running && demo) demo.GetComponent<PlayerInputReader>().SetApplicationFocus(true); }
        private IEnumerator Start()
        {
            var argument = Argument("--s2-dialogue-report"); if (Application.isEditor || string.IsNullOrWhiteSpace(argument)) yield break;
            var path = Path.GetFullPath(argument); Directory.CreateDirectory(Path.GetDirectoryName(path));
            running = true; Application.runInBackground = true; var settings = InputSystem.settings; var background = settings.backgroundBehavior;
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            var physical = InputSystem.devices.Where(d => (d is Keyboard || d is Mouse) && d != keyboard && d != mouse && d.enabled).ToArray();
            foreach (var device in physical) InputSystem.DisableDevice(device);
            var result = new Result { unity = Application.unityVersion, api = SystemInfo.graphicsDeviceType.ToString(), cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, os = SystemInfo.operatingSystem, inputEvidence = "Actual Windows Player InputSystem QueueTextEvent/keyboard/mouse routing; not an OS IME candidate-window claim." };
            var stack = new Stack<IEnumerator>(); stack.Push(Run(result, Path.GetDirectoryName(path)));
            while (stack.Count > 0)
            {
                bool more; object current = null;
                try { more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current; }
                catch (Exception error) { result.error = error.ToString(); break; }
                if (!more) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            if (panel && panel.IsOpen) panel.SetOpen(false);
            if (keyboard.added) InputSystem.RemoveDevice(keyboard); if (mouse.added) InputSystem.RemoveDevice(mouse);
            foreach (var device in physical) if (device.added) InputSystem.EnableDevice(device);
            settings.backgroundBehavior = background; result.inputRestored = settings == InputSystem.settings && settings.backgroundBehavior == background && physical.All(d => !d.added || d.enabled);
            result.phase = phase; result.passed &= string.IsNullOrEmpty(result.error) && result.inputRestored; running = false;
            File.WriteAllText(path, JsonUtility.ToJson(result, true)); Application.Quit(result.passed ? 0 : 1);
        }
        private static void Check(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
        private static IEnumerator Until(Func<bool> condition, double seconds, string label)
        { var timer = System.Diagnostics.Stopwatch.StartNew(); while (!condition()) { if (timer.Elapsed.TotalSeconds > seconds) throw new TimeoutException(label); yield return null; } }
        private IEnumerator PressKey(Key value)
        { InputSystem.QueueStateEvent(keyboard, new KeyboardState(value)); yield return null; InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null; }
        private IEnumerator TypeText(string value)
        { foreach (var character in value) InputSystem.QueueTextEvent(keyboard, character); yield return null; yield return null; }
        private IEnumerator Click(Button button)
        {
            yield return new WaitForEndOfFrame(); UnityEngine.Canvas.ForceUpdateCanvases(); var rect = button.GetComponent<RectTransform>(); var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var clicked = false; UnityEngine.Events.UnityAction witness = () => clicked = true; button.onClick.AddListener(witness);
            try
            {
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point, buttons = 1 }); yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = point }); yield return null; yield return null;
            }
            finally { button.onClick.RemoveListener(witness); }
            Check(clicked, "Actual send button click missed.");
        }
        private static IEnumerator Screenshot(string path)
        {
            if (File.Exists(path)) throw new InvalidOperationException("Use a fresh evidence directory.");
            yield return new WaitForEndOfFrame(); var capture = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(path, capture.EncodeToPNG()); } finally { Destroy(capture); }
        }
        private IEnumerator Run(Result result, string directory)
        {
            phase = "startup"; yield return Until(() => SplashScreen.isFinished, 15, "Splash timeout.");
            demo = UnityEngine.Object.FindAnyObjectByType<CabinDemo>(); Check(demo, "Actual cabin demo missing."); yield return Until(() => demo.Ready, 5, "Cabin startup timeout.");
            result.startupEventSystem = EventSystem.current; result.startupInputModule = EventSystem.current && EventSystem.current.currentInputModule;
            phase = "event-system-ready"; yield return Until(() => EventSystem.current && EventSystem.current.currentInputModule, 5, "EventSystem/input module startup timeout.");
            var reader = demo.GetComponent<PlayerInputReader>(); reader.SetApplicationFocus(true); panel = demo.gameObject.AddComponent<DialoguePanel>();
            var previousInput = EventSystem.current.currentInputModule.inputOverride; var camera = demo.GetComponentInChildren<Camera>(); result.cameraPresent = camera; Check(camera, "Player camera missing."); var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            result.width = Screen.width; result.height = Screen.height; result.hdr = camera.allowHDR && pipeline && pipeline.supportsHDR; result.displayHdr = HDROutputSettings.main != null && HDROutputSettings.main.active;
            result.pipeline = pipeline ? pipeline.name : "null"; result.useLlm = GameRuntimeOptions.UseLLM;
            Check(result.width == 1920 && result.height == 1080 && result.hdr && !result.displayHdr && !camera.GetUniversalAdditionalCameraData().allowHDROutput && result.pipeline == "ES_Gameplay_ForwardHDR" && result.api == "Direct3D11" && !result.useLlm, "Actual rendering baseline mismatch.");
            panel.PlayerSubmitted += message => { result.submitted++; panel.Session.TryAdd(DialogueSpeaker.Echo, "已收到。请继续检查记录。", false, out _); };
            phase = "text-entry"; yield return PressKey(Key.T); Check(panel.IsOpen && reader.UiBlocked && !reader.PointerCaptured, "Actual T/modal route failed.");
            yield return Until(() => panel.InputField.isFocused, 5, "Actual input focus timeout."); yield return PressKey(Key.Enter); result.emptyRejected = result.submitted == 0;
            result.input = "调查储藏室可疑点"; yield return TypeText(result.input); result.textEvents = panel.InputField.ReceivedTextEvents;
            result.textRouted = panel.InputField.text == result.input && result.textEvents == result.input.Length; Check(result.textRouted, "Chinese InputSystem text did not reach actual field exactly once.");
            yield return PressKey(Key.Enter); Check(result.submitted == 1 && panel.Session.Messages.Count == 2 && panel.DisplayedCount == 2, "Enter or echo bubble route failed.");
            yield return Screenshot(Path.Combine(directory, "Dialogue.png"));
            phase = "modal"; Check(!demo.GetComponent<JournalPanel>().SetOpen(true) && !demo.GetComponent<CabinControls>().SetOpen(true), "Other panel opened during dialogue.");
            yield return PressKey(Key.Escape); Check(!panel.IsOpen && !reader.UiBlocked, "Actual escape close failed.");
            yield return PressKey(Key.J); yield return PressKey(Key.T); Check(demo.GetComponent<JournalPanel>().IsOpen && !panel.IsOpen, "Journal mutual exclusion failed."); yield return PressKey(Key.Escape);
            yield return PressKey(Key.Tab); yield return PressKey(Key.T); Check(demo.GetComponent<CabinControls>().IsOpen && !panel.IsOpen, "Control mutual exclusion failed."); yield return PressKey(Key.Tab); result.modal = true;
            phase = "send-click-history"; yield return PressKey(Key.T); yield return Until(() => panel.InputField.isFocused, 5, "Second input focus timeout."); yield return TypeText("给我提示"); yield return Click(panel.SendButton); Check(result.submitted == 2, "Actual click did not submit text once.");
            for (var i = 0; i < 110; i++) panel.Session.TryAdd(DialogueSpeaker.Echo, "记录" + i + "：校准资料已归档，等待后续复核。", false, out _);
            yield return null; yield return null; panel.MessageScroll.verticalNormalizedPosition = 1; yield return null;
            result.rows = panel.AllocatedRows; result.rowBudget = result.rows == 100 && panel.DisplayedCount == 100; Check(result.rowBudget, "Row/history budget exceeded.");
            yield return Screenshot(Path.Combine(directory, "History.png")); yield return PressKey(Key.Escape);
            result.pointerReleased = !reader.PointerCaptured && !reader.UiBlocked && !panel.IsOpen; result.overrideRestored = EventSystem.current.currentInputModule.inputOverride == previousInput;
            Check(result.pointerReleased && result.overrideRestored, "Closing left pointer/UI/input override state behind."); result.passed = result.emptyRejected && result.textRouted && result.modal && result.rowBudget;
        }
    }
}
