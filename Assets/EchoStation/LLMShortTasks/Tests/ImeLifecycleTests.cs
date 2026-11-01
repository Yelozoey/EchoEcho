using System;
using System.Collections;
using System.Collections.Generic;
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

namespace EchoStation.LLMShortTasks.Tests
{
    public sealed class ImeLifecycleTests
    {
        Scene scene,previous;SyntheticInputScope inputs;GameTextInput field;InputSystemUIInputModule module;BaseInput original;bool background;readonly List<bool> commands=new List<bool>();InputDeviceCommandDelegate hook;
        unsafe long? Command(InputDevice device,InputDeviceCommand* command)
        {if(inputs!=null&&command!=null&&device==inputs.Keyboard&&command->type==EnableIMECompositionCommand.Type){var c=(EnableIMECompositionCommand*)command;commands.Add(c->imeEnabled);return InputDeviceCommand.GenericSuccess;}return null;}
        unsafe void AttachCommandHook(){hook=Command;InputSystem.onDeviceCommand+=hook;}
        IEnumerator Boot()
        {
            background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("IME01_"+Guid.NewGuid().ToString("N"));SceneManager.SetActiveScene(scene);inputs=new SyntheticInputScope();AttachCommandHook();
            var es=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));module=es.GetComponent<InputSystemUIInputModule>();original=module.inputOverride;var canvas=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var rect=new GameObject("Field",typeof(RectTransform),typeof(Image));rect.SetActive(false);rect.transform.SetParent(canvas.transform,false);rect.GetComponent<RectTransform>().sizeDelta=new Vector2(1000,80);var text=new GameObject("Text",typeof(RectTransform),typeof(Text));text.transform.SetParent(rect.transform,false);text.GetComponent<RectTransform>().sizeDelta=new Vector2(960,65);text.GetComponent<Text>().font=RuntimeUiFont.Get();field=rect.AddComponent<GameTextInput>();field.textComponent=text.GetComponent<Text>();field.targetGraphic=rect.GetComponent<Image>();field.characterLimit=200;rect.SetActive(true);field.Select();field.ActivateInputField();for(int i=0;i<5&&!field.isFocused;i++)yield return R04ShadowProbe.Frame();Assert.That(field.isFocused,Is.True);
        }
        [UnityTest]public IEnumerator FocusUsesRealImeCommandAndDoesNotReadNativeImeForSyntheticKeyboard()
        {yield return new EnterPlayMode();yield return Boot();Assert.That(commands,Does.Contain(true));Assert.That(field.LastImeCommandResult,Is.EqualTo(InputDeviceCommand.GenericSuccess));Assert.That(field.NativeImeBridgeActive,Is.False);}
        [UnityTest]public IEnumerator ReopenRequestsImeAgainAndDisableRestoresOriginalCommandAndModule()
        {yield return new EnterPlayMode();yield return Boot();commands.Clear();field.gameObject.SetActive(false);Assert.That(commands,Does.Contain(false));Assert.That(module.inputOverride,Is.SameAs(original));field.gameObject.SetActive(true);field.Select();field.ActivateInputField();yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();Assert.That(commands[commands.Count-1],Is.True);Assert.That(field.isFocused,Is.True);}
        [UnityTest]public IEnumerator CompositionEnterEscapeAndCommitGuardDoNotSubmitOrCloseEarly()
        {
            yield return new EnterPlayMode();yield return Boot();int submit=0,cancel=0;field.SubmitRequested+=()=>submit++;field.CancelRequested+=()=>cancel++;
            var composition=IMECompositionEvent.Create(inputs.Keyboard.deviceId,"nihao",InputState.currentTime);InputSystem.QueueEvent(ref composition);yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.Enter,Key.Escape));yield return R04ShadowProbe.Frame();Assert.That(submit,Is.Zero);Assert.That(cancel,Is.Zero);
            InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());composition=IMECompositionEvent.Create(inputs.Keyboard.deviceId,"",InputState.currentTime);InputSystem.QueueEvent(ref composition);InputSystem.QueueTextEvent(inputs.Keyboard,'你');InputSystem.QueueTextEvent(inputs.Keyboard,'好');yield return R04ShadowProbe.Frame();Assert.That(field.text,Is.EqualTo("你好"));yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.Enter));yield return R04ShadowProbe.Frame();Assert.That(submit,Is.EqualTo(1));Assert.That(cancel,Is.Zero);
        }
        [UnityTest]public IEnumerator SubmitCallbackMayDisableFieldWithoutDereferencingDetachedKeyboard()
        {
            yield return new EnterPlayMode();yield return Boot();int sent=0,cancel=0;field.SubmitRequested+=()=>{sent++;field.gameObject.SetActive(false);};field.CancelRequested+=()=>cancel++;
            InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.Enter,Key.Escape));yield return R04ShadowProbe.Frame();
            Assert.That(sent,Is.EqualTo(1));Assert.That(cancel,Is.Zero);Assert.That(field.isActiveAndEnabled,Is.False);Assert.That(module.inputOverride,Is.SameAs(original));
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(hook!=null)InputSystem.onDeviceCommand-=hook;hook=null;if(field)field.gameObject.SetActive(false);if(module)Assert.That(module.inputOverride,Is.SameAs(original));if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}
            if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}commands.Clear();
        }
    }
}
