using System;
using System.Collections;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class FlashlightInputTests
    {
        private Scene scene,previous;private bool background;private SyntheticInputScope inputs;
        [UnityTest]public IEnumerator P03_A01_ActualFTogglesOnceAndRespectsUiFocusAndDisableLifecycle()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("FlashlightInputFixture");SceneManager.SetActiveScene(scene);inputs=new SyntheticInputScope();var actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));actor.GetComponent<FirstPersonMotor>().enabled=false;actor.GetComponentInChildren<Camera>().enabled=false;var reader=actor.GetComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);var torch=actor.GetComponent<PlayerFlashlight>();Assert.That(torch.IsOn,Is.False);Assert.That(torch.Source.enabled,Is.False);
            InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.F));yield return R04ShadowProbe.Frame();Assert.That(torch.IsOn&&torch.Source.enabled,Is.True);for(var i=0;i<5;i++)yield return R04ShadowProbe.Frame();Assert.That(torch.IsOn,Is.True,"Held F must not repeat.");InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.F));yield return R04ShadowProbe.Frame();Assert.That(torch.IsOn,Is.False);
            reader.SetUiBlocked(true);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.F));yield return R04ShadowProbe.Frame();Assert.That(torch.IsOn,Is.False);reader.SetUiBlocked(false);reader.SetPointerCaptured(true);reader.SetApplicationFocus(false);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return R04ShadowProbe.Frame();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.F));yield return R04ShadowProbe.Frame();Assert.That(torch.IsOn,Is.False);
            torch.SetOn(true);torch.enabled=false;Assert.That(torch.Source.enabled,Is.False);Assert.That(torch.IsOn,Is.True);torch.enabled=true;Assert.That(torch.Source.enabled,Is.True);Assert.That(torch.Source.type,Is.EqualTo(LightType.Spot));Assert.That(torch.Source.intensity,Is.EqualTo(6));Assert.That(torch.Source.range,Is.EqualTo(20));Assert.That(torch.Source.spotAngle,Is.EqualTo(55));Assert.That(torch.Source.shadows,Is.EqualTo(LightShadows.Soft));Assert.That(new SerializedObject(torch.Source.GetUniversalAdditionalLightData()).FindProperty("m_AdditionalLightsShadowResolutionTier").intValue,Is.EqualTo(2));Assert.That(torch.Source.transform.parent,Is.SameAs(actor.GetComponent<FirstPersonMotor>().View));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True,"Original settings identity and flags must be restored.");inputs=null;}if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Flashlight fixture unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
