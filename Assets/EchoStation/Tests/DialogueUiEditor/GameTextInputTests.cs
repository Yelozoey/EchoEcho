using System;
using System.Collections;
using EchoStation.Player;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EchoStation.Tests.DialogueUi
{
    public sealed class GameTextInputTests
    {
        private Scene scene, previous;
        private SyntheticInputScope inputs;
        private GameTextInput field;
        private InputSystemUIInputModule module;
        private BaseInput oldInput;
        private bool background;
        private IEnumerator Setup()
        {
            background = Application.runInBackground; Application.runInBackground = true; previous = SceneManager.GetActiveScene();
            scene = SceneManager.CreateScene("S2_Text_" + Guid.NewGuid().ToString("N")); SceneManager.SetActiveScene(scene); inputs = new SyntheticInputScope();
            var system = new GameObject("TestEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); system.GetComponent<EventSystem>().sendNavigationEvents = false; module = system.GetComponent<InputSystemUIInputModule>(); oldInput = module.inputOverride;
            var canvas = new GameObject("TextCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var root = new GameObject("TextInput", typeof(RectTransform), typeof(Image)); root.SetActive(false); root.transform.SetParent(canvas.transform, false); root.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 60);
            var label = new GameObject("Text", typeof(RectTransform), typeof(Text)); label.transform.SetParent(root.transform, false); label.GetComponent<RectTransform>().sizeDelta = new Vector2(960, 55); var text = label.GetComponent<Text>(); text.font = RuntimeUiFont.Get(); text.fontSize = 22; text.supportRichText = false;
            field = root.AddComponent<GameTextInput>(); field.textComponent = text; field.targetGraphic = root.GetComponent<Image>(); field.characterLimit = 200; field.lineType = InputField.LineType.SingleLine; root.SetActive(true);
            EventSystem.current.SetSelectedGameObject(root); field.ActivateInputField();
            var timer = System.Diagnostics.Stopwatch.StartNew(); while (!field.isFocused) { if (timer.Elapsed.TotalSeconds > 5) Assert.Fail("Real text field did not focus."); yield return R04ShadowProbe.Frame(); }
        }
        private IEnumerator PressKeys(params Key[] keys)
        {
            InputSystem.QueueStateEvent(inputs.Keyboard, new KeyboardState()); yield return R04ShadowProbe.Frame();
            InputSystem.QueueStateEvent(inputs.Keyboard, new KeyboardState(keys)); yield return R04ShadowProbe.Frame();
            InputSystem.QueueStateEvent(inputs.Keyboard, new KeyboardState()); yield return R04ShadowProbe.Frame();
        }
        private IEnumerator Text(string text)
        { foreach (var c in text) InputSystem.QueueTextEvent(inputs.Keyboard, c); yield return R04ShadowProbe.Frame(); }
        private IEnumerator Compose(string value)
        { var e = IMECompositionEvent.Create(inputs.Keyboard.deviceId, value, InputState.currentTime); InputSystem.QueueEvent(ref e); yield return R04ShadowProbe.Frame(); }

        [UnityTest] public IEnumerator D02_A01_InputSystemChineseCharactersEnterActualFieldOnce()
        {
            yield return new EnterPlayMode(); yield return Setup(); yield return Text("调查储藏室");
            Assert.That(field.text, Is.EqualTo("调查储藏室")); Assert.That(field.ReceivedTextEvents, Is.EqualTo(5)); Assert.That(field.textComponent.supportRichText, Is.False);
        }
        [UnityTest] public IEnumerator D02_A02_BackspaceArrowsAndControlSelectionUseTheSameKeyboard()
        {
            yield return new EnterPlayMode(); yield return Setup(); yield return Text("调查"); yield return PressKeys(Key.Backspace); Assert.That(field.text, Is.EqualTo("调"));
            yield return PressKeys(Key.LeftArrow); yield return Text("先"); Assert.That(field.text, Is.EqualTo("先调"));
            yield return PressKeys(Key.LeftCtrl, Key.A); yield return Text("等待"); Assert.That(field.text, Is.EqualTo("等待"));
        }
        [UnityTest] public IEnumerator D02_A03_CharacterLimitAndCompositionCommitDoNotAccidentallySubmit()
        {
            yield return new EnterPlayMode(); yield return Setup(); var submitted = 0; field.SubmitRequested += () => submitted++;
            yield return Compose("diao"); Assert.That(field.IsComposing, Is.True); yield return PressKeys(Key.Enter); Assert.That(submitted, Is.Zero);
            yield return Compose(""); yield return Text("调"); yield return R04ShadowProbe.Frame(); yield return R04ShadowProbe.Frame();
            Assert.That(field.text, Is.EqualTo("调")); yield return PressKeys(Key.Enter); Assert.That(submitted, Is.EqualTo(1));
            yield return Text(new string('查', 205)); Assert.That(field.text.Length, Is.EqualTo(200));
        }
        [UnityTest] public IEnumerator D02_A04_DisableRestoresModuleAndStopsTextSubscriptionThenCanReenable()
        {
            yield return new EnterPlayMode(); yield return Setup(); var cancelled = 0; field.CancelRequested += () => cancelled++;
            yield return PressKeys(Key.Escape); Assert.That(cancelled, Is.EqualTo(1)); yield return Text("已输入");
            field.gameObject.SetActive(false); Assert.That(module.inputOverride, Is.SameAs(oldInput)); var count = field.ReceivedTextEvents;
            yield return Text("不应进入"); Assert.That(field.ReceivedTextEvents, Is.EqualTo(count));
            field.gameObject.SetActive(true); field.ActivateInputField(); yield return R04ShadowProbe.Frame(); yield return R04ShadowProbe.Frame();
            Assert.That(module.inputOverride, Is.TypeOf<GameTextBaseInput>());
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            if (field) field.gameObject.SetActive(false);
            if (module) Assert.That(module.inputOverride, Is.SameAs(oldInput));
            if (inputs != null) { inputs.Dispose(); Assert.That(inputs.IsRestored, Is.True); inputs = null; }
            if (Application.isPlaying)
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous); var unload = scene.IsValid() ? SceneManager.UnloadSceneAsync(scene) : null;
                var timer = System.Diagnostics.Stopwatch.StartNew(); while (unload != null && !unload.isDone) { if (timer.Elapsed.TotalSeconds > 5) throw new TimeoutException("Text fixture unload timed out."); yield return null; }
                Application.runInBackground = background; yield return new ExitPlayMode();
            }
        }
    }
}
