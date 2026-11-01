using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Tests.Fixtures
{
    public sealed class R05PlayerProbe : MonoBehaviour
    {
        public UniversalRendererData validationRenderer;
        [Serializable] public class Sample
        {
            public string name;public long medianDraws;public long[] draws;public int[] frames;public bool valid;
        }
        [Serializable]private class Result
        {
            public bool passed,counterValid,hdr,displayHdrActive;
            public string error,unity,api,cpu,gpu,os;public int width,height,ramMb,vramMb;
            public string[] availableRenderCounters;public Sample[] states;public R05OptimizationProbe.Result images;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Background(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--r05-report")>=0)Application.runInBackground=true;}
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();var flag=Array.IndexOf(args,"--r05-report");if(flag<0||flag+1>=args.Length)yield break;
            var path=Path.GetFullPath(args[flag+1]);var directory=Path.GetDirectoryName(path);Directory.CreateDirectory(directory);
            yield return null;yield return null;yield return null;
            var report=new Result{unity=Application.unityVersion,api=SystemInfo.graphicsDeviceType.ToString(),cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,
                os=SystemInfo.operatingSystem,width=Screen.width,height=Screen.height,ramMb=SystemInfo.systemMemorySize,vramMb=SystemInfo.graphicsMemorySize};
            IEnumerator run=Run(report,directory);while(true)
            {
                bool more;try{more=run.MoveNext();}catch(Exception e){report.error=e.ToString();break;}
                if(!more)break;yield return run.Current;
            }
            if(run is IDisposable disposable)disposable.Dispose();
            report.passed=report.passed && string.IsNullOrEmpty(report.error);File.WriteAllText(path,JsonUtility.ToJson(report,true));
            if(Array.IndexOf(args,"--r05-exit")>=0)Application.Quit(report.passed?0:1);
        }
        private IEnumerator Run(Result report,string directory)
        {
            var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
            report.availableRenderCounters=handles.Select(ProfilerRecorderHandle.GetDescription).Where(d=>d.Category==ProfilerCategory.Render).Select(d=>d.Name).ToArray();
            using var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);
            report.counterValid=draws.Valid;if(!draws.Valid)throw new InvalidOperationException("Draw Calls Count recorder unavailable; no synthetic values substituted.");
            using var probe=new R05OptimizationProbe(gameObject.scene,validationRenderer);report.images=probe.Report;
            yield return probe.RunOcclusionImages(directory);
            var roots=gameObject.scene.GetRootGameObjects();var near=roots.Single(o=>o.name=="R05_NearRoom");
            var merged=near.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="R05_NearCombined");
            var sources=near.GetComponentsInChildren<MeshRenderer>().Where(r=>r!=merged).ToArray();
            roots.Single(o=>o.name=="R05_DynamicProxies").SetActive(false);
            var records=new List<Sample>();
            foreach(var name in new[]{"unmerged","merged","occlusion","reverse","insideFar"})
            {
                foreach(var renderer in sources)renderer.enabled=name!="merged";merged.enabled=name=="merged";
                if(name=="insideFar")probe.FarView(true);else probe.NearView(name=="occlusion");
                if(name=="reverse")probe.Camera.transform.rotation=Quaternion.Euler(0,180,0);
                // Only ordinary camera frames here; no manual render/readback contaminates counters.
                for(var warm=0;warm<10;warm++)yield return R04ShadowProbe.Frame();
                var sample=new Sample{name=name,draws=new long[16],frames=new int[16]};
                for(var i=0;i<16;i++){yield return R04ShadowProbe.Frame();sample.draws[i]=draws.LastValue;sample.frames[i]=Time.frameCount;}
                sample.medianDraws=sample.draws.OrderBy(v=>v).ElementAt(8);sample.valid=sample.draws.All(v=>v>0)&&sample.frames.Distinct().Count()==16;
                records.Add(sample);
            }
            foreach(var renderer in sources)renderer.enabled=true;merged.enabled=false;
            report.states=records.ToArray();report.hdr=probe.Camera.allowHDR&&((UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline).supportsHDR;
            report.displayHdrActive=UnityEngine.HDROutputSettings.main.active;
            var baseline=records[0].medianDraws;
            report.passed=report.images.passed&&records.All(s=>s.valid)&&baseline-records[1].medianDraws>=6&&baseline-records[2].medianDraws>=8&&
                baseline-records[3].medianDraws>=8&&records[4].medianDraws>records[2].medianDraws&&report.width==1920&&report.height==1080&&report.api=="Direct3D11"&&report.hdr&&!report.displayHdrActive;
        }
    }
}
