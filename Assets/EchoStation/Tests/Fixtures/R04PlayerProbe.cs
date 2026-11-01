using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace EchoStation.Tests.Fixtures
{
    public sealed class R04PlayerProbe : MonoBehaviour
    {
        public UniversalRendererData validationRenderer;
        [Serializable] private class Result { public bool passed; public int screenWidth,screenHeight; public string error; public R04ShadowProbe.Result shadows; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Background() { if(Array.IndexOf(Environment.GetCommandLineArgs(),"--r04-report")>=0) Application.runInBackground=true; }
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();var flag=Array.IndexOf(args,"--r04-report");if(flag<0 || flag+1>=args.Length) yield break;
            var path=Path.GetFullPath(args[flag+1]); var directory=Path.GetDirectoryName(path); Directory.CreateDirectory(directory);
            yield return null;yield return null;yield return null;
            var report=new Result{screenWidth=Screen.width,screenHeight=Screen.height}; R04ShadowProbe probe=null; IEnumerator run=null;
            try { probe=new R04ShadowProbe(gameObject.scene,validationRenderer);report.shadows=probe.Report;run=probe.Run(directory); }
            catch(Exception e) {report.error=e.ToString();}
            if(run!=null)
            {
                while(true)
                {
                    bool more;try{more=run.MoveNext();}catch(Exception e){report.error=e.ToString();break;}
                    if(!more)break;yield return run.Current;
                }
            }
            probe?.Dispose();report.passed=string.IsNullOrEmpty(report.error) && report.shadows!=null && report.shadows.passed &&
                report.screenWidth==1920 && report.screenHeight==1080 && report.shadows.api=="Direct3D11";
            File.WriteAllText(path,JsonUtility.ToJson(report,true));if(Array.IndexOf(args,"--r04-exit")>=0) Application.Quit(report.passed?0:1);
        }
    }
}
