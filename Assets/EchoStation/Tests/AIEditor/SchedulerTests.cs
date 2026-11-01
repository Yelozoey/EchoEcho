using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using EchoStation.AI.Inference;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class SchedulerTests
 {
  private sealed class Clock:IMonotonicClock{public long Tick;public long Now=>Tick;public long Frequency=>Stopwatch.Frequency;}
  private sealed class Factory:ILocalModelFactory
  {
   public ManualResetEventSlim Work,DisposeGate;public int Disposing;
   public ILocalModelRuntime Create(int main,CancellationToken cancellation)=>new Runtime(this);
   private sealed class Runtime:ILocalModelRuntime
   {private readonly Factory f;public Runtime(Factory value){f=value;}public GenerationResult Generate(TokenRequest r,CancellationToken c){f.Work?.Wait(c);return new GenerationResult("fixture",new List<int>{151645},GenerationStop.Eos,0,"test");}public void Dispose(){Interlocked.Exchange(ref f.Disposing,1);f.DisposeGate?.Wait();}}
  }
  private static IEnumerator Until(Func<bool> test){var w=Stopwatch.StartNew();while(!test()&&w.Elapsed.TotalSeconds<5)yield return null;Assert.That(test(),Is.True);}
  [Test]public void ExactZero299And300MillisecondBoundaries()
  {var gate=new CompletionCooldown(1000);Assert.That(gate.Ready(0),Is.True);gate.Observe(1000);Assert.That(gate.Ready(1000),Is.False);Assert.That(gate.Ready(1299),Is.False);Assert.That(gate.RemainingMilliseconds(1299),Is.EqualTo(1));Assert.That(gate.Ready(1300),Is.True);}
  [Test]public void RepeatedOrOldObservationsDoNotExtendOrRollBack()
  {var gate=new CompletionCooldown(1000);gate.Observe(1000);gate.Observe(1000);Assert.That(gate.Ready(1300),Is.True);gate.Observe(2000);gate.Observe(1000);Assert.That(gate.Ready(2299),Is.False);Assert.That(gate.Ready(2300),Is.True);Assert.That(gate.Ready(1999),Is.False);}
  [Test]public void TickRoundingNeverAllowsLessThan300Milliseconds()
  {var gate=new CompletionCooldown(7);gate.Observe(100);Assert.That(gate.Ready(102),Is.False);Assert.That(gate.Ready(103),Is.True);Assert.Throws<ArgumentOutOfRangeException>(()=>new CompletionCooldown(0));}
  [UnityTest]public IEnumerator BusyAndUnreadResultAreRejectedThenCompletionStartsCooldown()
  {
   using(var work=new ManualResetEventSlim(false))
   {var owner=new InferenceOwner(new Factory{Work=work},Thread.CurrentThread.ManagedThreadId);var clock=new Clock();var schedule=new InferenceScheduler(owner,monotonicClock:clock);
    try{Assert.That(schedule.TrySubmit(new TokenRequest(1,new[]{1})),Is.EqualTo(Admission.NotReady));owner.Start();yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(schedule.TrySubmit(new TokenRequest(1,new[]{1})),Is.EqualTo(Admission.Accepted));clock.Tick=long.MaxValue/2;Assert.That(schedule.TrySubmit(new TokenRequest(2,new[]{2})),Is.EqualTo(Admission.Busy));work.Set();yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(schedule.TrySubmit(new TokenRequest(3,new[]{3})),Is.EqualTo(Admission.Busy));Assert.That(schedule.TryTakeCompletion(out var result),Is.True);clock.Tick=result.ActualCompletedTick+clock.Frequency*299/1000;Assert.That(schedule.TrySubmit(new TokenRequest(4,new[]{4})),Is.EqualTo(Admission.CoolingDown));clock.Tick=result.ActualCompletedTick+(clock.Frequency*3+9)/10;Assert.That(schedule.TrySubmit(new TokenRequest(5,new[]{5})),Is.EqualTo(Admission.Accepted));yield return Until(()=>owner.State==OwnerState.Ready);schedule.TryTakeCompletion(out _);}
    finally{owner.StopAsync();}yield return Until(()=>owner.CleanupComplete);
   }
  }
  [UnityTest]public IEnumerator StopUsesFinalResourceReleaseAndSharedGateSurvivesReplacement()
  {
   using(var release=new ManualResetEventSlim(false))
   {var factory=new Factory{DisposeGate=release};var owner=new InferenceOwner(factory,Thread.CurrentThread.ManagedThreadId);var clock=new Clock();var shared=new CompletionCooldown(clock.Frequency);var schedule=new InferenceScheduler(owner,shared,clock);
    owner.Start();yield return Until(()=>owner.State==OwnerState.Ready);schedule.TrySubmit(new TokenRequest(1,new[]{1}));yield return Until(()=>owner.State==OwnerState.Ready);schedule.TryTakeCompletion(out var completed);owner.StopAsync();yield return Until(()=>Volatile.Read(ref factory.Disposing)>0);Assert.That(owner.CleanupComplete,Is.False);release.Set();yield return Until(()=>owner.CleanupComplete);schedule.ObserveCompletion();Assert.That(owner.LastActualCompletionTick,Is.GreaterThanOrEqualTo(completed.ActualCompletedTick));Assert.That(shared.LastObserved,Is.EqualTo(owner.LastActualCompletionTick));
    var fresh=new InferenceOwner(new Factory(),Thread.CurrentThread.ManagedThreadId);var next=new InferenceScheduler(fresh,shared,clock);fresh.Start();yield return Until(()=>fresh.State==OwnerState.Ready);clock.Tick=shared.LastObserved+clock.Frequency*299/1000;Assert.That(next.TrySubmit(new TokenRequest(2,new[]{2})),Is.EqualTo(Admission.CoolingDown));fresh.StopAsync();yield return Until(()=>fresh.CleanupComplete);
   }
  }
 }
}
