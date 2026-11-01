using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using EchoStation.AI.Probe;
using EchoStation.Station;
using EchoStation.Player;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace EchoStation.AI.ProbeHost
{
 public sealed class G01WindowsProbe:MonoBehaviour
 {
  private static string Arg(string name){var a=Environment.GetCommandLineArgs();var i=Array.IndexOf(a,name);return i>=0&&i+1<a.Length?a[i+1]:null;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Install(){if(!Application.isEditor&&Arg("--g01-root")!=null)new GameObject("G01 Windows Probe").AddComponent<G01WindowsProbe>();}
  private IEnumerator Start()
  {
   Application.runInBackground=true;
   var root=Path.GetFullPath(Arg("--g01-root"));var output=Path.GetFullPath(Arg("--g01-output"));Directory.CreateDirectory(output);
   var record=new JObject{{"unity",Application.unityVersion},{"api",SystemInfo.graphicsDeviceType.ToString()},{"width",Screen.width},{"height",Screen.height},{"mainThread",Thread.CurrentThread.ManagedThreadId},{"pipeline",GraphicsSettings.currentRenderPipeline?GraphicsSettings.currentRenderPipeline.name:"null"}};
   var samples=new JArray();record["frames"]=samples;var timing=new FrameTiming[1];var count=0;string error=null;
   StationSession station=null;var deadline=Time.realtimeSinceStartup+30;
   while(!station||!station.Ready){station=FindAnyObjectByType<StationSession>();if(Time.realtimeSinceStartup>deadline){error="Station startup timeout";break;}yield return null;}
   if(station){station.GetComponent<PlayerInputReader>().SetPointerCaptured(false);station.GetComponent<StationPersistence>().ConfigureDirectory(Path.Combine(output,"ProbeSaves"));station.GetComponent<StationPersistence>().enabled=false;}
   var oldEnabled=Profiler.enabled;var oldBinary=Profiler.enableBinaryLog;var oldFile=Profiler.logFile;
   var raw=Path.Combine(output,"G01Profiler.raw");Profiler.enabled=false;Profiler.logFile=raw;Profiler.enableBinaryLog=true;Profiler.enabled=true;
   var shortReport=Path.Combine(output,"Short.json");var longReport=Path.Combine(output,"Long.json");Thread owner=null;
   try{owner=OrtCompatibilityProbe.Start(Path.Combine(root,"Library/EchoStation/S3Cache/models/qwen-int4/onnx",Arg("--g01-model")??"model_q4.onnx"),Path.Combine(root,"Docs/Tests/Stage3/Evidence/S3-G01/PrefillFixture.json"),Path.Combine(root,"Docs/Tests/Stage3/Evidence/S3-G01",Arg("--g01-reference")??""),shortReport,Thread.CurrentThread.ManagedThreadId);}
   catch(Exception e){error=e.ToString();}
   var started=Time.realtimeSinceStartup;
   while(error==null&&owner!=null&&owner.IsAlive&&Time.realtimeSinceStartup-started<90)
   {
    FrameTimingManager.CaptureFrameTimings();yield return null;
    if(FrameTimingManager.GetLatestTimings(1,timing)>0){var t=timing[0];samples.Add(new JObject{{"frame",Time.frameCount},{"timestamp",t.frameStartTimestamp},{"cpuMs",t.cpuFrameTime},{"gpuMs",t.gpuFrameTime},{"mainMs",t.cpuMainThreadFrameTime},{"renderMs",t.cpuRenderThreadFrameTime}});}
    count++;
   }
   if(owner!=null&&owner.IsAlive)error="Owner did not release within probe limit; process exit cleans probe";
   if(error==null)
   {
    var value=JObject.Parse(File.ReadAllText(shortReport));record["short"]=value;
    if(!(bool)value["passed"])error="Short model probe failed";
   }
   Profiler.enabled=false;Profiler.enableBinaryLog=oldBinary;Profiler.logFile=oldFile;Profiler.enabled=oldEnabled;
   yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(output,"Rendering.png"),image.EncodeToPNG());}finally{Destroy(image);}
   record["renderFramesDuringOwner"]=count;record["rawProfilerFile"]=raw;record["rawProfilerBytes"]=File.Exists(raw)?new FileInfo(raw).Length:0;record["error"]=error;record["passed"]=error==null&&count>0&&(long)record["rawProfilerBytes"]>0&&record["api"].ToString()=="Direct3D11"&&Screen.width==1920&&Screen.height==1080;
   File.WriteAllText(Path.Combine(output,"WindowsResult.json"),record.ToString());Application.Quit((bool)record["passed"]?0:1);
  }
 }
}
