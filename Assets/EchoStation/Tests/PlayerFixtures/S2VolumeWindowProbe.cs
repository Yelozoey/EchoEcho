using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Rendering;
using EchoStation.Station;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoStation.Tests.PlayerFixtures
{
 public sealed class S2VolumeWindowProbe:MonoBehaviour
 {
  [Serializable]private struct Sample{public int block,frame;public bool active,valid,duplicate;public ulong timestamp;public double cpuMs,gpuMs;}
  [Serializable]private sealed class Result{public bool passed,hdr,displayHdr,useLlm;public string unity,api,pipeline,cpu,gpu,os,error,phase;public int width,height,halfWidth,halfHeight,steps,ramMb,vramMb,invalid,duplicate;public double[] gpuMedian,gpuP95;public int[] validPerBlock;public Sample[] samples;}
  private Result result;private StationSession station;private LocalFogField field;private string folder,phase;
  private static string Argument(){var args=Environment.GetCommandLineArgs();var i=Array.IndexOf(args,"--s2-volume-report");return i>=0&&i+1<args.Length?args[i+1]:null;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Install(){if(!Application.isEditor&&Argument()!=null)new GameObject("S2_Volume_CLI_Probe").AddComponent<S2VolumeWindowProbe>();}
  private IEnumerator Start()
  {
   var argument=Argument();if(Application.isEditor||string.IsNullOrWhiteSpace(argument))yield break;var path=Path.GetFullPath(argument);folder=Path.GetDirectoryName(path);Directory.CreateDirectory(folder);Application.runInBackground=true;
   result=new Result{unity=Application.unityVersion,api=SystemInfo.graphicsDeviceType.ToString(),pipeline=GraphicsSettings.currentRenderPipeline?GraphicsSettings.currentRenderPipeline.name:"null",cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,os=SystemInfo.operatingSystem,ramMb=SystemInfo.systemMemorySize,vramMb=SystemInfo.graphicsMemorySize};
   var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool more;object current=null;try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){result.error=e.ToString();break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;}
   while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();if(field)field.enabled=true;result.phase=phase;result.passed&=string.IsNullOrEmpty(result.error);File.WriteAllText(path,JsonUtility.ToJson(result,true));Application.Quit(result.passed?0:1);
  }
  private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
  private IEnumerator Run()
  {
   phase="startup";var timer=System.Diagnostics.Stopwatch.StartNew();while(!station||!station.Ready){station=FindAnyObjectByType<StationSession>();if(timer.Elapsed.TotalSeconds>15)throw new TimeoutException("S2 session startup timeout");yield return null;}
   station.Echo.AutomaticWarningsEnabled=false;station.Echo.TrySubmit(ScriptCommands.Wait(),out _);station.GetComponent<PlayerInputReader>().enabled=false;station.GetComponent<CharacterController>().enabled=false;station.transform.position=new Vector3(0,-3.95f,26);station.Player.SetLookAngles(0,0);station.Player.enabled=false;station.Dialogue.enabled=false;station.GetComponent<JournalPanel>().enabled=false;station.GetComponent<StationControls>().enabled=false;station.GetComponent<StationTerminalPanel>().enabled=false;station.GetComponent<StationPowerPanel>().enabled=false;field=FindAnyObjectByType<LocalFogField>();Check(field,"Missing local fog field");result.pipeline=GraphicsSettings.currentRenderPipeline?GraphicsSettings.currentRenderPipeline.name:"null";
   var camera=station.Player.View.GetComponent<Camera>();result.width=Screen.width;result.height=Screen.height;result.hdr=camera.allowHDR;result.displayHdr=HDROutputSettings.main!=null&&HDROutputSettings.main.active;result.useLlm=GameRuntimeOptions.UseLLM;result.steps=LocalFogFeature.Steps;
   var timing=new FrameTiming[1];phase="warmup";var end=Time.realtimeSinceStartup+10;while(Time.realtimeSinceStartup<end){FrameTimingManager.CaptureFrameTimings();yield return null;FrameTimingManager.GetLatestTimings(1,timing);}
   result.samples=new Sample[480];result.validPerBlock=new int[4];result.gpuMedian=new double[4];result.gpuP95=new double[4];var seen=new HashSet<ulong>();
   for(var block=0;block<4;block++){
    phase="measure_"+block;field.enabled=(block&1)==1;for(var i=0;i<60;i++){FrameTimingManager.CaptureFrameTimings();yield return null;FrameTimingManager.GetLatestTimings(1,timing);}
    for(var i=0;i<120;i++){FrameTimingManager.CaptureFrameTimings();yield return null;var count=FrameTimingManager.GetLatestTimings(1,timing);var t=timing[0];var valid=count>0&&t.frameStartTimestamp!=0&&t.cpuFrameTime>0&&t.gpuFrameTime>0;var duplicate=valid&&!seen.Add(t.frameStartTimestamp);if(!valid)result.invalid++;if(duplicate)result.duplicate++;valid&=!duplicate;result.samples[block*120+i]=new Sample{block=block,frame=Time.frameCount,active=field.enabled,valid=valid,duplicate=duplicate,timestamp=count>0?t.frameStartTimestamp:0,cpuMs=count>0?t.cpuFrameTime:-1,gpuMs=count>0?t.gpuFrameTime:-1};if(valid)result.validPerBlock[block]++;}
    var values=result.samples.Where(s=>s.block==block&&s.valid).Select(s=>s.gpuMs).OrderBy(x=>x).ToArray();Check(values.Length>=100,"Insufficient valid GPU samples in block"+block);result.gpuMedian[block]=values[(int)Math.Ceiling(values.Length*.5)-1];result.gpuP95[block]=values[(int)Math.Ceiling(values.Length*.95)-1];
    if(field.enabled){result.halfWidth=field.LastWidth;result.halfHeight=field.LastHeight;}
    yield return new WaitForEndOfFrame();var image=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(folder,"Block_"+block+".png"),image.EncodeToPNG());}finally{Destroy(image);}
   }
   result.passed=result.unity=="6000.6.7f1"&&result.api=="Direct3D11"&&result.pipeline=="ES_S2_ForwardHDR"&&result.width==1920&&result.height==1080&&result.hdr&&!result.displayHdr&&!result.useLlm&&result.steps==16&&result.halfWidth==960&&result.halfHeight==540;phase="complete";
  }
 }
}
