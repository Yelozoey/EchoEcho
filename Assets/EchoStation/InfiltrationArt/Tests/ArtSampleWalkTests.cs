using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationArt.Editor;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.Tests.Player;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace EchoStation.InfiltrationArt.Tests
{
    public sealed class ArtSampleWalkTests
    {
        Scene previous,sample;GameObject root;SalvageMotor motor;PlayerInputReader reader;
        SyntheticInputScope inputs;bool background;float scale;int targetFrameRate;
        ArtSampleAudioScope audio;
        RenderPipelineAsset previousDefault,previousQuality;
        IEnumerator Load()
        {
            background=Application.runInBackground;Application.runInBackground=true;scale=Time.timeScale;Time.timeScale=1;
            targetFrameRate=Application.targetFrameRate;Application.targetFrameRate=60;
            var gameView=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(gameView).Show();EditorWindow.GetWindow(gameView).Focus();
            previous=SceneManager.GetActiveScene();previousDefault=GraphicsSettings.defaultRenderPipeline;previousQuality=QualitySettings.renderPipeline;
            audio=new ArtSampleAudioScope();
            sample=EditorSceneManager.LoadSceneInPlayMode(ArtSampleScene.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));
            while(!sample.isLoaded)yield return null;SceneManager.SetActiveScene(sample);yield return null;
            Assert.That(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(a=>a.enabled),Is.EqualTo(1));
            root=sample.GetRootGameObjects().Single(g=>g.name=="IA_ArtSample_World");motor=root.GetComponentInChildren<SalvageMotor>();reader=root.GetComponentInChildren<PlayerInputReader>();
            for(int i=0;i<5;i++)yield return null;
        }
        IEnumerator WaitSeconds(float duration)
        {var timer=System.Diagnostics.Stopwatch.StartNew();while(timer.Elapsed.TotalSeconds<duration)yield return null;}
        static IEnumerator Frames(int count)
        {
            for(int i=0;i<count;i++)
            {
                int frame=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();
                while(Time.frameCount==frame){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(5));yield return null;}
            }
        }
        void BeginInput()
        {inputs=new SyntheticInputScope();reader.SetApplicationFocus(true);reader.SetUiBlocked(false);reader.SetPointerCaptured(true);}
        [UnityTest,Timeout(90000)]
        public IEnumerator RealPlayerActionsMoveCollideLookAndJump()
        {
            yield return new EnterPlayMode();yield return Load();BeginInput();yield return Frames(2);
            Vector3 start=motor.transform.position;
            InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.W));yield return WaitSeconds(.6f);
            InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return Frames(2);
            Assert.That(motor.transform.position.z-start.z,Is.GreaterThan(1f),"Actual bound W action must move the controller");
            motor.RestorePose(new Vector3(3.3f,.08f,-4),90,0);reader.SetPointerCaptured(true);
            InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.W));yield return WaitSeconds(.55f);
            InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return Frames(2);
            Assert.That(motor.transform.position.x,Is.LessThan(3.85f),"Room wall must block the actual CharacterController");
            motor.RestorePose(new Vector3(0,.05f,-3.5f),0,0);Physics.SyncTransforms();
            var landing=System.Diagnostics.Stopwatch.StartNew();while(!motor.Grounded&&landing.Elapsed.TotalSeconds<2)yield return Frames(1);
            float yaw=motor.transform.eulerAngles.y;
            InputSystem.QueueStateEvent(inputs.Mouse,new MouseState{delta=new Vector2(24,0)});yield return Frames(2);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(yaw,motor.transform.eulerAngles.y)),Is.GreaterThan(.5f));
            reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);yield return Frames(2);
            var controller=motor.GetComponent<CharacterController>();Physics.Raycast(motor.transform.position+Vector3.up*.2f,Vector3.down,out var contact,2);
            File.WriteAllText(ArtSampleAssets.Evidence+"/JumpPrecondition_"+Guid.NewGuid().ToString("N").Substring(0,8)+".json",new JObject{{"frame",Time.frameCount},{"deltaTime",Time.deltaTime},{"fixedDeltaTime",Time.fixedDeltaTime},{"scale",Time.timeScale},{"targetFrameRate",Application.targetFrameRate},{"grounded",motor.Grounded},{"canPlay",reader.CanPlay},{"position",new JArray(motor.transform.position.x,motor.transform.position.y,motor.transform.position.z)},{"minMoveDistance",controller.minMoveDistance},{"skinWidth",controller.skinWidth},{"collisionFlags",controller.collisionFlags.ToString()},{"groundRay",contact.collider?contact.collider.name:null},{"floorY",contact.point.y}}.ToString());
            Assert.That(reader.CanPlay,Is.True,"Jump precondition: input captured");Assert.That(motor.Grounded,Is.True,"Jump precondition: grounded on the actual sample floor");
            InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.Space));yield return Frames(2);
            InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());
            float high=motor.transform.position.y;var timer=System.Diagnostics.Stopwatch.StartNew();
            while(timer.Elapsed.TotalSeconds<1.5){high=Mathf.Max(high,motor.transform.position.y);yield return null;}
            Assert.That(high,Is.GreaterThan(.35f));Assert.That(motor.transform.position.y,Is.LessThan(.2f));
        }
        static Quaternion[] Rotations(Transform[] transforms)=>transforms.Select(t=>t.localRotation).ToArray();
        static float Difference(Transform[] transforms,Quaternion[] before)=>transforms.Select((t,i)=>Quaternion.Angle(before[i],t.localRotation)).Max();
        [UnityTest,Timeout(90000)]
        public IEnumerator GuardUsesAuthoredMotionAtCorrectScaleAndKeepsAnimating()
        {
            yield return new EnterPlayMode();yield return Load();reader.SetUiBlocked(true);
            var guard=root.GetComponentsInChildren<ArtSampleModel>().Single(m=>m.Role=="Guard");var animator=guard.GetComponentInChildren<Animator>();
            animator.Play("Idle",0,0);animator.Update(.3f);
            var parts=guard.GetComponentsInChildren<Transform>();var first=Rotations(parts);animator.Update(.4f);
            Assert.That(Difference(parts,first),Is.GreaterThan(.05f),"A referenced clip must actually animate the imported rigid parts");
            var bounds=ArtSampleAssets.BoundsOf(guard.gameObject);Assert.That(bounds.size.y,Is.InRange(1.5f,2.1f));
            animator.Update(4f);var later=Rotations(parts);animator.Update(.4f);
            Assert.That(Difference(parts,later),Is.GreaterThan(.05f),"Idle must continue past its first cycle");
            string path=ArtSampleAssets.Evidence+"/GuardPlayback_"+Guid.NewGuid().ToString("N").Substring(0,8)+".json";
            File.WriteAllText(path,new JObject{{"animated",true},{"continuesAfterFirstCycle",true},{"heightMeters",bounds.size.y},{"clip",animator.runtimeAnimatorController.animationClips[0].name}}.ToString());
        }
        [UnityTest,Timeout(90000)]
        public IEnumerator PreviewHasNoAiAndEscapeClickAndFocusRespectPointerOwnership()
        {
            yield return new EnterPlayMode();yield return Load();BeginInput();yield return Frames(2);
            Assert.That(root.GetComponentInChildren<ArtSampleRuntime>().IsArtPreview,Is.True);
            Assert.That(root.GetComponentsInChildren<MonoBehaviour>(true).Where(m=>m).Any(m=>m.GetType().Namespace!=null&&m.GetType().Namespace.StartsWith("EchoStation.AI",StringComparison.Ordinal)),Is.False);
            Assert.That(reader.PointerCaptured,Is.True);
            InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.Escape));yield return Frames(2);
            Assert.That(reader.PointerCaptured,Is.False);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return null;
            InputSystem.QueueStateEvent(inputs.Mouse,new MouseState().WithButton(MouseButton.Left));yield return Frames(2);
            Assert.That(reader.PointerCaptured,Is.True);InputSystem.QueueStateEvent(inputs.Mouse,new MouseState());
            reader.SetApplicationFocus(false);Assert.That(reader.PointerCaptured,Is.False);Assert.That(reader.Move,Is.EqualTo(Vector2.zero));
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;
            if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(sample.IsValid()&&sample.isLoaded){var op=SceneManager.UnloadSceneAsync(sample);while(op!=null&&!op.isDone)yield return null;}
            audio?.Dispose();audio=null;
            Assert.That(GraphicsSettings.defaultRenderPipeline,Is.SameAs(previousDefault));Assert.That(QualitySettings.renderPipeline,Is.SameAs(previousQuality));
            Application.runInBackground=background;Time.timeScale=scale;Application.targetFrameRate=targetFrameRate;
            yield return new ExitPlayMode();
        }
    }
}
