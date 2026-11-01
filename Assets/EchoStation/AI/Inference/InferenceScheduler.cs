using System;
using System.Diagnostics;

namespace EchoStation.AI.Inference
{
 public interface IMonotonicClock{long Now {get;}long Frequency {get;}}
 public sealed class StopwatchClock:IMonotonicClock{public long Now=>Stopwatch.GetTimestamp();public long Frequency=>Stopwatch.Frequency;}
 public sealed class CompletionCooldown
 {
  private readonly long ticks;private long last;private bool observed;
  public long Frequency {get;}
  public long LastObserved=>observed?last:0;
  public CompletionCooldown(long frequency)
  {if(frequency<1||frequency>(long.MaxValue-9)/3)throw new ArgumentOutOfRangeException(nameof(frequency));Frequency=frequency;ticks=(frequency*3+9)/10;}
  public void Observe(long completed){if(completed<0)throw new ArgumentOutOfRangeException(nameof(completed));if(!observed||completed>last){last=completed;observed=true;}}
  public bool Ready(long now)=>!observed||(now>=last&&now-last>=ticks);
  public double RemainingMilliseconds(long now)=>Ready(now)?0:((double)ticks-(now-(double)last))*1000/Frequency;
 }
 public enum Admission{Accepted,Busy,NotReady,CoolingDown}
 public sealed class InferenceScheduler
 {
  private readonly InferenceOwner owner;private readonly CompletionCooldown cooldown;private readonly IMonotonicClock clock;
  public InferenceScheduler(InferenceOwner service,CompletionCooldown sharedCooldown=null,IMonotonicClock monotonicClock=null)
  {owner=service??throw new ArgumentNullException(nameof(service));clock=monotonicClock??owner.Clock;cooldown=sharedCooldown??new CompletionCooldown(clock.Frequency);if(cooldown.Frequency!=clock.Frequency)throw new ArgumentException("Clock frequency differs");}
  public CompletionCooldown Cooldown=>cooldown;
  public void ObserveCompletion(){var tick=owner.LastActualCompletionTick;if(tick>0)cooldown.Observe(tick);}
  public Admission TrySubmit(TokenRequest request)
  {
   ObserveCompletion();var state=owner.State;if(state==OwnerState.Running||owner.HasUnreadCompletion)return Admission.Busy;if(state!=OwnerState.Ready)return Admission.NotReady;
   if(!cooldown.Ready(clock.Now))return Admission.CoolingDown;return owner.TrySubmit(request)?Admission.Accepted:Admission.Busy;
  }
  public bool TryTakeCompletion(out OwnerCompletion result){var found=owner.TryTakeCompletion(out result);ObserveCompletion();return found;}
 }
}
