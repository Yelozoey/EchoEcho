using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Player;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Profiling;

namespace EchoStation.Tests.PlayerFixtures
{
    public static class P09PerformanceCapture
    {
        [Serializable]public struct Sample{public int block,frame;public double deltaMs,cpuMs,gpuMs,mainMs,renderMs,presentWaitMs;public long gcBytes,usedBytes,drawCalls;public ulong timestamp;}
        [Serializable]public sealed class Report{public bool valid,profilerEnabled,allocationCallstacks;public int ordinaryFrames,validGpuFrames;public long warmupCounterChecksum;public string rawFile,threadSource,counterAvailability;public string[] availableCounters;public Sample[] samples;}
        public static void Position(CabinDemo demo,Vector3 position,float yaw,float pitch=0)
        {var controller=demo.GetComponent<CharacterController>();controller.enabled=false;demo.transform.position=position;demo.GetComponent<FirstPersonMotor>().SetLookAngles(yaw,pitch);demo.GetComponent<FirstPersonMotor>().ResetMotion();controller.enabled=true;Physics.SyncTransforms();}
        public static IEnumerator Run(CabinDemo demo,Report report,string directory,bool allocationStacks=false)
        {
            var oldEnabled=Profiler.enabled;var oldBinary=Profiler.enableBinaryLog;var oldPath=Profiler.logFile;var oldStacks=Profiler.enableAllocationCallstacks;report.rawFile=Path.GetFullPath(Path.Combine(directory,"S1Profiler.raw"));report.threadSource="FrameTiming.cpuMainThreadFrameTime / cpuRenderThreadFrameTime / cpuMainThreadPresentWaitTime";
            Profiler.enabled=false;Profiler.logFile=report.rawFile;Profiler.enableBinaryLog=true;Profiler.enableAllocationCallstacks=allocationStacks;Profiler.enabled=true;report.profilerEnabled=Profiler.enabled;report.allocationCallstacks=Profiler.enableAllocationCallstacks;
            try
            {
                yield return null;yield return null;var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);report.availableCounters=handles.Select(ProfilerRecorderHandle.GetDescription).Where(d=>d.Name.Contains("Thread")||d.Name.Contains("Memory")||d.Name.Contains("GC ")||d.Name.Contains("Draw Calls")).Select(d=>d.Category.Name+":"+d.Name).ToArray();
                using var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);using var memory=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory",1);using var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);report.counterAvailability="gc="+gc.Valid+" memory="+memory.Valid+" draws="+draws.Valid;if(!gc.Valid||!memory.Valid||!draws.Valid)throw new InvalidOperationException("Required Profiler counters unavailable: "+report.counterAvailability);report.samples=new Sample[900];var timing=new FrameTiming[1];var warmupEnd=Time.realtimeSinceStartup+10;while(Time.realtimeSinceStartup<warmupEnd){FrameTimingManager.CaptureFrameTimings();yield return null;FrameTimingManager.GetLatestTimings(1,timing);report.warmupCounterChecksum=gc.LastValue^memory.LastValue^draws.LastValue;}
                var positions=new[]{new Vector3(2,.05f,-2.5f),new Vector3(6.5f,.05f,1),new Vector3(0,.05f,-6)};var yaws=new[]{-40f,50f,0f};for(var block=0;block<3;block++)
                {
                    Position(demo,positions[block],yaws[block]);yield return new WaitForSecondsRealtime(1);for(var i=0;i<300;i++){FrameTimingManager.CaptureFrameTimings();yield return null;var count=FrameTimingManager.GetLatestTimings(1,timing);var t=timing[0];var index=block*300+i;report.samples[index]=new Sample{block=block,frame=Time.frameCount,deltaMs=Time.unscaledDeltaTime*1000,cpuMs=count>0?t.cpuFrameTime:-1,gpuMs=count>0?t.gpuFrameTime:-1,timestamp=count>0?t.frameStartTimestamp:0,mainMs=count>0?t.cpuMainThreadFrameTime:-1,renderMs=count>0?t.cpuRenderThreadFrameTime:-1,presentWaitMs=count>0?t.cpuMainThreadPresentWaitTime:-1,gcBytes=gc.LastValue,usedBytes=memory.LastValue,drawCalls=draws.LastValue};if(count>0&&t.gpuFrameTime>0&&t.cpuFrameTime>0&&t.frameStartTimestamp!=0)report.validGpuFrames++;report.ordinaryFrames++;}
                }
                report.valid=report.ordinaryFrames==900&&report.validGpuFrames>=600&&report.profilerEnabled;
            }
            finally{Profiler.enabled=false;Profiler.enableAllocationCallstacks=oldStacks;Profiler.enableBinaryLog=oldBinary;Profiler.logFile=oldPath;Profiler.enabled=oldEnabled;}
        }
    }
}
