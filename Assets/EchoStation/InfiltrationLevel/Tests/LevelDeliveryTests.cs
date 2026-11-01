using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.InfiltrationLevel.Editor;
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
namespace EchoStation.InfiltrationLevel.Tests
{
    public sealed class LevelDeliveryTests
    {
        Scene previous,sample;Transform root;LevelTestScope scope;SyntheticInputScope inputs;
        RenderPipelineAsset defaultBefore,qualityBefore;
        IEnumerator Load()
        {
            scope=new LevelTestScope();previous=SceneManager.GetActiveScene();defaultBefore=GraphicsSettings.defaultRenderPipeline;qualityBefore=QualitySettings.renderPipeline;
            var game=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(game).Show();EditorWindow.GetWindow(game).Focus();
            sample=EditorSceneManager.LoadSceneInPlayMode(LevelBuilder.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive));while(!sample.isLoaded)yield return null;SceneManager.SetActiveScene(sample);root=LevelBuilder.World(sample);
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
            var lines=root.GetComponentsInChildren<LineRenderer>();Assert.That(lines.Length,Is.EqualTo(5));Assert.That(lines.All(l=>l.enabled),Is.True);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.Tab));yield return Frames(2);Assert.That(lines.All(l=>!l.enabled),Is.True);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());yield return Frames(2);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(Key.Tab));yield return Frames(2);Assert.That(lines.All(l=>l.enabled),Is.True);InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState());
            var rows=new JArray();foreach(var post in root.GetComponentsInChildren<LevelPost>())
            {
                var animator=post.GetComponentInChildren<Animator>();var clip=animator.runtimeAnimatorController.animationClips.Single();Assert.That(clip.name,Does.Contain("Idle"));Assert.That(clip.isLooping,Is.True,"Actual imported Idle must loop: "+clip.name);animator.Play("Idle",0,0);animator.Update(4.1f);var parts=post.GetComponentsInChildren<Transform>();var before=parts.Select(p=>p.localRotation).ToArray();animator.Update(.4f);float motion=parts.Select((p,i)=>Quaternion.Angle(p.localRotation,before[i])).Max();Assert.That(motion,Is.GreaterThan(.05));rows.Add(new JObject{{"id",post.Id},{"motionDegreesAfterFirstCycle",motion},{"sourceIdleLoop",true},{"clip",clip.name}});
            }
            Assert.That(root.GetComponentInChildren<LevelPreview>().AiEnabled,Is.False);Assert.That(root.GetComponentsInChildren<MonoBehaviour>().Where(m=>m).Any(m=>(m.GetType().Namespace??"").StartsWith("EchoStation.AI",StringComparison.Ordinal)),Is.False);
            reader.SetApplicationFocus(false);Assert.That(reader.PointerCaptured,Is.False);Assert.That(reader.Move,Is.EqualTo(Vector2.zero));File.WriteAllText(LevelBuilder.Evidence+"/DeliveryInput_"+Guid.NewGuid().ToString("N").Substring(0,8)+".json",new JObject{{"jumpHigh",high},{"tabActualLines",5},{"fourIdleLoops",rows},{"ai",false}}.ToString());
        }
        struct ProbeSample
        {
            public int View,Mode,Frame;public double Delta,Cpu,Gpu;public long Gc,Memory,Draws,Batches,Main,GpuCounter;public uint TimingCount;public ulong Timestamp;public bool Valid;
        }
        static JObject Stats(IEnumerable<double> values)
        {
            var a=values.Where(v=>double.IsFinite(v)&&v>=0).OrderBy(v=>v).ToArray();return a.Length==0?new JObject{{"count",0}}:new JObject{{"count",a.Length},{"mean",a.Average()},{"p95",a[Mathf.Clamp((int)Math.Ceiling(a.Length*.95)-1,0,a.Length-1)]},{"max",a.Last()}};
        }
        [UnityTest,Timeout(240000)]public IEnumerator ActualEditorProfilerSamplesAt1080pWithEffectComparison()
        {
            yield return new EnterPlayMode();yield return Load();root.GetComponentInChildren<SalvageMotor>().enabled=false;root.GetComponentInChildren<PlayerInputReader>().enabled=false;
            var camera=root.GetComponentInChildren<Camera>();camera.scene=sample;var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear);target.Create();var oldTarget=camera.targetTexture;camera.targetTexture=target;
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(LevelLighting.RendererPath);var ao=LevelLighting.Ao(renderer);bool oldAo=ao.isActive;var fog=root.GetComponentInChildren<LevelFogField>();bool oldFog=fog.enabled;
            var rows=new JArray();var samples=new ProbeSample[720];int index=0;var validity=new JObject();var timings=new FrameTiming[1];var seen=new HashSet<ulong>(1024);
            using var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);using var memory=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory",1);using var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);using var batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);using var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);using var gpu=ProfilerRecorder.StartNew(ProfilerCategory.Render,"GPU Frame Time",1);
            validity["gc"]=gc.Valid;validity["memory"]=memory.Valid;validity["draws"]=draws.Valid;validity["batches"]=batches.Valid;validity["mainThread"]=main.Valid;validity["gpuCounter"]=gpu.Valid;
            try
            {
                Assert.That(gc.Valid&&memory.Valid&&draws.Valid,Is.True,"Actual memory/GC/draw counters required");
                var positions=new[]{new Vector3(0,1.6f,-5),new Vector3(-12,1.6f,-5),new Vector3(12,1.6f,-7)};var targets=new[]{new Vector3(1,3,0),new Vector3(-12,1.2f,2),new Vector3(12,1.2f,2)};
                for(int view=0;view<3;view++)for(int effects=0;effects<4;effects++)
                {
                    camera.transform.position=positions[view];camera.transform.LookAt(targets[view]);ao.SetActive(effects==0||effects==2);renderer.SetDirty();fog.enabled=effects==0||effects==1;
                    var warm=System.Diagnostics.Stopwatch.StartNew();while(warm.Elapsed.TotalSeconds<.75){FrameTimingManager.CaptureFrameTimings();yield return Frames(1);}
                    for(int i=0;i<60;i++)
                    {
                        FrameTimingManager.CaptureFrameTimings();yield return Frames(1);uint count=FrameTimingManager.GetLatestTimings(1,timings);var t=timings[0];bool valid=count>0&&t.frameStartTimestamp!=0&&t.cpuFrameTime>0&&t.gpuFrameTime>0&&seen.Add(t.frameStartTimestamp);
                        samples[index++]=new ProbeSample{View=view,Mode=effects,Frame=Time.frameCount,Delta=Time.unscaledDeltaTime*1000,Gc=gc.LastValue,Memory=memory.LastValue,Draws=draws.LastValue,Batches=batches.Valid?batches.LastValue:0,Main=main.Valid?main.LastValue:0,GpuCounter=gpu.Valid?gpu.LastValue:0,TimingCount=count,Valid=valid,Cpu=t.cpuFrameTime,Gpu=t.gpuFrameTime,Timestamp=t.frameStartTimestamp};
                    }
                }
                foreach(var p in samples)rows.Add(new JObject{{"view",p.View},{"mode",p.Mode},{"aoOn",p.Mode==0||p.Mode==2},{"fogOn",p.Mode==0||p.Mode==1},{"frame",p.Frame},{"deltaMs",p.Delta},{"gcBytes",p.Gc},{"editorUsedBytes",p.Memory},{"drawCalls",p.Draws},{"batches",p.Batches},{"mainNs",p.Main},{"gpuCounterNs",p.GpuCounter},{"timingCount",p.TimingCount},{"validGpuTiming",p.Valid},{"cpuMs",p.Cpu},{"gpuMs",p.Gpu},{"timestamp",p.Timestamp}});
                var groups=new JArray();foreach(var g in rows.GroupBy(r=>((int)r["view"],(int)r["mode"])))groups.Add(new JObject{{"view",g.Key.Item1},{"mode",g.Key.Item2},{"deltaMs",Stats(g.Select(r=>(double)r["deltaMs"]))},{"mainMs",Stats(g.Where(r=>(long)r["mainNs"]>0).Select(r=>(long)r["mainNs"]/1e6))},{"validGpuMs",Stats(g.Where(r=>(bool)r["validGpuTiming"]).Select(r=>(double)r["gpuMs"]))},{"drawCalls",Stats(g.Select(r=>(double)r["drawCalls"]))},{"gcBytes",Stats(g.Select(r=>(double)r["gcBytes"]))},{"usedBytes",Stats(g.Select(r=>(double)r["editorUsedBytes"]))}});
                File.WriteAllText(LevelBuilder.Evidence+"/EditorProfiler_"+Guid.NewGuid().ToString("N").Substring(0,8)+".json",new JObject{{"preallocatedSamplesJsonAfterCapture",true},{"warmupSecondsPerMode",.75},{"editorOverheadIncluded",true},{"offscreenCameraTarget",true},{"width",camera.pixelWidth},{"height",camera.pixelHeight},{"device",SystemInfo.graphicsDeviceName},{"cpu",SystemInfo.processorType},{"ramMb",SystemInfo.systemMemorySize},{"api",SystemInfo.graphicsDeviceType.ToString()},{"frameTimingEnabled",FrameTimingManager.IsFeatureEnabled()},{"counterValidity",validity},{"targetHardwareAcceptance",false},{"groups",groups},{"samples",rows}}.ToString());
                Assert.That(camera.pixelWidth,Is.EqualTo(1920));Assert.That(camera.pixelHeight,Is.EqualTo(1080));Assert.That(rows.Count,Is.EqualTo(720));Assert.That(rows.All(r=>(long)r["drawCalls"]>0&&(long)r["editorUsedBytes"]>0),Is.True);
            }
            finally{ao.SetActive(oldAo);renderer.SetDirty();fog.enabled=oldFog;camera.targetTexture=oldTarget;target.Release();UnityEngine.Object.Destroy(target);}
        }
        [UnityTearDown]public IEnumerator Close()
        {
            if(!Application.isPlaying)yield break;if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(sample.IsValid()&&sample.isLoaded){var u=SceneManager.UnloadSceneAsync(sample);while(!u.isDone)yield return null;}scope?.Dispose();scope=null;Assert.That(GraphicsSettings.defaultRenderPipeline,Is.SameAs(defaultBefore));Assert.That(QualitySettings.renderPipeline,Is.SameAs(qualityBefore));yield return new ExitPlayMode();
        }
    }
}
