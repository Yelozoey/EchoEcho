using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.ReflectionSample.Editor;
using EchoStation.Tests.Player;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
namespace EchoStation.ReflectionSample.Tests
{
    public sealed class ReflectionDeliveryTests:ReflectionTestBase
    {
        [UnityTest,Timeout(120000)]public IEnumerator RealKeysMoveAndToggleAllThreeControlsAndPause()
        {
            yield return new EnterPlayMode();yield return Load();var game=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(game).Show();EditorWindow.GetWindow(game).Focus();var reader=root.GetComponentInChildren<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);
            using(var input=new SyntheticInputScope())
            {
                var initial=reader.transform.position;InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState(Key.W));yield return Frames(16);InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState());yield return Frames(2);float moved=Vector3.Distance(initial,reader.transform.position);Assert.That(moved,Is.GreaterThan(.15f));
                InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState(Key.Digit1));yield return Frames(2);Assert.That(preview.DynamicOn,Is.False);InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState());yield return Frames(2);InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState(Key.Digit1));yield return Frames(2);Assert.That(preview.DynamicOn,Is.True);InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState());yield return Frames(2);
                InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState(Key.Digit2));yield return Frames(2);Assert.That(preview.EnvironmentOn,Is.False);InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState());yield return Frames(2);InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState(Key.Digit3));yield return Frames(2);Assert.That(preview.HighlightsOn,Is.False);InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState());yield return Frames(2);
                InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState(Key.P));yield return Frames(2);Assert.That(root.GetComponent<ReflectionDemoMotion>().Paused,Is.True);InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState());yield return Frames(2);var position=root.Find("RS_RobotDemo").position;yield return Frames(4);Assert.That(Vector3.Distance(position,root.Find("RS_RobotDemo").position),Is.LessThan(.0001));
                InputSystem.QueueStateEvent(input.Keyboard,new KeyboardState(Key.Escape));yield return Frames(2);Assert.That(reader.PointerCaptured,Is.False);File.WriteAllText(ReflectionSampleBuilder.Evidence+"/Input_"+Guid.NewGuid().ToString("N").Substring(0,8)+".json",new JObject{{"moved",moved},{"digit1Twice",true},{"digit2",true},{"digit3",true},{"pauseP",true},{"escape",true}}.ToString());
            }
        }
        struct Sample{public bool On,Valid;public double Cpu,Gpu;public long Main,Draws,Memory;public ulong Stamp;}
        static JObject Stats(IEnumerable<double> values){var a=values.OrderBy(v=>v).ToArray();return a.Length==0?new JObject{{"count",0}}:new JObject{{"count",a.Length},{"mean",a.Average()},{"p95",a[(int)Math.Ceiling(a.Length*.95)-1]},{"max",a.Last()}};}
        [UnityTest,Timeout(120000)]public IEnumerator Actual1080pGpuSamplesComparePlanarOnAndOff()
        {
            yield return new EnterPlayMode();yield return Load();Freeze();var game=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");EditorWindow.GetWindow(game).Show();EditorWindow.GetWindow(game).Focus();camera.transform.position=new Vector3(0,1.6f,-2.55f);camera.transform.LookAt(new Vector3(0,.65f,.7f));
            var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGBHalf);target.Create();var previousTarget=camera.targetTexture;camera.targetTexture=target;var samples=new Sample[240];int index=0;var timings=new FrameTiming[1];var seen=new HashSet<ulong>(512);
            using var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1);using var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);using var memory=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory",1);
            try
            {
                Assert.That(main.Valid&&draws.Valid&&memory.Valid,Is.True);
                foreach(bool on in new[]{true,false})
                {
                    preview.SetDynamic(on);var warm=System.Diagnostics.Stopwatch.StartNew();while(warm.Elapsed.TotalSeconds<.75){FrameTimingManager.CaptureFrameTimings();yield return Frames(1);}
                    for(int i=0;i<120;i++){FrameTimingManager.CaptureFrameTimings();yield return Frames(1);uint count=FrameTimingManager.GetLatestTimings(1,timings);var t=timings[0];bool valid=count>0&&t.frameStartTimestamp!=0&&t.cpuFrameTime>0&&t.gpuFrameTime>0&&seen.Add(t.frameStartTimestamp);samples[index++]=new Sample{On=on,Valid=valid,Cpu=t.cpuFrameTime,Gpu=t.gpuFrameTime,Main=main.LastValue,Draws=draws.LastValue,Memory=memory.LastValue,Stamp=t.frameStartTimestamp};}
                }
                var rows=new JArray(samples.Select(s=>new JObject{{"on",s.On},{"valid",s.Valid},{"cpuMs",s.Cpu},{"gpuMs",s.Gpu},{"mainNs",s.Main},{"draws",s.Draws},{"memory",s.Memory},{"stamp",s.Stamp}}));var groups=new JArray(samples.GroupBy(s=>s.On).Select(g=>new JObject{{"on",g.Key},{"gpuMs",Stats(g.Where(s=>s.Valid).Select(s=>s.Gpu))},{"mainMs",Stats(g.Select(s=>s.Main/1e6))},{"draws",Stats(g.Select(s=>(double)s.Draws))}}));
                File.WriteAllText(ReflectionSampleBuilder.Evidence+"/Profiler_"+Guid.NewGuid().ToString("N").Substring(0,8)+".json",new JObject{{"width",1920},{"height",1080},{"device",SystemInfo.graphicsDeviceName},{"cpu",SystemInfo.processorType},{"api",SystemInfo.graphicsDeviceType.ToString()},{"editorOverheadIncluded",true},{"preallocatedSamples",true},{"warmSeconds",.75},{"validGpu",samples.Count(s=>s.Valid)},{"validGpuAndCounters",samples.Count(s=>s.Valid&&s.Draws>0&&s.Main>0&&s.Memory>0)},{"zeroCounterSamplesRetained",samples.Count(s=>s.Draws<=0||s.Main<=0)},{"targetHardwareAccepted",false},{"groups",groups},{"samples",rows}}.ToString());
                Assert.That(samples.All(s=>s.Memory>0),Is.True);foreach(var group in samples.GroupBy(s=>s.On))Assert.That(group.Count(s=>s.Valid&&s.Draws>0&&s.Main>0),Is.GreaterThanOrEqualTo(100),"Each mode needs 100 positive GPU+draw+CPU samples; zero counters remain invalid raw evidence");
            }
            finally{preview.SetDynamic(true);camera.targetTexture=previousTarget;target.Release();UnityEngine.Object.Destroy(target);}
        }
    }
}
