using System;
using System.Threading;
using EchoStation.AI.Context;
using EchoStation.AI.Execution;
using EchoStation.AI.Inference;
using EchoStation.Dialogue;
using EchoStation.Dialogue.Integration;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Echo;
using EchoStation.Station;
using EchoStation.Story;

namespace EchoStation.AI.GameIntegration
{
 // Main-thread adapter. The factory/Owner receives immutable snapshots, never this adapter.
 public sealed class StationLlmSession:IDisposable
 {
  private readonly int main;private readonly StationSession station;private readonly ScriptDialogueController dialogue;private readonly StationInferenceSnapshotSource source;private readonly ScriptKnowledgeResponder knowledge;
  private readonly InferenceSessionHost host;private readonly IMonotonicClock clock;private readonly UseLlmRouting routing=new UseLlmRouting();private readonly Func<DialogueMessage,bool> interceptor;
  private InferenceSnapshot active;private bool applying,disposed;private string failure;
  internal bool ApplyingTrustedResult=>applying;
  public bool Enabled=>routing.Enabled;public int ConsecutiveFaults=>routing.ConsecutiveFaults;public bool AutomaticallyDisabled=>routing.AutomaticallyDisabled;
  public bool CleanupComplete=>host.CleanupComplete;public RequestPhase Phase=>host.Phase;public NativeActivity Activity=>host.Activity;public int OwnerThreadId=>host.OwnerThreadId;
  public int DeliveredGenerationCount {get;private set;}public string LastRawOutput {get;private set;}public IntentApplicationResult LastApplication {get;private set;}public double LastInferenceSeconds {get;private set;}
  public FeedbackPhase Feedback
  {
   get
   {
    if(!Enabled)return CleanupComplete?FeedbackPhase.Script:FeedbackPhase.Draining;
    switch(host.Phase)
    {
     case RequestPhase.Loading:case RequestPhase.New:return FeedbackPhase.Loading;
     case RequestPhase.Running:return FeedbackPhase.Thinking;
     case RequestPhase.Draining:case RequestPhase.Stopping:return FeedbackPhase.Draining;
     case RequestPhase.Faulted:case RequestPhase.Stopped:return FeedbackPhase.Failure;
     default:return failure!=null?FeedbackPhase.Failure:host.Cooldown.Ready(clock.Now)?FeedbackPhase.Ready:FeedbackPhase.CoolingDown;
    }
   }
  }
  public StationLlmSession(StationSession session,ILocalModelFactory factory,int unityMainThread,IMonotonicClock monotonicClock=null)
  {
   if(unityMainThread<=0||Thread.CurrentThread.ManagedThreadId!=unityMainThread)throw new InvalidOperationException("LLM game adapter belongs to the Unity thread");main=unityMainThread;
   if(!session||!session.Ready||session.Knowledge==null||!session.Knowledge.IsReady)throw new ArgumentException("Ready station and known-memory bridge required");station=session;dialogue=station.GetComponent<ScriptDialogueController>();
   if(!dialogue||dialogue.InputInterceptor!=null)throw new InvalidOperationException("Dialogue input already has an owner");clock=monotonicClock??new StopwatchClock();host=new InferenceSessionHost(factory,main,clock);source=new StationInferenceSnapshotSource(station,main);
   knowledge=new ScriptKnowledgeResponder(station.Memories,StationScript.CreateSearch(),StationScript.Hints,new[]{"7316","0428"},()=>station.Elapsed);
   interceptor=HandleInput;dialogue.InputInterceptor=interceptor;dialogue.PlayerIntentRequested+=OnManualIntent;
  }
  public void SetEnabled(bool enabled)
  {
   EnsureThread();if(disposed){if(enabled)throw new ObjectDisposedException(nameof(StationLlmSession));return;}
   if(Enabled==enabled)return;routing.SetEnabled(enabled);active=null;failure=null;host.SetEnabled(enabled);
  }
  private bool HandleInput(DialogueMessage message)
  {
   EnsureThread();if(disposed||!Enabled)return false;Poll();if(!Enabled)return false;
   if(host.Phase!=RequestPhase.Ready)
   {Speak(host.Phase==RequestPhase.Running||host.Phase==RequestPhase.Draining?"我正在处理上一条请求，请稍候或取消。":"本地AI尚未就绪。你可以稍候或切回脚本模式。");return true;}
   StationInferenceCapture captured;
   try{captured=source.Capture(message.Text);}catch(ArgumentException){Speak("当前记录或目标不可用，请稍后重试。");return true;}catch(InvalidOperationException){Speak("当前游戏状态尚未就绪，请稍后重试。");return true;}
   var admitted=host.TrySubmit(captured.Snapshot,out _);
   if(admitted==Admission.Accepted){active=captured.Snapshot;failure=null;LastApplication=null;LastRawOutput=null;}
   else Speak(admitted==Admission.CoolingDown?"请稍候，再发送下一条指令。":"我正在处理上一条请求，请稍候或取消。");
   return true;
  }
  public void Poll()
  {
   EnsureThread();host.Poll();if(disposed)return;
   if(station.Restoring){if(active!=null)Invalidate(LifecycleChange.LoadGame);return;}
   if(!host.TryTakeCompletion(out var completed))return;
   if(completed.Completion.Outcome!=RequestOutcome.Succeeded)
   {
    active=null;if(completed.Completion.Outcome==RequestOutcome.Cancelled)return;
    var timeout=completed.Completion.Outcome==RequestOutcome.RequestTimeout||completed.Completion.Outcome==RequestOutcome.ColdTimeout;
    Fail(timeout?"本地AI响应超时，该请求已取消。":"本地AI未能完成请求。你可以重试或切回脚本模式。");return;
   }
   var submitted=active;active=null;if(submitted==null)return;
   StationInferenceCapture live;
   try{live=source.Capture(submitted.Input);}catch(InvalidOperationException){host.Invalidate(LifecycleChange.WorldChanged);Speak("游戏状态已变化，请重新发送指令。");return;}catch(ArgumentException){host.Invalidate(LifecycleChange.WorldChanged);Speak("当前目标信息不可用，请重新发送指令。");return;}
   DeliveredGenerationCount++;var generation=completed.Completion.Generation;LastRawOutput=generation?.Text;LastInferenceSeconds=generation==null?0:generation.Seconds+generation.PreparationSeconds;
   LastApplication=LlmIntentApplication.Apply(host,completed,submitted,live.Snapshot.Targets,live.Catalog,knowledge,station.Dialogue.Session.Version,Deliver);
   if(LastApplication.IsFault)Fail(LastApplication.Status==ApplicationStatus.ParseFallback?null:"本地AI返回的上下文无效，请重新发送指令。");
   else if(LastApplication.Status!=ApplicationStatus.Stale){routing.RecordSuccess();failure=null;}
  }
  public void Cancel()
  {
   EnsureThread();if(disposed||!Enabled)return;
   if(host.Phase==RequestPhase.Loading){SetEnabled(false);Speak("已取消本地AI加载，后续输入使用脚本模式。");return;}
   if(host.Phase!=RequestPhase.Running)return;Invalidate(LifecycleChange.ManualOverride);Speak("已取消这条请求。完成资源清理后可以重新发送。");
  }
  public void Invalidate(LifecycleChange reason)
  {EnsureThread();if(disposed)return;active=null;failure=null;host.Invalidate(reason);if(reason==LifecycleChange.SceneUnloaded)routing.SetEnabled(false);}
  private void OnManualIntent(IntentRequest request){if(!applying&&!disposed)Invalidate(LifecycleChange.ManualOverride);}
  private bool Deliver(IntentRequest request)
  {applying=true;try{return dialogue.SubmitPlayerIntent(request);}finally{applying=false;}}
  private void Speak(string line)=>Deliver(ScriptCommands.Answer(line));
  private void Fail(string message)
  {
   failure=message??"invalid-json";if(message!=null)Speak(message);
   if(routing.RecordFault()){active=null;host.SetEnabled(false);Speak("本地AI连续三次未能完成请求，已切回脚本模式。失败指令不会自动执行；请重新输入。");}
  }
  private void EnsureThread(){if(Thread.CurrentThread.ManagedThreadId!=main)throw new InvalidOperationException("LLM game adapter is main-thread only");}
  public void Dispose()
  {
   EnsureThread();if(disposed)return;disposed=true;active=null;routing.SetEnabled(false);
   if(dialogue){if(dialogue.InputInterceptor==interceptor)dialogue.InputInterceptor=null;dialogue.PlayerIntentRequested-=OnManualIntent;}
   host.Dispose();
  }
 }
}
