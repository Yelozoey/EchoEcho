using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Dialogue;
using EchoStation.Echo;
using EchoStation.Persistence;
using EchoStation.Player;
using EchoStation.Puzzles;
using EchoStation.Station;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
namespace EchoStation.Tests.PlayerFixtures
{
 public sealed class S2WindowProbe:MonoBehaviour
 {
  [Serializable]private sealed class Result
  {public bool passed,inputRestored,hdr,displayHdr,useLlm,chinese,modal,limit,history,scroll,display,mouseLook,torch,crossProcess,slotRetained;public string mode,phase,error,unity,api,pipeline,cpu,gpu,os,inputEvidence;public int width,height,ramMb,vramMb;public double playerDelta,echoDelta,clockDelta;public StationRouteDriver.Result route;}
  private StationSession station;private Keyboard keyboard;private Mouse mouse;private StationRouteDriver driver;private string folder,phase;private bool running;private Result report;
  private static string Arg(string flag){var args=Environment.GetCommandLineArgs();var i=Array.IndexOf(args,flag);return i>=0&&i+1<args.Length?args[i+1]:null;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Install(){if(!Application.isEditor&&Arg("--s2-window-report")!=null)new GameObject("S2_Window_CLI_Probe").AddComponent<S2WindowProbe>();}
  private void Update(){if(running&&station)station.GetComponent<PlayerInputReader>().SetApplicationFocus(true);}
  private IEnumerator Start()
  {
   var path=Path.GetFullPath(Arg("--s2-window-report"));folder=Path.GetDirectoryName(path);Directory.CreateDirectory(folder);Application.runInBackground=true;var settings=InputSystem.settings;var background=settings.backgroundBehavior;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();running=true;
   report=new Result{mode=Arg("--s2-window-mode"),unity=Application.unityVersion,api=SystemInfo.graphicsDeviceType.ToString(),cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,os=SystemInfo.operatingSystem,ramMb=SystemInfo.systemMemorySize,vramMb=SystemInfo.graphicsMemorySize,inputEvidence="Windows Player actual InputSystem keyboard/mouse/text and product UI; not OS IME candidate-window verification"};
   var stack=new Stack<IEnumerator>();stack.Push(Run());while(stack.Count>0){bool more;object current=null;try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){report.error=e.ToString();break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;}
   while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();driver?.Dispose();report.inputRestored=driver!=null&&driver.Report.inputRestored;if(keyboard.added)InputSystem.RemoveDevice(keyboard);if(mouse.added)InputSystem.RemoveDevice(mouse);settings.backgroundBehavior=background;report.inputRestored&=InputSystem.settings==settings&&settings.backgroundBehavior==background;running=false;report.phase=phase;report.passed&=report.inputRestored&&string.IsNullOrEmpty(report.error);File.WriteAllText(path,JsonUtility.ToJson(report,true));Application.Quit(report.passed?0:1);
  }
  private static void Check(bool v,string why){if(!v)throw new InvalidOperationException(why);}
  private IEnumerator Frame(){yield return null;}
  private IEnumerator Until(Func<bool> condition,float seconds,string why){var end=Time.realtimeSinceStartup+seconds;while(!condition()){if(Time.realtimeSinceStartup>end)throw new TimeoutException(why);yield return null;}}
  private IEnumerator Type(string text){foreach(var c in text)InputSystem.QueueTextEvent(keyboard,c);yield return null;yield return null;}
  private IEnumerator Snap(string name){yield return new WaitForEndOfFrame();var tex=ScreenCapture.CaptureScreenshotAsTexture();try{File.WriteAllBytes(Path.Combine(folder,name+".png"),tex.EncodeToPNG());}finally{Destroy(tex);}}
  private Button Button(string canvas,string name)=>station.gameObject.scene.GetRootGameObjects().Single(r=>r.name==canvas).GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
  private IEnumerator Run()
  {
   phase="startup";yield return Until(()=>SplashScreen.isFinished,15,"Splash");yield return Until(()=>{station=FindAnyObjectByType<StationSession>();return station&&station.Ready;},15,"Station startup");yield return Until(()=>EventSystem.current&&EventSystem.current.currentInputModule,5,"Input module");var input=station.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);station.GetComponent<StationPersistence>().ConfigureDirectory(Path.GetFullPath(Arg("--s2-window-save-dir")));driver=new StationRouteDriver(station,keyboard,mouse,Frame,folder);station.Echo.AutomaticWarningsEnabled=false;
   var pipe=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;var camera=station.Player.View.GetComponent<Camera>();report.width=Screen.width;report.height=Screen.height;report.pipeline=pipe?pipe.name:"null";report.hdr=pipe&&pipe.supportsHDR&&camera.allowHDR;report.displayHdr=HDROutputSettings.main!=null&&HDROutputSettings.main.active;report.useLlm=GameRuntimeOptions.UseLLM;Check(report.unity=="6000.6.7f1"&&report.api=="Direct3D11"&&report.width==1920&&report.height==1080&&report.pipeline=="ES_S2_ForwardHDR"&&report.hdr&&!report.displayHdr&&!camera.GetUniversalAdditionalCameraData().allowHDROutput&&!report.useLlm,"Rendering baseline");
   if(report.mode=="explore")yield return Explore();else if(report.mode=="resume")yield return Resume();else throw new InvalidOperationException("Unknown test mode");phase="complete";report.passed=true;
  }
  private IEnumerator Explore()
  {
   phase="look_torch";var yaw=station.Player.Yaw;InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(90,0));yield return null;yield return null;report.mouseLook=Mathf.Abs(Mathf.DeltaAngle(yaw,station.Player.Yaw))>1;Check(report.mouseLook,"Mouse delta did not turn player");var torch=station.GetComponent<PlayerFlashlight>();var oldTorch=torch.IsOn;yield return driver.KeyPress(Key.F);report.torch=torch.IsOn!=oldTorch;Check(report.torch,"F did not toggle torch");
   phase="archive_route";driver.Checkpoint=Snap;yield return driver.Run("archive_exit");report.route=driver.Report;Check(report.route.passed,"Archive route");
   phase="dialogue_native";inputCapture();yield return driver.KeyPress(Key.T);var panel=station.Dialogue;yield return Until(()=>panel.InputField.isFocused,5,"Text focus");var before=panel.Session.Messages.Last().Number;var text="通道终端的密码是多少";var eventCount=panel.InputField.ReceivedTextEvents;yield return Type(text);report.chinese=panel.InputField.text==text&&panel.InputField.ReceivedTextEvents-eventCount==text.Length;Check(report.chinese,"Chinese text route");yield return driver.Click(panel.SendButton);Check(panel.Session.Messages.Last().Number>before&&panel.Session.Messages.Last().Text.Contains("0428"),"Known password reply");
   var position=station.transform.position;yield return driver.KeyPress(Key.W);yield return driver.KeyPress(Key.E);yield return driver.KeyPress(Key.J);yield return driver.KeyPress(Key.Tab);report.modal=panel.IsOpen&&station.GetComponent<PlayerInputReader>().UiBlocked&&!station.GetComponent<JournalPanel>().IsOpen&&!station.GetComponent<StationControls>().IsOpen&&Vector3.Distance(position,station.transform.position)<.02f;Check(report.modal,"Modal isolation");
   yield return Type(new string('测',210));report.limit=panel.InputField.text.Length==200;Check(report.limit,"200 character cap");panel.InputField.text="";
   for(var i=0;i<110;i++)panel.Session.TryAdd(DialogueSpeaker.Echo,"容量夹具"+i+"：等待后续资料复核。",false,out _);yield return null;yield return null;report.history=panel.DisplayedCount==100&&panel.AllocatedRows==100;Check(report.history,"History cap");panel.MessageScroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();yield return null;var rect=panel.MessageScroll.viewport;var point=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return null;InputSystem.QueueStateEvent(mouse,new MouseState{position=point,scroll=new Vector2(0,120)});yield return null;yield return null;report.scroll=panel.MessageScroll.verticalNormalizedPosition>0;Check(report.scroll,"Actual wheel did not scroll history");yield return Snap("DialogueHistory");yield return driver.KeyPress(Key.Escape);Check(!panel.IsOpen,"Escape close");
   phase="display_settings";yield return driver.KeyPress(Key.Tab);yield return driver.Click(Button("ES_S2Controls","Res0"));yield return Until(()=>Screen.width==1280&&Screen.height==720,5,"720p resize");yield return driver.Click(Button("ES_S2Controls","Res2"));yield return Until(()=>Screen.width==1920&&Screen.height==1080,5,"1080p resize");yield return driver.Click(Button("ES_S2Controls","Fullscreen"));yield return Until(()=>Screen.fullScreenMode==FullScreenMode.FullScreenWindow,5,"Fullscreen");yield return driver.Click(Button("ES_S2Controls","Fullscreen"));yield return Until(()=>Screen.fullScreenMode==FullScreenMode.Windowed&&Screen.width==1920&&Screen.height==1080,5,"Window restore");report.display=true;yield return Snap("DisplaySettings");yield return driver.Click(Button("ES_S2Controls","Close"));
   report.slotRetained=station.GetComponent<StationPersistence>().Files.Read().Data.MemoryJson==driver.SavedData.MemoryJson;Check(report.slotRetained,"Capacity fixture altered manual slot");
  }
  private void inputCapture(){var input=station.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);}
  private IEnumerator Resume()
  {
   phase="cross_process_read";var persistence=station.GetComponent<StationPersistence>();var saved=persistence.Files.Read();Check(saved.Success,"A process slot missing");var bytes=File.ReadAllBytes(persistence.Files.PrimaryPath);var expected=saved.Data;Check(station.Progress.Capture().Length==0,"New process not empty");yield return driver.KeyPress(Key.F9);yield return Until(()=>!persistence.Busy&&!station.GetComponent<StationLifecycle>().IsChangingSession,8,"Cross process load");Check(persistence.LastSucceeded,persistence.LastMessage);var actual=persistence.Capture();report.playerDelta=Vector3.Distance(new Vector3(expected.Player.X,expected.Player.Y,expected.Player.Z),station.transform.position);report.echoDelta=Vector3.Distance(new Vector3(expected.Echo.X,expected.Echo.Y,expected.Echo.Z),station.Echo.transform.position);report.clockDelta=actual.Elapsed-expected.Elapsed;
   Check(report.playerDelta<.02&&report.echoDelta<.12&&Mathf.Abs(Mathf.DeltaAngle(actual.Player.Yaw,expected.Player.Yaw))<.01f&&Math.Abs(actual.Player.Pitch-expected.Player.Pitch)<.01f&&actual.Flashlight==expected.Flashlight,"Actor/look/torch restore");Check(actual.MemoryJson==expected.MemoryJson&&SameProgress(actual.Progress,expected.Progress)&&actual.Dialogue.Select(m=>(m.Number,m.Speaker,m.Text,m.IsRefusal)).SequenceEqual(expected.Dialogue.Select(m=>(m.Number,m.Speaker,m.Text,m.IsRefusal))),"World/memory/history mismatch");Check(actual.Progress.Completed.Length==6&&actual.Progress.Scans.Contains("scan_mark_01")&&actual.Progress.Scans.Contains("scan_lab_calibration_01")&&ProgressAudit.Validate(actual.Progress)==null&&report.clockDelta>=0&&report.clockDelta<2,"Restored consistency");report.slotRetained=File.ReadAllBytes(persistence.Files.PrimaryPath).SequenceEqual(bytes);Check(report.slotRetained,"Read changed slot");report.crossProcess=true;yield return Snap("CrossProcessRestored");inputCapture();yield return driver.KeyPress(Key.J);Check(station.GetComponent<JournalPanel>().IsOpen,"Restored journal unavailable");yield return Snap("RestoredJournal");yield return driver.KeyPress(Key.Escape);
  }
  private static bool SameProgress(ProgressState a,ProgressState b)=>a.Completed.SequenceEqual(b.Completed)&&a.Journal.SequenceEqual(b.Journal)&&a.Scans.SequenceEqual(b.Scans)&&a.Events.SequenceEqual(b.Events)&&a.Zone==b.Zone&&a.AccessLevel==b.AccessLevel&&a.CabinOpen==b.CabinOpen&&a.CoreOpen==b.CoreOpen&&a.VentOpen==b.VentOpen&&a.Powered==b.Powered&&Math.Abs(a.VentElapsed-b.VentElapsed)<.001f;
 }
}
