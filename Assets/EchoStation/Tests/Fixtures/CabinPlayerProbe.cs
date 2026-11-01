using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Tests.Fixtures
{
    public sealed class CabinPlayerProbe : MonoBehaviour
    {
        public UniversalRendererData validationRenderer;
        [Serializable]private class Result
        {
            public bool passed;public string error,api,cpu,gpu,os;public int width,height,ramMb,vramMb;
            public CabinCollisionProbe.Result collision;public CabinLightingProbe.Result lighting;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Background(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--w01-report")>=0)Application.runInBackground=true;}
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();var flag=Array.IndexOf(args,"--w01-report");if(flag<0||flag+1>=args.Length)yield break;
            var path=Path.GetFullPath(args[flag+1]);var directory=Path.GetDirectoryName(path);Directory.CreateDirectory(directory);
            yield return null;yield return null;yield return null;
            var report=new Result{api=SystemInfo.graphicsDeviceType.ToString(),cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,os=SystemInfo.operatingSystem,
                width=Screen.width,height=Screen.height,ramMb=SystemInfo.systemMemorySize,vramMb=SystemInfo.graphicsMemorySize};
            CabinLightingProbe probe=null;IEnumerator run=null;
            try{report.collision=CabinCollisionProbe.Run(gameObject.scene.GetPhysicsScene());probe=new CabinLightingProbe(gameObject.scene,validationRenderer);report.lighting=probe.Report;run=probe.Run(directory);}
            catch(Exception e){report.error=e.ToString();}
            if(run!=null)while(true)
            {bool more;try{more=run.MoveNext();}catch(Exception e){report.error=e.ToString();break;}if(!more)break;yield return run.Current;}
            probe?.Dispose();report.passed=string.IsNullOrEmpty(report.error)&&report.collision!=null&&report.collision.passed&&report.collision.checks.Length==23&&
                report.lighting!=null&&report.lighting.passed&&report.api=="Direct3D11"&&report.width==1920&&report.height==1080;
            File.WriteAllText(path,JsonUtility.ToJson(report,true));if(Array.IndexOf(args,"--w01-exit")>=0)Application.Quit(report.passed?0:1);
        }
    }
}
