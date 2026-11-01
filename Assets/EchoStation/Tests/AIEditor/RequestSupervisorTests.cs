using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using EchoStation.AI.Inference;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class RequestSupervisorTests
 {
  private sealed class Clock:IMonotonicClock
  {private long tick=1000;public long Now=>Interlocked.Read(ref tick);public long Frequency=>1000;public void Set(long value)=>Interlocked.Exchange(ref tick,value);}
  private sealed class Factory:ILocalModelFactory
  {
   public readonly ManualResetEventSlim Load=new ManualResetEventSlim(true),Work=new ManualResetEventSlim(false);public int Created,Started,Disposed;public Exception LoadFault,WorkFault;
   public ILocalModelRuntime Create(int main,CancellationToken c){Interlocked.Increment(ref Created);Load.Wait();if(LoadFault!=null)throw LoadFault;return new Runtime(this);}
   private sealed class Runtime:ILocalModelRuntime
   {private readonly Factory f;public Runtime(Factory value){f=value;}public GenerationResult Generate(TokenRequest r,CancellationToken c){Interlocked.Increment(ref f.Started);f.Work.Wait();if(f.WorkFault!=null)throw f.WorkFault;return new GenerationResult("fixture",new List<int>{151645},GenerationStop.Eos,0,"test-fixture");}public void Dispose(){Interlocked.Increment(ref f.Disposed);}}
   public void Release(){Load.Set();Work.Set();}
  }
  private static IEnumerator Until(Func<bool> p){var w=Stopwatch.StartNew();while(!p()&&w.Elapsed.TotalSeconds<5)yield return null;Assert.That(p(),Is.True,"supervisor fixture transition timeout");}
  [UnityTest]public IEnumerator Exact20SecondDeadlineNotifiesOnceThenDrainsAndCools()
  {
   var f=new Factory();var clock=new Clock();var owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId,clock);var s=new RequestSupervisor(owner);
   try
   {
    s.Start();yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(s.TrySubmit(new TokenRequest(1,new[]{1})),Is.EqualTo(Admission.Accepted));yield return Until(()=>Volatile.Read(ref f.Started)>0);
    clock.Set(20999);s.Poll();Assert.That(s.HasNotification,Is.False);clock.Set(21000);var watch=Stopwatch.StartNew();s.Poll();Assert.That(watch.Elapsed.TotalMilliseconds,Is.LessThan(100));Assert.That(s.Phase,Is.EqualTo(RequestPhase.Draining));
    Assert.That(s.TryTakeCompletion(out var timeout),Is.True);Assert.That(timeout.Outcome,Is.EqualTo(RequestOutcome.RequestTimeout));Assert.That(timeout.ActualCompletedTick,Is.Zero);Assert.That(timeout.Generation,Is.Null);Assert.That(s.TryTakeCompletion(out _),Is.False);Assert.That(s.TrySubmit(new TokenRequest(2,new[]{1})),Is.EqualTo(Admission.Busy));Assert.That(f.Disposed,Is.Zero);
    f.Work.Set();yield return Until(()=>owner.State==OwnerState.Ready);s.Poll();Assert.That(s.TryTakeCompletion(out _),Is.False);clock.Set(21299);Assert.That(s.TrySubmit(new TokenRequest(3,new[]{1})),Is.EqualTo(Admission.CoolingDown));clock.Set(21300);Assert.That(s.TrySubmit(new TokenRequest(4,new[]{1})),Is.EqualTo(Admission.Accepted));yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(s.TryTakeCompletion(out var next),Is.True);Assert.That(next.RequestId,Is.EqualTo(4));Assert.That(next.Outcome,Is.EqualTo(RequestOutcome.Succeeded));
   }
   finally{f.Release();s.StopAsync();}yield return Until(()=>owner.CleanupComplete);
  }
  [UnityTest]public IEnumerator CompletionBeforeDeadlineIsAcceptedEvenWhenControlPollsLate()
  {
   var f=new Factory();var clock=new Clock();var owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId,clock);var s=new RequestSupervisor(owner);
   try{s.Start();yield return Until(()=>owner.State==OwnerState.Ready);s.TrySubmit(new TokenRequest(1,new[]{1}));clock.Set(20999);f.Work.Set();yield return Until(()=>owner.State==OwnerState.Ready);clock.Set(50000);Assert.That(s.TryTakeCompletion(out var result),Is.True);Assert.That(result.Outcome,Is.EqualTo(RequestOutcome.Succeeded));Assert.That(result.ActualCompletedTick,Is.EqualTo(20999));}
   finally{f.Release();s.StopAsync();}yield return Until(()=>owner.CleanupComplete);
  }
  [UnityTest]public IEnumerator CompletionAtDeadlineIsRejectedWithoutPriorPoll()
  {
   var f=new Factory();var clock=new Clock();var owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId,clock);var s=new RequestSupervisor(owner);
   try{s.Start();yield return Until(()=>owner.State==OwnerState.Ready);s.TrySubmit(new TokenRequest(1,new[]{1}));clock.Set(21000);f.Work.Set();yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(s.TryTakeCompletion(out var result),Is.True);Assert.That(result.Outcome,Is.EqualTo(RequestOutcome.RequestTimeout));Assert.That(result.Generation,Is.Null);}
   finally{f.Release();s.StopAsync();}yield return Until(()=>owner.CleanupComplete);
  }
  [UnityTest]public IEnumerator CancelAfterWorkerPublicationStillDiscardsUnreadSuccess()
  {
   var f=new Factory();var clock=new Clock();var owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId,clock);var s=new RequestSupervisor(owner);
   try{s.Start();yield return Until(()=>owner.State==OwnerState.Ready);s.TrySubmit(new TokenRequest(1,new[]{1}));f.Work.Set();yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(s.CancelCurrent(),Is.True);Assert.That(s.TryTakeCompletion(out var result),Is.True);Assert.That(result.Outcome,Is.EqualTo(RequestOutcome.Cancelled));Assert.That(result.Generation,Is.Null);Assert.That(s.TryTakeCompletion(out _),Is.False);}
   finally{f.Release();s.StopAsync();}yield return Until(()=>owner.CleanupComplete);
  }
  [UnityTest]public IEnumerator Cold30SecondTimeoutNeverDisposesStillLoadingRuntimeOnControlThread()
  {
   var f=new Factory();f.Load.Reset();var clock=new Clock();var owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId,clock);var s=new RequestSupervisor(owner);
   try
   {
    s.Start();yield return Until(()=>Volatile.Read(ref f.Created)>0);clock.Set(30999);s.Poll();Assert.That(s.HasNotification,Is.False);clock.Set(31000);s.Poll();Assert.That(s.TryTakeCompletion(out var result),Is.True);Assert.That(result.Outcome,Is.EqualTo(RequestOutcome.ColdTimeout));Assert.That(result.RequestId,Is.Zero);Assert.That(owner.CleanupComplete,Is.False);Assert.That(f.Disposed,Is.Zero);Assert.That(s.TrySubmit(new TokenRequest(1,new[]{1})),Is.EqualTo(Admission.NotReady));f.Load.Set();yield return Until(()=>owner.CleanupComplete);s.Poll();Assert.That(s.TryTakeCompletion(out _),Is.False);Assert.That(f.Disposed,Is.EqualTo(1));Assert.That(f.Started,Is.Zero);
   }
   finally{f.Release();s.StopAsync();}
  }
  [UnityTest]public IEnumerator TimelyColdLoadDoesNotTimeoutBecauseOfLateControlPoll()
  {
   var f=new Factory();var clock=new Clock();var owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId,clock);var s=new RequestSupervisor(owner);
   try{s.Start();yield return Until(()=>owner.State==OwnerState.Ready);clock.Set(50000);s.Poll();Assert.That(s.Phase,Is.EqualTo(RequestPhase.Ready));Assert.That(s.HasNotification,Is.False);}
   finally{f.Release();s.StopAsync();}yield return Until(()=>owner.CleanupComplete);
  }
  [UnityTest]public IEnumerator LateColdLoadIsRejectedEvenWithoutEarlierControlPoll()
  {
   var f=new Factory();f.Load.Reset();var clock=new Clock();var owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId,clock);var s=new RequestSupervisor(owner);
   try{s.Start();yield return Until(()=>Volatile.Read(ref f.Created)>0);clock.Set(31000);f.Load.Set();yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(s.TryTakeCompletion(out var result),Is.True);Assert.That(result.Outcome,Is.EqualTo(RequestOutcome.ColdTimeout));yield return Until(()=>owner.CleanupComplete);Assert.That(f.Disposed,Is.EqualTo(1));}
   finally{f.Release();s.StopAsync();}
  }
  [UnityTest]public IEnumerator StartupAndBackendFailuresAreTypedAndNeverReturnGeneration()
  {
   var f=new Factory{LoadFault=new IOException("fixture-load")};var owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId);var s=new RequestSupervisor(owner);
   try{s.Start();yield return Until(()=>owner.CleanupComplete);Assert.That(s.TryTakeCompletion(out var result),Is.True);Assert.That(result.Outcome,Is.EqualTo(RequestOutcome.StartupFailed));Assert.That(result.Generation,Is.Null);Assert.That(s.TryTakeCompletion(out _),Is.False);}
   finally{f.Release();s.StopAsync();}
   f=new Factory{WorkFault=new IOException("fixture-backend")};f.Work.Set();owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId);s=new RequestSupervisor(owner);
   try{s.Start();yield return Until(()=>owner.State==OwnerState.Ready);s.TrySubmit(new TokenRequest(1,new[]{1}));yield return Until(()=>owner.CleanupComplete);Assert.That(s.TryTakeCompletion(out var result),Is.True);Assert.That(result.Outcome,Is.EqualTo(RequestOutcome.BackendFailed));Assert.That(result.Generation,Is.Null);Assert.That(f.Disposed,Is.EqualTo(1));}
   finally{f.Release();s.StopAsync();}
  }
 }
}
