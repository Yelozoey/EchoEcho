#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.Salvage.Validation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
namespace EchoStation.SalvageNext.Validation
{
    public sealed class NextWindowsProbe:MonoBehaviour
    {
        [Serializable]public struct Sample{public int quality,zone;public bool valid;public ulong timestamp;public double cpuMs,gpuMs,mainMs,renderMs,deltaMs;public long gcBytes,usedBytes,drawCalls;}
        [Serializable]public sealed class Report{public bool passed,hdr,displayHdr,useLLM,inputRestored,profilerRestored;public string mode,error,api,unity,cpu,gpu,pipeline,scene;public int width,height;public string[] actions;public Sample[] samples;}
        string folder,mode;Report report;NextSession session;NextDisplayPanel panel;NextRoomStreaming stream;SalvageMotor motor;SalvageEcho echo;SalvageEchoCoordinator brain;PlayerInputReader input;SalvageR2InputScope devices;readonly List<string> actions=new List<string>();bool background;
        static string Arg(string name){var a=Environment.GetCommandLineArgs();int i=Array.IndexOf(a,name);return i>=0&&i+1<a.Length?a[i+1]:null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Bootstrap()
        {
            if(Application.isEditor)return;string mode=Arg("--salvage-next-validation");if(mode!="display"&&mode!="performance")return;string label=Arg("--salvage-next-validation-session");if(string.IsNullOrEmpty(label)||label.Length>40||label.Any(c=>!char.IsLetterOrDigit(c)&&c!='_'&&c!='-')){Application.Quit(2);return;}
            var driver=new GameObject("ExplicitNextWindowsValidation").AddComponent<NextWindowsProbe>();driver.mode=mode;driver.folder=Path.Combine(Path.GetDirectoryName(Application.dataPath),"Validation",label);Directory.CreateDirectory(driver.folder);driver.panel=FindAnyObjectByType<NextDisplayPanel>();driver.session=driver.panel.GetComponent<NextSession>();driver.panel.ConfigureForTests(new UnityDisplayBackend(driver.session),Path.Combine(driver.folder,"Settings"));driver.session.ConfigureStoreForTests(Path.Combine(driver.folder,"UnusedSave"));
        }
        static void Check(bool ok,string why){if(!ok)throw new InvalidOperationException(why);}
        IEnumerator Wait(Func<bool> predicate,double seconds,string why){var watch=System.Diagnostics.Stopwatch.StartNew();while(!predicate()){Check(watch.Elapsed.TotalSeconds<seconds,why);yield return null;}}
        IEnumerator Start()
        {
            report=new Report{scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,mode=mode,api=SystemInfo.graphicsDeviceType.ToString(),unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName};background=Application.runInBackground;Application.runInBackground=true;var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool more;object next=null;try{more=stack.Peek().MoveNext();if(more)next=stack.Peek().Current;}catch(Exception e){report.error=e.ToString();break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator child){stack.Push(child);continue;}yield return next;}while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();devices?.Dispose();report.inputRestored=devices==null||devices.IsRestored;Application.runInBackground=background;
            var pipe=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;report.pipeline=pipe?pipe.name:"null";report.hdr=pipe&&pipe.supportsHDR;report.displayHdr=HDROutputSettings.main!=null&&HDROutputSettings.main.active;report.width=Screen.width;report.height=Screen.height;report.useLLM=GameRuntimeOptions.UseLLM;report.actions=actions.ToArray();report.passed=string.IsNullOrEmpty(report.error)&&report.inputRestored&&report.api=="Direct3D11"&&report.unity=="6000.6.7f1"&&report.width==1920&&report.height==1080&&report.hdr&&!report.displayHdr&&!report.useLLM;File.WriteAllText(Path.Combine(folder,mode+"_Result.json"),JsonUtility.ToJson(report,true));Application.Quit(report.passed?0:1);
        }
        IEnumerator Run()
        {
            motor=session.GetComponent<SalvageMotor>();input=session.GetComponent<PlayerInputReader>();stream=session.GetComponent<NextRoomStreaming>();echo=FindAnyObjectByType<SalvageEcho>();brain=echo.GetComponent<SalvageEchoCoordinator>();yield return Wait(()=>!stream.Blocking,20,"Initial room load");motor.enabled=false;brain.enabled=false;brain.AutomaticWarnings=false;Check(brain.Initialize(),"Brain");brain.ResetBase(IntentKind.Wait);input.SetApplicationFocus(true);input.SetPointerCaptured(true);
            if(mode=="display"){devices=new SalvageR2InputScope();yield return Display();}else yield return Performance();
        }
        IEnumerator KeyPress(Key key){input.SetApplicationFocus(true);InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState());yield return null;InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState(key));yield return null;InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState());yield return null;}
        IEnumerator Click(Button button){Check(button&&button.gameObject.activeInHierarchy&&button.interactable,"Clickable UI "+(button?button.name:"null"));input.SetApplicationFocus(true);yield return new WaitForSecondsRealtime(.2f);Canvas.ForceUpdateCanvases();int clicked=0;UnityEngine.Events.UnityAction receipt=()=>clicked++;button.onClick.AddListener(receipt);try{var rect=button.GetComponent<RectTransform>();var p=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));InputSystem.QueueStateEvent(devices.Mouse,new MouseState{position=p});yield return null;InputSystem.QueueStateEvent(devices.Mouse,new MouseState{position=p}.WithButton(MouseButton.Left));yield return null;InputSystem.QueueStateEvent(devices.Mouse,new MouseState{position=p});yield return Wait(()=>clicked>0,2,"No real UI click receipt: "+button.name);Check(clicked==1,"Exactly one UI click: "+button.name);}finally{button.onClick.RemoveListener(receipt);}}
        IEnumerator ApplyAndObserve(){Check(panel.Transaction.Phase==DisplayPhase.Idle,"Previous display transaction settled");var requested=panel.Draft;yield return Click(panel.ApplyButton);Check(panel.Transaction.Phase==DisplayPhase.Applying||panel.Transaction.Phase==DisplayPhase.Confirming,"A fresh request must begin");Check(panel.Transaction.Requested.Equals(requested),"Fresh request matches selected values");yield return Wait(()=>panel.Transaction.Phase==DisplayPhase.Confirming,5,"Native display did not match: "+panel.Transaction.Message);Check(!GameRuntimeOptions.UseLLM,"LLM stays disabled");}
        IEnumerator Snap(string name){yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(folder,name+".png"),tex.EncodeToPNG());}finally{Destroy(tex);}}
        IEnumerator Display()
        {
            yield return KeyPress(Key.F10);Check(panel.IsOpen,"Actual F10");yield return Click(panel.ResolutionButtons[0]);yield return ApplyAndObserve();Check(Screen.width==1280&&Screen.height==720,"Actual720p");yield return Click(panel.ConfirmButton);Check(panel.Store.TryRead(out var saved,out _)&&saved.Width==1280,"Confirmed display persisted");actions.Add("720p_confirmed");yield return Snap("Display_720");
            yield return Click(panel.ResolutionButtons[1]);yield return ApplyAndObserve();Check(Screen.width==1600&&Screen.height==900,"Actual900p");yield return Click(panel.RevertButton);yield return Wait(()=>panel.Transaction.Phase==DisplayPhase.Idle,5,"Cancel rollback");Check(Screen.width==1280&&Screen.height==720,"Native cancel restored720p");actions.Add("900p_cancelled_native_restore");
            yield return Click(panel.ResolutionButtons[2]);yield return Click(panel.ModeButtons[1]);yield return ApplyAndObserve();Check(Screen.fullScreenMode==FullScreenMode.FullScreenWindow,"Borderless native");yield return Click(panel.ConfirmButton);actions.Add("borderless_confirmed");yield return Snap("Display_Borderless");
            yield return Click(panel.ModeButtons[2]);yield return Click(panel.ApplyButton);yield return Wait(()=>panel.Transaction.Phase==DisplayPhase.Confirming||panel.Transaction.Phase==DisplayPhase.Idle||panel.Transaction.Phase==DisplayPhase.Failed,8,"Exclusive system response");if(panel.Transaction.Phase==DisplayPhase.Confirming){Check(Screen.fullScreenMode==FullScreenMode.ExclusiveFullScreen,"Exclusive accepted");yield return Click(panel.RevertButton);actions.Add("exclusive_stably_accepted_then_cancelled");}else actions.Add("exclusive_rejected_safe_rollback");yield return Wait(()=>panel.Transaction.Phase==DisplayPhase.Idle,5,"Exclusive rollback");Check(Screen.fullScreenMode==FullScreenMode.FullScreenWindow,"Exclusive restored previous mode");
            yield return Click(panel.ModeButtons[0]);yield return ApplyAndObserve();var confirmationClock=System.Diagnostics.Stopwatch.StartNew();yield return Wait(()=>panel.Transaction.Phase==DisplayPhase.Idle,20,"15 second timeout rollback");Check(confirmationClock.Elapsed.TotalSeconds>=14.5,"Real confirmation timeout elapsed");Check(Screen.fullScreenMode==FullScreenMode.FullScreenWindow,"Timeout restored borderless");actions.Add("15s_native_timeout_restore");
            yield return Click(panel.ModeButtons[0]);yield return ApplyAndObserve();input.SetApplicationFocus(false);yield return Wait(()=>!panel.IsOpen,5,"Focus loss rollback");Check(Screen.fullScreenMode==FullScreenMode.FullScreenWindow,"Focus rollback actual mode");input.SetApplicationFocus(true);Check(panel.Open(),"Reopen");actions.Add("focus_guard_native_restore");
            yield return Click(panel.ModeButtons[0]);yield return Click(panel.QualityButtons[1]);yield return ApplyAndObserve();yield return Click(panel.ConfirmButton);yield return KeyPress(Key.F10);Check(!panel.IsOpen&&input.CanPlay,"Close restores game input");Check(Screen.width==1920&&Screen.height==1080&&Screen.fullScreenMode==FullScreenMode.Windowed,"Final1080p window");yield return Snap("Display_Final");yield return PostProcessing();
        }
        IEnumerator PostProcessing()
        {
            var shader=Shader.Find("Hidden/Universal Render Pipeline/UberPost");Check(shader&&shader.isSupported,"Actual Windows UberPost supported");var camera=motor.View.GetComponent<Camera>();var additional=camera.GetUniversalAdditionalCameraData();Check(camera.allowHDR&&!additional.allowHDROutput,"Camera internal HDR/SDR");bool previous=additional.renderPostProcessing;Color32[] before=null,after=null;
            try{additional.renderPostProcessing=false;yield return null;yield return Snap("Post_Off");var texture=ScreenCapture.CaptureScreenshotAsTexture();before=texture.GetPixels32();Destroy(texture);additional.renderPostProcessing=true;yield return null;yield return Snap("Post_On");texture=ScreenCapture.CaptureScreenshotAsTexture();after=texture.GetPixels32();Destroy(texture);}
            finally{additional.renderPostProcessing=previous;}
            Check(before.Length==after.Length,"Stable native dimensions");double difference=0;for(int i=0;i<before.Length;i++)difference+=(Math.Abs(before[i].r-after[i].r)+Math.Abs(before[i].g-after[i].g)+Math.Abs(before[i].b-after[i].b))/(3d*255);difference/=before.Length;Check(difference>1e-4,"Actual Windows post on/off image difference");actions.Add("windows_uber_supported_post_difference_"+difference.ToString("G9",System.Globalization.CultureInfo.InvariantCulture));
        }
        IEnumerator Performance()
        {
            File.WriteAllText(Path.Combine(folder,"AwaitingVisiblePlayer.json"),"{\"foregroundWindowRequired\":true,\"maximumWaitSeconds\":30}");yield return Wait(()=>Application.isFocused,30,"Actual Player must be foreground for valid GPU evidence");actions.Add("actual_player_foreground");
            var samples=new Sample[3*3*180];int offset=0;int previousRate=Application.targetFrameRate,previousVsync=QualitySettings.vSyncCount;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;actions.Add("measurement_only_60fps_vsync0");bool oldEnabled=Profiler.enabled,oldBinary=Profiler.enableBinaryLog;string oldFile=Profiler.logFile;Profiler.enabled=false;Profiler.logFile=Path.Combine(folder,"SalvageNextProfiler.raw");Profiler.enableBinaryLog=true;Profiler.enabled=true;
            try
            {
                using var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);using var memory=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory",1);using var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);Check(gc.Valid&&memory.Valid&&draws.Valid,"Required profiler counters");var timing=new FrameTiming[1];var seen=new HashSet<ulong>(2048);File.WriteAllText(Path.Combine(folder,"PerformanceWarmup.json"),"{\"visibleWindowRequired\":true,\"warmupSeconds\":10}");var warm=Time.realtimeSinceStartup+10;while(Time.realtimeSinceStartup<warm){FrameTimingManager.CaptureFrameTimings();yield return null;FrameTimingManager.GetLatestTimings(1,timing);}var locations=new[]{new Vector3(5,.1f,12),new Vector3(18,.1f,12),new Vector3(30,.1f,12)};
                for(int quality=0;quality<3;quality++)
                {
                    session.SetQuality((RecoveryQuality)quality);
                    for(int zone=0;zone<3;zone++)
                    {
                        Check(Application.isFocused,"Foreground Player required during GPU capture");
                        motor.RestorePose(locations[zone],90,0);Check(echo.Navigation.Agent.Warp(locations[zone]+Vector3.back*2),"Echo nav position");stream.EvaluateNow();yield return Wait(()=>!stream.Blocking,20,"Performance room ready");input.SetPointerCaptured(false);var stable=Time.realtimeSinceStartup+1;while(Time.realtimeSinceStartup<stable){FrameTimingManager.CaptureFrameTimings();yield return null;FrameTimingManager.GetLatestTimings(1,timing);}
                        int valid=0;for(int i=0;i<180;i++){FrameTimingManager.CaptureFrameTimings();yield return null;uint count=FrameTimingManager.GetLatestTimings(1,timing);var t=timing[0];bool ok=count>0&&t.frameStartTimestamp!=0&&t.cpuFrameTime>0&&t.gpuFrameTime>0&&seen.Add(t.frameStartTimestamp);if(ok)valid++;samples[offset++]=new Sample{quality=quality,zone=zone,valid=ok,timestamp=t.frameStartTimestamp,cpuMs=t.cpuFrameTime,gpuMs=t.gpuFrameTime,mainMs=t.cpuMainThreadFrameTime,renderMs=t.cpuRenderThreadFrameTime,deltaMs=Time.unscaledDeltaTime*1000,gcBytes=gc.LastValue,usedBytes=memory.LastValue,drawCalls=draws.LastValue};}Check(valid>=160,"Insufficient real nonzero GPU frames q="+quality+" zone="+zone);yield return Snap("Quality_"+quality+"_Zone_"+zone);actions.Add("measured_"+quality+"_"+zone+"_valid_"+valid);
                    }
                }
                report.samples=samples;Check(stream.Owner.Failures==0,"Stream failures");session.SetQuality(RecoveryQuality.Medium);
            }
            finally{report.samples=samples.Take(offset).ToArray();Profiler.enabled=false;Profiler.enableBinaryLog=oldBinary;Profiler.logFile=oldFile;Profiler.enabled=oldEnabled;Application.targetFrameRate=previousRate;QualitySettings.vSyncCount=previousVsync;report.profilerRestored=Profiler.enabled==oldEnabled&&Profiler.enableBinaryLog==oldBinary&&Profiler.logFile==oldFile&&Application.targetFrameRate==previousRate&&QualitySettings.vSyncCount==previousVsync;}
            Check(report.profilerRestored,"Profiler restoration");
        }
    }
}
#endif
