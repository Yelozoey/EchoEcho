using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using EchoStation.AI.Context;

namespace EchoStation.AI.Inference
{
 public enum OwnerState{New,Loading,Ready,Running,Stopping,Stopped,Faulted}
 public enum OwnerOutcome{Succeeded,Cancelled,Failed}
 public enum OwnerFailure{None,Cancelled,Timeout,Generation,Backend}
 public sealed class TokenRequest
 {
  public long Id {get;}public IReadOnlyList<int> Input {get;}public InferenceSnapshot Snapshot {get;}
  public TokenRequest(long id,IReadOnlyList<int> input)
  {if(id<=0)throw new ArgumentOutOfRangeException(nameof(id));QwenTensorContract.ValidateInput(input);var copy=new int[input.Count];for(var i=0;i<copy.Length;i++)copy[i]=input[i];Input=Array.AsReadOnly(copy);Id=id;}
  public TokenRequest(long id,InferenceSnapshot snapshot)
  {if(id<=0)throw new ArgumentOutOfRangeException(nameof(id));Snapshot=snapshot??throw new ArgumentNullException(nameof(snapshot));Id=id;}
 }
 public sealed class OwnerCompletion
 {
  public long RequestId {get;}public OwnerOutcome Outcome {get;}public OwnerFailure Failure {get;}public GenerationResult Generation {get;}public string Error {get;}public long ActualCompletedTick {get;}public int OwnerThreadId {get;}
  internal OwnerCompletion(long id,OwnerOutcome outcome,OwnerFailure failure,GenerationResult generation,string error,int thread,long tick){RequestId=id;Outcome=outcome;Failure=failure;Generation=generation;Error=error;OwnerThreadId=thread;ActualCompletedTick=tick;}
 }
 public interface ILocalModelRuntime:IDisposable{GenerationResult Generate(TokenRequest request,CancellationToken cancellation);}
 public interface ILocalModelFactory{ILocalModelRuntime Create(int unityMainThread,CancellationToken cancellation);}
 public readonly struct NativeActivity
 {public readonly int Calls,ThreadId;public readonly bool Active;public NativeActivity(int calls,int thread,bool active){Calls=calls;ThreadId=thread;Active=active;}}
 public interface ILocalRuntimeDiagnostics{NativeActivity Activity {get;}}
 public sealed class InferenceOwner:IDisposable
 {
  private readonly object sync=new object();private readonly ILocalModelFactory factory;private readonly int unityMain;
  private readonly AutoResetEvent wake=new AutoResetEvent(false);private readonly CancellationTokenSource shutdown=new CancellationTokenSource();
  private Thread worker;private OwnerState state;private TokenRequest pending;private OwnerCompletion completion;private string error;private bool stop,cancelPending;private CancellationTokenSource currentCancellation;
  private int ownerId;private long lastCompletion,readyTick;private double coldSeconds;private ILocalRuntimeDiagnostics diagnostics;
  public OwnerState State {get{lock(sync)return state;}}
  public int OwnerThreadId {get{lock(sync)return ownerId;}}
  public long LastActualCompletionTick {get{lock(sync)return lastCompletion;}}
  public double ColdSeconds {get{lock(sync)return coldSeconds;}}
  public long ReadyTick {get{lock(sync)return readyTick;}}
  public string StartupError {get{lock(sync)return error;}}
  public bool HasUnreadCompletion {get{lock(sync)return completion!=null;}}
  public NativeActivity Activity {get{lock(sync)return diagnostics?.Activity??default;}}
  public bool CleanupComplete {get{lock(sync)return state==OwnerState.Stopped||state==OwnerState.Faulted;}}
  public IMonotonicClock Clock {get;}
  public InferenceOwner(ILocalModelFactory runtimeFactory,int unityMainThread,IMonotonicClock clock=null){factory=runtimeFactory??throw new ArgumentNullException(nameof(runtimeFactory));if(unityMainThread<=0)throw new ArgumentOutOfRangeException(nameof(unityMainThread));Clock=clock??new StopwatchClock();if(Clock.Frequency<=0)throw new ArgumentOutOfRangeException(nameof(clock));unityMain=unityMainThread;state=OwnerState.New;}
  public void Start()
  {
   lock(sync)
   {
    if(state!=OwnerState.New)throw new InvalidOperationException("Owner is single-use");state=OwnerState.Loading;
    worker=new Thread(Run){IsBackground=true,Name="Echo-LLM-Owner",Priority=ThreadPriority.BelowNormal};worker.Start();
   }
  }
  public bool TrySubmit(TokenRequest request)
  {
   if(request==null)throw new ArgumentNullException(nameof(request));
   lock(sync){if(state!=OwnerState.Ready||stop||pending!=null||completion!=null)return false;cancelPending=false;pending=request;state=OwnerState.Running;wake.Set();return true;}
  }
  public bool CancelCurrent()
  {lock(sync){if(state!=OwnerState.Running)return false;cancelPending=true;currentCancellation?.Cancel();return true;}}
  public bool TryTakeCompletion(out OwnerCompletion result)
  {lock(sync){result=completion;if(result==null)return false;completion=null;return true;}}
  public void StopAsync()
  {
   lock(sync)
   {
    if(state==OwnerState.Stopped||state==OwnerState.Faulted||stop)return;stop=true;
    if(state==OwnerState.New){wake.Dispose();shutdown.Dispose();state=OwnerState.Stopped;return;}
    state=OwnerState.Stopping;shutdown.Cancel();wake.Set();
   }
  }
  private void Run()
  {
   ILocalModelRuntime runtime=null;Exception fatal=null;
   try
   {
    var thread=Thread.CurrentThread.ManagedThreadId;if(thread==unityMain)throw new InvalidOperationException("Owner must be background");lock(sync)ownerId=thread;
    var cold=Stopwatch.StartNew();runtime=factory.Create(unityMain,shutdown.Token);if(runtime==null)throw new InvalidOperationException("Model factory returned null");shutdown.Token.ThrowIfCancellationRequested();
    lock(sync){diagnostics=runtime as ILocalRuntimeDiagnostics;coldSeconds=cold.Elapsed.TotalSeconds;readyTick=Clock.Now;if(!stop)state=OwnerState.Ready;}
    for(;;)
    {
     TokenRequest request;lock(sync){if(stop)break;request=pending;pending=null;if(request!=null){currentCancellation=CancellationTokenSource.CreateLinkedTokenSource(shutdown.Token);if(cancelPending)currentCancellation.Cancel();}}
     if(request==null){wake.WaitOne();continue;}
     var outcome=OwnerOutcome.Succeeded;var failure=OwnerFailure.None;GenerationResult generated=null;string detail=null;
     try{currentCancellation.Token.ThrowIfCancellationRequested();generated=runtime.Generate(request,currentCancellation.Token);if(generated==null)throw new InvalidOperationException("Runtime returned null");currentCancellation.Token.ThrowIfCancellationRequested();}
     catch(OperationCanceledException){outcome=OwnerOutcome.Cancelled;failure=OwnerFailure.Cancelled;detail="cancelled";generated=null;}
     catch(GenerationException fault){outcome=OwnerOutcome.Failed;failure=fault.Code==GenerationFault.Timeout?OwnerFailure.Timeout:OwnerFailure.Generation;detail=fault.ToString();generated=null;}
     catch(Exception fault){outcome=OwnerOutcome.Failed;failure=OwnerFailure.Backend;detail=fault.ToString();generated=null;fatal=fault;}
     lock(sync)
     {
      // Cancellation wins even if it arrives after Generate's last check but before publication.
      if(cancelPending||stop){outcome=OwnerOutcome.Cancelled;failure=OwnerFailure.Cancelled;generated=null;detail="cancelled";}
      currentCancellation.Dispose();currentCancellation=null;
      completion=new OwnerCompletion(request.Id,outcome,failure,generated,detail,thread,Clock.Now);lastCompletion=completion.ActualCompletedTick;if(fatal!=null)state=OwnerState.Stopping;else if(!stop)state=OwnerState.Ready;
     }
     if(fatal!=null)break;
    }
   }
   catch(OperationCanceledException){}
   catch(Exception fault){fatal=fault;}
   finally
   {
    try{runtime?.Dispose();}catch(Exception disposal){fatal=fatal??disposal;}
    lock(sync){stop=true;pending=null;diagnostics=null;currentCancellation?.Dispose();currentCancellation=null;wake.Dispose();shutdown.Dispose();if(lastCompletion>0)lastCompletion=Math.Max(lastCompletion,Clock.Now);error=fatal?.ToString();state=fatal==null?OwnerState.Stopped:OwnerState.Faulted;}
   }
  }
  public void Dispose()=>StopAsync();
 }
}
