using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using EchoStation.Player;
using EchoStation.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace EchoStation.Tests.PlayerFixtures
{
    public sealed class P06FocusWindowProbe:MonoBehaviour
    {
        public GameObject player;public Material chartMaterial;private bool running;
        [Serializable]private class Sample{public int block;public bool active;public ulong timestamp;public double cpuMs,gpuMs;}
        [Serializable]private class Result{public bool passed,hdr,displayHdr,uiSharp;public int width,height,uiOff,uiOn,invalid,duplicate;public string error,unity,api;public float nearOff,nearOn,farOff,farOn,uiDifference;public Sample[] samples;}
        private void Update(){if(running)player.GetComponent<PlayerInputReader>().SetApplicationFocus(true);}
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();var index=Array.IndexOf(args,"--p06-report");if(index<0||index+1>=args.Length)yield break;running=true;Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;var path=Path.GetFullPath(args[index+1]);Directory.CreateDirectory(Path.GetDirectoryName(path));var result=new Result{width=Screen.width,height=Screen.height,unity=Application.unityVersion,api=SystemInfo.graphicsDeviceType.ToString()};var run=Run(Path.GetDirectoryName(path),result);while(true){bool more;try{more=run.MoveNext();}catch(Exception error){result.error=error.ToString();break;}if(!more)break;yield return run.Current;}File.WriteAllText(path,JsonUtility.ToJson(result,true));if(Array.IndexOf(args,"--p06-exit")>=0)Application.Quit(result.passed?0:1);
        }
        private IEnumerator Run(string directory,Result result)
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();while(!SplashScreen.isFinished){if(timer.Elapsed.TotalSeconds>15)throw new TimeoutException("Splash timeout.");yield return null;}player.GetComponent<FirstPersonMotor>().enabled=false;player.GetComponent<CharacterController>().enabled=false;player.transform.position=new Vector3(0,-1.6f,0);player.transform.rotation=Quaternion.identity;var camera=player.GetComponentInChildren<Camera>();camera.transform.localRotation=Quaternion.identity;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;var reader=player.GetComponent<PlayerInputReader>();reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);var target=new GameObject("WindowFocusTarget");target.SetActive(false);target.transform.position=new Vector3(0,0,1.05f);var box=target.AddComponent<BoxCollider>();box.size=Vector3.one*.1f;box.isTrigger=true;target.AddComponent<WorldTarget>().Configure("window_focus_target","对焦测试");target.SetActive(true);Physics.SyncTransforms();
            using(var chart=new FocusChart(chartMaterial))
            {
                var focus=player.GetComponent<InteractionFocus>();focus.enabled=false;for(var i=0;i<15;i++)yield return null;yield return new WaitForEndOfFrame();var off=Capture(directory+"/WindowsFocusOff.png",out result.uiOff);focus.enabled=true;yield return new WaitForSecondsRealtime(.25f);yield return new WaitForEndOfFrame();var on=Capture(directory+"/WindowsFocusOn.png",out result.uiOn);result.nearOff=FocusChart.Contrast(off,1920,600,450,800,630);result.nearOn=FocusChart.Contrast(on,1920,600,450,800,630);result.farOff=FocusChart.Contrast(off,1920,1300,450,1600,630);result.farOn=FocusChart.Contrast(on,1920,1300,450,1600,630);double difference=0;var glyphPixels=0;for(var y=56;y<104;y++)for(var x=850;x<1070;x++){var a=off[y*1920+x];var b=on[y*1920+x];if(a.r>=200f/255&&a.g>=215f/255&&a.b>=240f/255){difference+=(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b))/3;glyphPixels++;}}result.uiDifference=glyphPixels>0?(float)(difference/glyphPixels):1;result.uiSharp=glyphPixels>200&&result.uiDifference<2f/255;
                var timings=new FrameTiming[1];var seen=new HashSet<ulong>();var samples=new List<Sample>();for(var block=0;block<4;block++){focus.enabled=block%2==1;for(var i=0;i<180;i++){FrameTimingManager.CaptureFrameTimings();yield return null;if(i<60)continue;var count=FrameTimingManager.GetLatestTimings(1,timings);var t=timings[0];if(count==0||t.frameStartTimestamp==0||t.cpuFrameTime<=0||t.gpuFrameTime<=0){result.invalid++;continue;}if(!seen.Add(t.frameStartTimestamp)){result.duplicate++;continue;}samples.Add(new Sample{block=block,active=focus.enabled,timestamp=t.frameStartTimestamp,cpuMs=t.cpuFrameTime,gpuMs=t.gpuFrameTime});}}result.samples=samples.ToArray();var counts=new int[4];foreach(var sample in samples)counts[sample.block]++;result.hdr=camera.allowHDR;result.displayHdr=HDROutputSettings.main!=null&&HDROutputSettings.main.active;result.passed=result.unity=="6000.6.7f1"&&result.api=="Direct3D11"&&result.width==1920&&result.height==1080&&result.hdr&&!result.displayHdr&&result.uiSharp&&result.nearOn>result.nearOff*.9f&&result.farOn<result.farOff*.8f;foreach(var count in counts)result.passed&=count>=60;
            }
        }
        private static Color[] Capture(string path,out int whites){var texture=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(path,texture.EncodeToPNG());var colors=texture.GetPixels();whites=0;for(var y=30;y<100;y++)for(var x=600;x<1320;x++){var c=colors[y*texture.width+x];if(c.r>.7f&&c.g>.7f&&c.b>.7f)whites++;}return colors;}finally{Destroy(texture);}}
    }
}
