using System;
using System.Collections.Generic;
using System.Threading;
using EchoStation.AI.Context;

namespace EchoStation.AI.Inference
{
 public sealed class StampedCompletion
 {
  public RequestStamp Stamp {get;}public RequestCompletion Completion {get;}
  internal StampedCompletion(RequestStamp stamp,RequestCompletion completion){Stamp=stamp;Completion=completion;}
 }
 // Single control thread, at most one live Owner. No Unity references or game execution here.
 public sealed class InferenceSessionHost:IDisposable
 {
  private readonly ILocalModelFactory factory;private readonly int unityMain,controlThread;private readonly IMonotonicClock clock;private readonly CompletionCooldown cooldown;private readonly SessionFence fence=new SessionFence();
  private RequestSupervisor supervisor;private RequestStamp? active;private StampedCompletion pending;private bool enabled,restart,disposed;private long claimed,delivered;
  public bool Enabled=>enabled;
  public long Session=>fence.Session;
  public long Revision=>fence.Revision;
  public bool CleanupComplete=>supervisor==null||supervisor.Owner.CleanupComplete;
  public RequestPhase Phase=>supervisor?.Phase??RequestPhase.New;
  public NativeActivity Activity=>supervisor?.Owner.Activity??default;
  public int OwnerThreadId=>supervisor?.Owner.OwnerThreadId??0;
  public CompletionCooldown Cooldown=>cooldown;
  public InferenceSessionHost(ILocalModelFactory runtimeFactory,int unityMainThread,IMonotonicClock monotonicClock=null)
  {factory=runtimeFactory??throw new ArgumentNullException(nameof(runtimeFactory));if(unityMainThread<=0)throw new ArgumentOutOfRangeException(nameof(unityMainThread));unityMain=unityMainThread;clock=monotonicClock??new StopwatchClock();cooldown=new CompletionCooldown(clock.Frequency);controlThread=Thread.CurrentThread.ManagedThreadId;}
  public void SetEnabled(bool value)
  {
   EnsureThread();if(disposed){if(value)throw new ObjectDisposedException(nameof(InferenceSessionHost));return;}if(enabled==value)return;
   enabled=value;fence.Change(value?LifecycleChange.LLMEnabled:LifecycleChange.LLMDisabled);DropDelivery();restart=value;if(!value)supervisor?.StopAsync();Poll();
  }
  public void Invalidate(LifecycleChange reason)
  {
   EnsureThread();ThrowIfDisposed();if(reason==LifecycleChange.LLMEnabled){SetEnabled(true);return;}if(reason==LifecycleChange.LLMDisabled){SetEnabled(false);return;}
   fence.Change(reason);DropDelivery();if(reason==LifecycleChange.SceneUnloaded){enabled=false;restart=false;supervisor?.StopAsync();}Poll();
  }
  // Explicit recovery, never an automatic polling retry loop. The caller reports the failed input.
  public bool Restart()
  {EnsureThread();ThrowIfDisposed();if(!enabled)return false;fence.Change(LifecycleChange.WorldChanged);DropDelivery();supervisor?.StopAsync();restart=true;Poll();return true;}
  public Admission TrySubmit(IReadOnlyList<int> input,out RequestStamp stamp)
  {return TrySubmitCore(input,null,out stamp);}
  public Admission TrySubmit(InferenceSnapshot snapshot,out RequestStamp stamp)
  {if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));return TrySubmitCore(null,snapshot,out stamp);}
  private Admission TrySubmitCore(IReadOnlyList<int> input,InferenceSnapshot snapshot,out RequestStamp stamp)
  {
   EnsureThread();ThrowIfDisposed();stamp=default;Poll();if(!enabled||supervisor==null)return Admission.NotReady;if(pending!=null)return Admission.Busy;
   var candidate=checked(fence.LastRequest+1);var result=supervisor.TrySubmit(snapshot==null?new TokenRequest(candidate,input):new TokenRequest(candidate,snapshot));
   if(result==Admission.Accepted){stamp=fence.Issue();active=stamp;delivered=0;}return result;
  }
  public void Poll()
  {
   EnsureThread();
   if(supervisor!=null)
   {
    supervisor.Poll();if(supervisor.TryTakeCompletion(out var result))
    {
     if(enabled&&!disposed)
     {
      if(result.RequestId==0)Store(new StampedCompletion(default,result));
      else if(active.HasValue&&active.Value.Request==result.RequestId&&IsCurrent(active.Value))Store(new StampedCompletion(active.Value,result));
     }
     if(result.RequestId!=0)active=null;
    }
    if(supervisor.Owner.CleanupComplete&&supervisor.Owner.LastActualCompletionTick>0)cooldown.Observe(supervisor.Owner.LastActualCompletionTick);
   }
   if(!enabled||disposed||!restart)return;
   if(supervisor!=null&&!supervisor.Owner.CleanupComplete)return;
   supervisor=new RequestSupervisor(new InferenceOwner(factory,unityMain,clock),cooldown);restart=false;supervisor.Start();
  }
  public bool TryTakeCompletion(out StampedCompletion result)
  {EnsureThread();Poll();result=pending;if(result==null)return false;pending=null;if(result.Completion.Outcome==RequestOutcome.Succeeded&&result.Completion.Generation!=null)delivered=result.Stamp.Request;return true;}
  public bool IsCurrent(RequestStamp stamp)
  {EnsureThread();return enabled&&!disposed&&claimed!=stamp.Request&&fence.IsCurrent(stamp);}
  public bool TryClaim(RequestStamp stamp)
  {EnsureThread();if(delivered!=stamp.Request||!IsCurrent(stamp))return false;claimed=stamp.Request;return true;}
  private void Store(StampedCompletion result)
  {if(pending!=null)throw new InvalidOperationException("Host notification must be consumed");pending=result;}
  private void DropDelivery()
  {pending=null;active=null;delivered=0;if(supervisor==null)return;supervisor.CancelCurrent();supervisor.TryTakeCompletion(out _);}
  private void EnsureThread(){if(Thread.CurrentThread.ManagedThreadId!=controlThread)throw new InvalidOperationException("Session host belongs to its control thread");}
  private void ThrowIfDisposed(){if(disposed)throw new ObjectDisposedException(nameof(InferenceSessionHost));}
  public void Dispose()
  {EnsureThread();if(disposed)return;SetEnabled(false);supervisor?.StopAsync();disposed=true;restart=false;pending=null;active=null;}
 }
}
