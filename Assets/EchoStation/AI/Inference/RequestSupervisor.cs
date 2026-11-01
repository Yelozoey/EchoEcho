using System;
using System.Threading;

namespace EchoStation.AI.Inference
{
 public enum RequestPhase{New,Loading,Ready,Running,Draining,Stopping,Stopped,Faulted}
 public enum RequestOutcome{Succeeded,Cancelled,RequestTimeout,ColdTimeout,GenerationFailed,BackendFailed,StartupFailed}
 public sealed class RequestCompletion
 {
  public long RequestId {get;}public RequestOutcome Outcome {get;}public GenerationResult Generation {get;}public string Error {get;}public long ActualCompletedTick {get;}
  internal RequestCompletion(long id,RequestOutcome outcome,GenerationResult generation,string error,long completed){RequestId=id;Outcome=outcome;Generation=generation;Error=error;ActualCompletedTick=completed;}
 }
 // Main-thread control/data boundary only. Poll never waits for a worker or native resource.
 public sealed class RequestSupervisor:IDisposable
 {
  private readonly InferenceOwner owner;private readonly InferenceScheduler scheduler;private readonly IMonotonicClock clock;private readonly int controlThread;
  private bool started,loading,notified,draining,stopping;private long coldDeadline,requestDeadline,active;private RequestCompletion completion;
  public InferenceOwner Owner=>owner;
  public CompletionCooldown Cooldown=>scheduler.Cooldown;
  public bool HasNotification=>completion!=null;
  public RequestPhase Phase
  {
   get{EnsureThread();if(owner.CleanupComplete)return owner.State==OwnerState.Faulted?RequestPhase.Faulted:RequestPhase.Stopped;if(!started)return RequestPhase.New;if(stopping||owner.State==OwnerState.Stopping)return RequestPhase.Stopping;if(loading)return RequestPhase.Loading;if(draining)return RequestPhase.Draining;return active>0?RequestPhase.Running:RequestPhase.Ready;}
  }
  public RequestSupervisor(InferenceOwner service,CompletionCooldown sharedCooldown=null)
  {owner=service??throw new ArgumentNullException(nameof(service));clock=owner.Clock;if(clock.Frequency>long.MaxValue/60||clock.Now<0)throw new ArgumentOutOfRangeException(nameof(service));scheduler=new InferenceScheduler(owner,sharedCooldown);controlThread=Thread.CurrentThread.ManagedThreadId;}
  public void Start()
  {EnsureThread();if(started)throw new InvalidOperationException("Supervisor is single-use");coldDeadline=Deadline(30);started=true;loading=true;owner.Start();}
  public Admission TrySubmit(TokenRequest request)
  {
   EnsureThread();Poll();if(completion!=null||active>0)return Admission.Busy;if(!started||loading||stopping)return Admission.NotReady;
   var deadline=Deadline(20);var result=scheduler.TrySubmit(request);if(result==Admission.Accepted){active=request.Id;requestDeadline=deadline;notified=false;draining=false;}return result;
  }
  public void Poll()
  {
   EnsureThread();if(!started)return;scheduler.ObserveCompletion();
   if(loading)
   {
    if(owner.State==OwnerState.Ready)
    {if(owner.ReadyTick>=coldDeadline)ColdTimeout();else loading=false;}
    else if(owner.CleanupComplete)
    {loading=false;if(!stopping)Publish(0,RequestOutcome.StartupFailed,null,owner.StartupError,0);}
    else if(clock.Now>=coldDeadline)ColdTimeout();
   }
   if(active==0)return;
   if(scheduler.TryTakeCompletion(out var result))
   {
    if(result.RequestId!=active)throw new InvalidOperationException("Owner returned an unrelated request");
    if(!notified)
    {
     if(result.ActualCompletedTick>=requestDeadline)Publish(active,RequestOutcome.RequestTimeout,null,"request-timeout",result.ActualCompletedTick);
     else
     {
      var outcome=result.Outcome==OwnerOutcome.Succeeded?RequestOutcome.Succeeded:result.Outcome==OwnerOutcome.Cancelled?RequestOutcome.Cancelled:result.Failure==OwnerFailure.Timeout?RequestOutcome.RequestTimeout:result.Failure==OwnerFailure.Backend?RequestOutcome.BackendFailed:RequestOutcome.GenerationFailed;
      Publish(active,outcome,outcome==RequestOutcome.Succeeded?result.Generation:null,result.Error,result.ActualCompletedTick);
     }
    }
    active=0;draining=false;return;
   }
   if(owner.CleanupComplete)
   {if(!notified)Publish(active,RequestOutcome.BackendFailed,null,owner.StartupError,owner.LastActualCompletionTick);active=0;draining=false;return;}
   if(!notified&&clock.Now>=requestDeadline)
   {Publish(active,RequestOutcome.RequestTimeout,null,"request-timeout",0);notified=true;draining=true;owner.CancelCurrent();}
  }
  public bool CancelCurrent()
  {
   EnsureThread();if(active==0||notified)return false;
   Publish(active,RequestOutcome.Cancelled,null,"cancelled",0);notified=true;draining=true;owner.CancelCurrent();return true;
  }
  public bool TryTakeCompletion(out RequestCompletion result)
  {EnsureThread();Poll();result=completion;if(result==null)return false;completion=null;return true;}
  public void StopAsync()
  {EnsureThread();if(stopping)return;CancelCurrent();stopping=true;loading=false;owner.StopAsync();}
  private void ColdTimeout()
  {Publish(0,RequestOutcome.ColdTimeout,null,"cold-timeout",0);loading=false;stopping=true;owner.StopAsync();}
  private void Publish(long id,RequestOutcome outcome,GenerationResult generation,string error,long completed)
  {if(completion!=null)throw new InvalidOperationException("Notification must be consumed before another terminal result");completion=new RequestCompletion(id,outcome,generation,error,completed);}
  private long Deadline(int seconds){var now=clock.Now;var ticks=clock.Frequency*seconds;return now>long.MaxValue-ticks?long.MaxValue:now+ticks;}
  private void EnsureThread(){if(Thread.CurrentThread.ManagedThreadId!=controlThread)throw new InvalidOperationException("Supervisor belongs to its control thread");}
  public void Dispose()=>StopAsync();
 }
}
