using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Tests.Fixtures
{
    public sealed class CabinVisibilityPlayerProbe : MonoBehaviour
    {
        public UniversalRendererData validationRenderer;
        [Serializable]public class Sample{public string name;public long median;public long[] draws;public int[] frames;public bool valid;}
        [Serializable]private class Result
        {
            public bool passed;public string error,api,cpu,gpu,os;public int width,height,ramMb,vramMb;
            public float imageDifference;public Sample[] samples;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Background(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--w01b3-report")>=0)Application.runInBackground=true;}
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();var flag=Array.IndexOf(args,"--w01b3-report");if(flag<0||flag+1>=args.Length)yield break;
            var path=Path.GetFullPath(args[flag+1]);var directory=Path.GetDirectoryName(path);Directory.CreateDirectory(directory);yield return null;yield return null;yield return null;
            var report=new Result{api=SystemInfo.graphicsDeviceType.ToString(),cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,os=SystemInfo.operatingSystem,
                width=Screen.width,height=Screen.height,ramMb=SystemInfo.systemMemorySize,vramMb=SystemInfo.graphicsMemorySize};
            var run=Run(report,directory);
            while(true){bool more;try{more=run.MoveNext();}catch(Exception e){report.error=e.ToString();break;}if(!more)break;yield return run.Current;}
            if(run is IDisposable disposable)disposable.Dispose();report.passed=report.passed&&string.IsNullOrEmpty(report.error);
            File.WriteAllText(path,JsonUtility.ToJson(report,true));if(Array.IndexOf(args,"--w01b3-exit")>=0)Application.Quit(report.passed?0:1);
        }
        private IEnumerator Run(Result report,string directory)
        {
            using var probe=new CabinVisibilityProbe(gameObject.scene,validationRenderer);
            probe.HiddenRoomView(false);yield return R04ShadowProbe.Frame();var off=probe.Frames.Capture(Path.Combine(directory,"01_Off.png"));
            probe.HiddenRoomView(true);yield return R04ShadowProbe.Frame();report.imageDifference=SceneFrameCapture.Difference(off,probe.Frames.Capture(Path.Combine(directory,"02_On.png")));
            probe.InsideStorageView();yield return R04ShadowProbe.Frame();probe.Frames.Capture(Path.Combine(directory,"03_Storage.png"));
            using var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);
            if(!recorder.Valid)throw new InvalidOperationException("Actual Draw Calls recorder unavailable.");
            report.samples=new Sample[3];
            for(var stage=0;stage<3;stage++)
            {
                if(stage<2)probe.HiddenRoomView(stage==1);else probe.InsideStorageView();
                for(var warm=0;warm<10;warm++)yield return R04ShadowProbe.Frame();
                var row=new Sample{name=new[]{"off","on","storage"}[stage],draws=new long[16],frames=new int[16]};
                for(var i=0;i<16;i++){yield return R04ShadowProbe.Frame();row.draws[i]=recorder.LastValue;row.frames[i]=Time.frameCount;}
                row.median=row.draws.OrderBy(v=>v).ElementAt(8);row.valid=row.draws.All(v=>v>0)&&row.frames.Distinct().Count()==16;report.samples[stage]=row;
            }
            report.passed=report.samples.All(s=>s.valid)&&report.samples[0].median>report.samples[1].median&&report.samples[1].median>0&&
                report.imageDifference<1e-6f&&report.width==1920&&report.height==1080&&report.api=="Direct3D11";
        }
    }
}
