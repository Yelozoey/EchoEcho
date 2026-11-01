using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EchoStation.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Tests.Fixtures
{
    // Dedicated diagnostic scene only. Normal camera frames; no readback/PNG work in sampled blocks.
    public sealed class ScanWaveTimingProbe:MonoBehaviour
    {
        public UniversalRenderPipelineAsset pipeline;
        private bool effect;
        [Serializable]private class Sample{public int block,frame;public bool active;public ulong timestamp;public double cpuMs,gpuMs;}
        [Serializable]private class Result{public bool passed,frameTimingEnabled,internalHdr,displayHdrActive;public string unity,cpu,gpu,api,pipeline,error;public int width,height,invalid,duplicate;public float imageDifference;public Sample[] samples;}
        private void Awake(){if(!Application.isPlaying||!pipeline||Array.IndexOf(Environment.GetCommandLineArgs(),"--e12-report")<0)return;GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;Application.runInBackground=true;}
        private void LateUpdate(){var wave=GetComponent<ScanWaveView>().Source.State;if(effect){wave.Begin(new Vector3(0,.8f,0));wave.Tick(.3f);}else wave.Cancel();}
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();var index=Array.IndexOf(args,"--e12-report");if(index<0||index+1>=args.Length)yield break;
            var report=new Result{unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),pipeline=pipeline.name,width=Screen.width,height=Screen.height,frameTimingEnabled=FrameTimingManager.IsFeatureEnabled(),internalHdr=pipeline.supportsHDR&&GetComponent<Camera>().allowHDR,displayHdrActive=HDROutputSettings.main!=null&&HDROutputSettings.main.active};
            var path=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(Path.GetDirectoryName(path));yield return null;yield return null;yield return null;
            using(var images=new SceneFrameCapture(GetComponent<Camera>()))
            {
                var wave=GetComponent<ScanWaveView>().Source.State;wave.Cancel();var off=images.Capture(Path.Combine(Path.GetDirectoryName(path),"WindowsWaveOff.png"));wave.Begin(new Vector3(0,.8f,0));wave.Tick(.3f);var on=images.Capture(Path.Combine(Path.GetDirectoryName(path),"WindowsWaveOn.png"));report.imageDifference=SceneFrameCapture.Difference(off,on);wave.Cancel();
            }
            var timings=new FrameTiming[1];var timestamps=new HashSet<ulong>();var samples=new List<Sample>();
            for(var block=0;block<4;block++)
            {
                effect=block%2==1;
                for(var i=0;i<150;i++)
                {
                    FrameTimingManager.CaptureFrameTimings();yield return null;if(i<60)continue;var count=FrameTimingManager.GetLatestTimings(1,timings);var t=timings[0];
                    if(count==0||t.frameStartTimestamp==0||t.gpuFrameTime<=0||t.cpuFrameTime<=0){report.invalid++;continue;}if(!timestamps.Add(t.frameStartTimestamp)){report.duplicate++;continue;}
                    samples.Add(new Sample{block=block,frame=Time.frameCount,active=effect,timestamp=t.frameStartTimestamp,cpuMs=t.cpuFrameTime,gpuMs=t.gpuFrameTime});
                }
            }
            effect=false;report.samples=samples.ToArray();var perBlock=new int[4];foreach(var sample in samples)perBlock[sample.block]++;report.passed=report.unity=="6000.6.7f1"&&report.api=="Direct3D11"&&report.width==1920&&report.height==1080&&report.internalHdr&&!report.displayHdrActive&&report.frameTimingEnabled&&report.imageDifference>.001f;foreach(var count in perBlock)report.passed&=count>=60;
            File.WriteAllText(path,JsonUtility.ToJson(report,true));if(Array.IndexOf(args,"--e12-exit")>=0)Application.Quit(report.passed?0:1);
        }
    }
}
