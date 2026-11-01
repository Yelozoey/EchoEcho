using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationLevel;
using EchoStation.InfiltrationLevel.Editor;
using EchoStation.InfiltrationMetal.Editor;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.Tests.Player;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.InfiltrationMetal.Tests
{
    public sealed class MetalDeliveryTests
    {
        Scene previous,sample;Transform root;MetalTestScope scope;SyntheticInputScope inputs;
        RenderPipelineAsset defaultBefore,qualityBefore;
        IEnumerator Load()
        {
            scope=new MetalTestScope();previous=SceneManager.GetActiveScene();defaultBefore=GraphicsSettings.defaultRenderPipeline;qualityBefore=QualitySettings.renderPipeline;
            var game=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(game).Show();EditorWindow.GetWindow(game).Focus();
            sample=EditorSceneManager.LoadSceneInPlayMode(MetalScene.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!sample.isLoaded)yield return null;SceneManager.SetActiveScene(sample);root=MetalScene.World(sample);
            for(int i=0;i<5;i++)yield return Frames(1);
        }
        static IEnumerator Frames(int count)
        {
            for(int i=0;i<count;i++){int frame=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.frameCount==frame){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8));yield return null;}}
        }
        [UnityTest,Timeout(120000)]public IEnumerator JumpRouteToggleFourAuthoredIdleLoopsAndNoAi()
        {
            yield return new EnterPlayMode();yield return Load();inputs=new SyntheticInputScope();var motor=root.GetComponentInChildren<SalvageMotor>();var reader=motor.GetComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);
            motor.RestorePose(LevelLayout.Start,0,0);Physics.SyncTransforms();var settle=System.Diagnostics.Stopwatch.StartNew();while(!motor.Grounded&&settle.Elapsed.TotalSeconds<3)yield return Frames(1);Assert.That(motor.Grounded&&reader.CanPlay,Is.True);
            InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.Space));yield return Frames(2);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());float high=motor.transform.position.y;var timer=System.Diagnostics.Stopwatch.StartNew();while(timer.Elapsed.TotalSeconds<1.5){high=Mathf.Max(high,motor.transform.position.y);yield return Frames(1);}Assert.That(high,Is.GreaterThan(.35));Assert.That(motor.transform.position.y,Is.LessThan(.2));
            var reflection=root.GetComponent<MetalReflectionView>();InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.F8));yield return Frames(2);Assert.That(reflection.ReflectionOn,Is.False);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return Frames(2);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.F8));yield return Frames(2);Assert.That(reflection.ReflectionOn,Is.True);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return Frames(2);
            var lines=root.GetComponentsInChildren<LineRenderer>();Assert.That(lines.Length,Is.EqualTo(5));Assert.That(lines.All(l=>l.enabled),Is.True);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.Tab));yield return Frames(2);Assert.That(lines.All(l=>!l.enabled),Is.True);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return Frames(2);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.Tab));yield return Frames(2);Assert.That(lines.All(l=>l.enabled),Is.True);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());
            var rows=new JArray();foreach(var post in root.GetComponentsInChildren<LevelPost>())
            {
                var animator=post.GetComponentInChildren<Animator>();var clip=animator.runtimeAnimatorController.animationClips.Single();Assert.That(clip.name,Does.Contain("Idle"));Assert.That(clip.isLooping,Is.True,"Actual imported Idle must loop: "+clip.name);animator.Play("Idle",0,0);animator.Update(4.1f);var parts=post.GetComponentsInChildren<Transform>();var before=parts.Select(p=>p.localRotation).ToArray();animator.Update(.4f);float motion=parts.Select((p,i)=>Quaternion.Angle(p.localRotation,before[i])).Max();Assert.That(motion,Is.GreaterThan(.05));rows.Add(new JObject{{"id",post.Id},{"motionDegreesAfterFirstCycle",motion},{"sourceIdleLoop",true},{"clip",clip.name}});
            }
            Assert.That(root.GetComponentInChildren<LevelPreview>().AiEnabled,Is.False);Assert.That(root.GetComponentsInChildren<MonoBehaviour>().Where(m=>m).Any(m=>(m.GetType().Namespace??"").StartsWith("EchoStation.AI",StringComparison.Ordinal)),Is.False);
            reader.SetApplicationFocus(false);Assert.That(reader.PointerCaptured,Is.False);Assert.That(reader.Move,Is.EqualTo(Vector2.zero));File.WriteAllText(MetalScene.Evidence+"/DeliveryInput_"+Guid.NewGuid().ToString("N").Substring(0,8)+".json",new JObject{{"jumpHigh",high},{"tabActualLines",5},{"fourIdleLoops",rows},{"ai",false}}.ToString());
        }
        [UnityTearDown]public IEnumerator Close()
        {
            if(!Application.isPlaying)yield break;if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded){var u=SceneManager.UnloadSceneAsync(sample);while(!u.isDone)yield return null;}scope?.Dispose();scope=null;Assert.That(GraphicsSettings.defaultRenderPipeline,Is.SameAs(defaultBefore));Assert.That(QualitySettings.renderPipeline,Is.SameAs(qualityBefore));yield return new ExitPlayMode();
        }
    }
}
