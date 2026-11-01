using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EchoStation.Player;
using EchoStation.Player.Editor;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class PlayerMotorTests
    {
        private Scene scene,previous;private bool background;private GameObject actor;private Keyboard keyboard;private Mouse mouse;private SyntheticInputScope inputs;
        [Serializable]private class Result{public bool passed;public float[] distances;public float rotatedForwardX,focusDrift,uiDrift,movedBeforeFocus;}
        private FirstPersonMotor Setup()
        {
            background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("PlayerMotorFixture",new CreateSceneParameters(LocalPhysicsMode.Physics3D));SceneManager.SetActiveScene(scene);var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.position=new Vector3(0,-.1f,0);floor.transform.localScale=new Vector3(20,.2f,20);actor=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath));actor.GetComponentInChildren<Camera>().enabled=false;scene.GetPhysicsScene().Simulate(.02f);Directory.CreateDirectory("Docs/Tests/Stage1/Evidence/S1-P01");return actor.GetComponent<FirstPersonMotor>();
        }
        private void Position(Vector3 p){var controller=actor.GetComponent<CharacterController>();controller.enabled=false;actor.transform.position=p;controller.enabled=true;scene.GetPhysicsScene().Simulate(.02f);}
        [UnityTest]public IEnumerator P01_B01_RealControllerMovesFourDirectionsAndUsesRelativeYaw()
        {
            yield return new EnterPlayMode();var motor=Setup();motor.enabled=false;actor.GetComponent<PlayerInputReader>().enabled=false;var distances=new List<float>();
            foreach(var direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right}){Position(new Vector3(0,.03f,0));motor.SetLookAngles(0,0);var start=actor.transform.position;for(var i=0;i<60;i++)motor.ApplyInput(direction,Vector2.zero,1f/60);var delta=actor.transform.position-start;distances.Add(new Vector2(delta.x,delta.z).magnitude);Assert.That(Vector2.Distance(new Vector2(delta.x,delta.z),direction*3),Is.LessThan(.01f));}
            Position(new Vector3(0,.03f,0));motor.SetLookAngles(90,0);for(var i=0;i<30;i++)motor.ApplyInput(Vector2.up,Vector2.zero,1f/30);var result=new Result{distances=distances.ToArray(),rotatedForwardX=actor.transform.position.x,passed=Mathf.Abs(actor.transform.position.x-3)<.01f};motor.ApplyInput(Vector2.zero,new Vector2(0,10000),0);Assert.That(motor.Pitch,Is.EqualTo(-80));Assert.That(actor.transform.eulerAngles.x,Is.EqualTo(0).Within(.001f));File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-P01/ActualController.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True);
        }
        [UnityTest]public IEnumerator P01_B02_HeldVirtualWStopsImmediatelyOnFocusLossAndUiBlock()
        {
            yield return new EnterPlayMode();inputs=new SyntheticInputScope();
            var oldCursor=Cursor.lockState;var oldVisible=Cursor.visible;var motor=Setup();var input=actor.GetComponent<PlayerInputReader>();keyboard=inputs.Keyboard;mouse=inputs.Mouse;input.SetApplicationFocus(true);input.SetPointerCaptured(true);InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));InputSystem.Update();var start=actor.transform.position;var timer=System.Diagnostics.Stopwatch.StartNew();
            while(Vector3.Distance(start,actor.transform.position)<.5f)
            {
                if(timer.Elapsed.TotalSeconds>5)
                {
                    File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-P01/InputStallDiagnostics.json",JsonUtility.ToJson(new InputDiagnostics{canPlay=input.CanPlay,captured=input.PointerCaptured,uiBlocked=input.UiBlocked,appFocused=Application.isFocused,keyPressed=keyboard.wKey.isPressed,deviceEnabled=keyboard.enabled,motorEnabled=motor.enabled,move=input.Move,position=actor.transform.position,editorBehavior=InputSystem.settings.editorInputBehaviorInPlayMode.ToString(),backgroundBehavior=InputSystem.settings.backgroundBehavior.ToString()},true));Assert.Fail("Actual InputAction/Update did not move controller.");
                }
                yield return R04ShadowProbe.Frame();
            }
            var result=new Result{movedBeforeFocus=Vector3.Distance(start,actor.transform.position)};input.SetApplicationFocus(false);var held=actor.transform.position;yield return Observe(.3f);result.focusDrift=Vector3.Distance(held,actor.transform.position);Assert.That(input.PointerCaptured,Is.False);input.SetApplicationFocus(true);Assert.That(input.CanPlay,Is.False);input.SetPointerCaptured(true);timer.Restart();
            while(Vector3.Distance(held,actor.transform.position)<.3f){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Recaptured input did not resume.");yield return R04ShadowProbe.Frame();}input.SetUiBlocked(true);held=actor.transform.position;yield return Observe(.3f);result.uiDrift=Vector3.Distance(held,actor.transform.position);result.passed=result.movedBeforeFocus>=.5f&&result.focusDrift<.001f&&result.uiDrift<.001f;File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-P01/ActualFocusGate.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));
            InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return R04ShadowProbe.Frame();Assert.That(input.PointerCaptured,Is.False,"UI click must not capture gameplay.");InputSystem.QueueStateEvent(mouse,new MouseState());yield return R04ShadowProbe.Frame();input.SetUiBlocked(false);InputSystem.QueueStateEvent(mouse,new MouseState{buttons=1});yield return R04ShadowProbe.Frame();Assert.That(input.PointerCaptured,Is.True,"Fresh left click must recapture.");InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W,Key.Escape));yield return R04ShadowProbe.Frame();Assert.That(input.PointerCaptured,Is.False,"Escape must release pointer.");input.enabled=false;Assert.That(Cursor.lockState,Is.EqualTo(oldCursor));Assert.That(Cursor.visible,Is.EqualTo(oldVisible));
        }
        private static IEnumerator Observe(float seconds){var start=Time.time;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.time-start<seconds){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Player clock stalled.");yield return R04ShadowProbe.Frame();}}
        [Serializable]private class InputDiagnostics{public bool canPlay,captured,uiBlocked,appFocused,keyPressed,deviceEnabled,motorEnabled;public Vector2 move;public Vector3 position;public string editorBehavior,backgroundBehavior;}
        [Test]public void P01_B03_PrefabHasApprovedControllerAndHdrSdrCamera()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabSetup.PrefabPath);Assert.That(prefab,Is.Not.Null);var c=prefab.GetComponent<CharacterController>();Assert.That(c.height,Is.EqualTo(1.75f));Assert.That(c.radius,Is.EqualTo(.3f));Assert.That(c.center,Is.EqualTo(new Vector3(0,.875f,0)));Assert.That(c.skinWidth,Is.EqualTo(.03f));Assert.That(c.stepOffset,Is.EqualTo(.25f));Assert.That(c.slopeLimit,Is.EqualTo(45));var camera=prefab.GetComponentInChildren<Camera>();Assert.That(camera.transform.localPosition.y,Is.EqualTo(1.6f));Assert.That(camera.allowHDR,Is.True);Assert.That(camera.GetUniversalAdditionalCameraData().allowHDROutput,Is.False);Assert.That(camera.GetUniversalAdditionalCameraData().volumeLayerMask.value,Is.EqualTo(256));Assert.That(prefab.GetComponent<FirstPersonMotor>().View,Is.EqualTo(camera.transform));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True,"Original settings identity and flags must be restored.");inputs=null;}keyboard=null;mouse=null;if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Player fixture unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
