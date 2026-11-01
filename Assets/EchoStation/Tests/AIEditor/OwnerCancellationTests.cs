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
 public sealed class OwnerCancellationTests
 {
  // A deliberately non-interruptible fixture, not counted as actual LLM generation.
  private sealed class Factory:ILocalModelFactory
  {
   public readonly ManualResetEventSlim Work=new ManualResetEventSlim(false);public int Started,Disposed,DisposedThread;public Exception FirstFault;
   public ILocalModelRuntime Create(int main,CancellationToken cancellation)=>new Runtime(this);
   private sealed class Runtime:ILocalModelRuntime
   {
    private readonly Factory f;public Runtime(Factory value){f=value;}
    public GenerationResult Generate(TokenRequest r,CancellationToken c)
    {var call=Interlocked.Increment(ref f.Started);f.Work.Wait();if(call==1&&f.FirstFault!=null)throw f.FirstFault;return new GenerationResult("fixture",new List<int>{151645},GenerationStop.Eos,0,"test-fixture");}
    public void Dispose(){f.DisposedThread=Thread.CurrentThread.ManagedThreadId;Interlocked.Increment(ref f.Disposed);}
   }
  }
  private static IEnumerator Until(Func<bool> p)
  {var w=Stopwatch.StartNew();while(!p()&&w.Elapsed.TotalSeconds<5)yield return null;Assert.That(p(),Is.True,"fixture transition timeout");}
  [UnityTest]public IEnumerator NoninterruptibleCancellationIsImmediateButLateSuccessIsDiscarded()
  {
   var f=new Factory();var owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId);
   try
   {
    owner.Start();yield return Until(()=>owner.State==OwnerState.Ready);owner.TrySubmit(new TokenRequest(1,new[]{1}));yield return Until(()=>Volatile.Read(ref f.Started)==1);
    var watch=Stopwatch.StartNew();Assert.That(owner.CancelCurrent(),Is.True);Assert.That(watch.Elapsed.TotalMilliseconds,Is.LessThan(100));
    Assert.That(owner.State,Is.EqualTo(OwnerState.Running));Assert.That(owner.TryTakeCompletion(out _),Is.False);Assert.That(owner.TrySubmit(new TokenRequest(2,new[]{1})),Is.False);Assert.That(f.Disposed,Is.Zero);
    f.Work.Set();yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(owner.TryTakeCompletion(out var cancelled),Is.True);Assert.That(cancelled.Outcome,Is.EqualTo(OwnerOutcome.Cancelled));Assert.That(cancelled.Failure,Is.EqualTo(OwnerFailure.Cancelled));Assert.That(cancelled.Generation,Is.Null);
    Assert.That(owner.TrySubmit(new TokenRequest(3,new[]{1})),Is.True);yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(owner.TryTakeCompletion(out var next),Is.True);Assert.That(next.Outcome,Is.EqualTo(OwnerOutcome.Succeeded));Assert.That(next.RequestId,Is.EqualTo(3));Assert.That(f.Disposed,Is.Zero);
   }
   finally{f.Work.Set();owner.StopAsync();}
   yield return Until(()=>owner.CleanupComplete);Assert.That(f.Disposed,Is.EqualTo(1));Assert.That(f.DisposedThread,Is.EqualTo(owner.OwnerThreadId));f.Work.Dispose();
  }
  [UnityTest]public IEnumerator GenerationFailuresAreTypedRecoverableAndNextRequestCanRun()
  {
   foreach(GenerationFault fault in Enum.GetValues(typeof(GenerationFault)))
   {
    var f=new Factory{FirstFault=new GenerationException(fault,"fixture-generation")};f.Work.Set();var owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId);
    try
    {
     owner.Start();yield return Until(()=>owner.State==OwnerState.Ready);owner.TrySubmit(new TokenRequest(1,new[]{1}));yield return Until(()=>owner.State==OwnerState.Ready);
     Assert.That(owner.TryTakeCompletion(out var failed),Is.True);Assert.That(failed.Outcome,Is.EqualTo(OwnerOutcome.Failed));Assert.That(failed.Failure,Is.EqualTo(fault==GenerationFault.Timeout?OwnerFailure.Timeout:OwnerFailure.Generation));Assert.That(failed.Generation,Is.Null);Assert.That(f.Disposed,Is.Zero);
     owner.TrySubmit(new TokenRequest(2,new[]{1}));yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(owner.TryTakeCompletion(out var success),Is.True);Assert.That(success.Outcome,Is.EqualTo(OwnerOutcome.Succeeded));
    }
    finally{f.Work.Set();owner.StopAsync();}
    yield return Until(()=>owner.CleanupComplete);Assert.That(f.Disposed,Is.EqualTo(1));f.Work.Dispose();
   }
  }
  [UnityTest]public IEnumerator BackendFailureIsFatalEvenIfPlayerCancelled()
  {
   var f=new Factory{FirstFault=new IOException("fixture-native-fault")};var owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId);
   try
   {
    owner.Start();yield return Until(()=>owner.State==OwnerState.Ready);owner.TrySubmit(new TokenRequest(1,new[]{1}));yield return Until(()=>Volatile.Read(ref f.Started)==1);owner.CancelCurrent();f.Work.Set();yield return Until(()=>owner.CleanupComplete);
    Assert.That(owner.State,Is.EqualTo(OwnerState.Faulted));Assert.That(owner.StartupError,Does.Contain("fixture-native-fault"));Assert.That(owner.TryTakeCompletion(out var result),Is.True);Assert.That(result.Outcome,Is.EqualTo(OwnerOutcome.Cancelled));Assert.That(result.Generation,Is.Null);Assert.That(f.Disposed,Is.EqualTo(1));Assert.That(owner.TrySubmit(new TokenRequest(2,new[]{1})),Is.False);
   }
   finally{f.Work.Set();owner.StopAsync();}f.Work.Dispose();
  }
  [UnityTest]public IEnumerator CancelledCompletionStillStarts300MillisecondCooldown()
  {
   var f=new Factory();var owner=new InferenceOwner(f,Thread.CurrentThread.ManagedThreadId);var schedule=new InferenceScheduler(owner);
   try
   {
    Assert.That(owner.CancelCurrent(),Is.False);owner.Start();yield return Until(()=>owner.State==OwnerState.Ready);schedule.TrySubmit(new TokenRequest(1,new[]{1}));Assert.That(owner.CancelCurrent(),Is.True);f.Work.Set();yield return Until(()=>owner.State==OwnerState.Ready);
    Assert.That(schedule.TryTakeCompletion(out var result),Is.True);Assert.That(result.Outcome,Is.EqualTo(OwnerOutcome.Cancelled));Assert.That(schedule.Cooldown.Ready(result.ActualCompletedTick),Is.False);Assert.That(schedule.Cooldown.Ready(result.ActualCompletedTick+Stopwatch.Frequency*299/1000),Is.False);Assert.That(schedule.Cooldown.Ready(result.ActualCompletedTick+(Stopwatch.Frequency*3+9)/10),Is.True);
   }
   finally{f.Work.Set();owner.StopAsync();}yield return Until(()=>owner.CleanupComplete);f.Work.Dispose();
  }
 }
}
