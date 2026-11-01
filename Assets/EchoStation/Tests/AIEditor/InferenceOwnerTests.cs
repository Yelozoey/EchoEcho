using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using EchoStation.AI.Inference;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class InferenceOwnerTests
 {
  // These fixtures validate communication/lifetime only; never counted as true model inference.
  private sealed class Factory:ILocalModelFactory
  {
   public int CreatedThread,DisposedThread,Disposals,WorkStarted;public bool FailLoad,FailWork;public ManualResetEventSlim LoadGate,WorkGate;
   public ILocalModelRuntime Create(int main,CancellationToken cancellation){CreatedThread=Thread.CurrentThread.ManagedThreadId;if(FailLoad)throw new IOException("fixture-load-failure");LoadGate?.Wait(cancellation);return new Runtime(this);}
   private sealed class Runtime:ILocalModelRuntime
   {
    private readonly Factory owner;public Runtime(Factory value){owner=value;}
    public GenerationResult Generate(TokenRequest request,CancellationToken cancellation){Interlocked.Increment(ref owner.WorkStarted);owner.WorkGate?.Wait(cancellation);cancellation.ThrowIfCancellationRequested();if(owner.FailWork)throw new IOException("fixture-work-failure");return new GenerationResult("fixture",new List<int>{151645},GenerationStop.Eos,0,"test-fixture");}
    public void Dispose(){owner.DisposedThread=Thread.CurrentThread.ManagedThreadId;Interlocked.Increment(ref owner.Disposals);}
   }
  }
  private static IEnumerator Until(Func<bool> predicate)
  {var time=System.Diagnostics.Stopwatch.StartNew();while(!predicate()&&time.Elapsed.TotalSeconds<5)yield return null;Assert.That(predicate(),Is.True,"Owner state transition timed out");}
  [UnityTest]public IEnumerator ExplicitStartIsLazySingleAndOwnerDisposesResources()
  {
   var main=Thread.CurrentThread.ManagedThreadId;var factory=new Factory();using(var owner=new InferenceOwner(factory,main))
   {Assert.That(factory.CreatedThread,Is.Zero);Assert.That(owner.State,Is.EqualTo(OwnerState.New));owner.Start();yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(owner.OwnerThreadId,Is.Not.EqualTo(main));Assert.That(factory.CreatedThread,Is.EqualTo(owner.OwnerThreadId));Assert.Throws<InvalidOperationException>(()=>owner.Start());owner.StopAsync();yield return Until(()=>owner.CleanupComplete);Assert.That(factory.Disposals,Is.EqualTo(1));Assert.That(factory.DisposedThread,Is.EqualTo(factory.CreatedThread));}
  }
  [Test]public void TokenRequestsAreCopiedAndBounded()
  {var ids=new[]{1,2};var request=new TokenRequest(1,ids);ids[0]=9;Assert.That(request.Input[0],Is.EqualTo(1));Assert.Throws<ArgumentOutOfRangeException>(()=>new TokenRequest(0,new[]{1}));Assert.Throws<ArgumentException>(()=>new TokenRequest(1,new int[1537]));}
  [UnityTest]public IEnumerator RunningAndUnreadResultNeverBuildQueue()
  {
   using(var gate=new ManualResetEventSlim(false))
   {var factory=new Factory{WorkGate=gate};using(var owner=new InferenceOwner(factory,Thread.CurrentThread.ManagedThreadId))
    {owner.Start();yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(owner.TrySubmit(new TokenRequest(1,new[]{1})),Is.True);Assert.That(owner.TrySubmit(new TokenRequest(2,new[]{2})),Is.False);gate.Set();yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(owner.TrySubmit(new TokenRequest(3,new[]{3})),Is.False);Assert.That(owner.TryTakeCompletion(out var result),Is.True);Assert.That(result.RequestId,Is.EqualTo(1));Assert.That(result.Outcome,Is.EqualTo(OwnerOutcome.Succeeded));Assert.That(owner.TryTakeCompletion(out _),Is.False);Assert.That(owner.TrySubmit(new TokenRequest(4,new[]{4})),Is.True);yield return Until(()=>owner.State==OwnerState.Ready);Assert.That(owner.TryTakeCompletion(out result),Is.True);Assert.That(result.RequestId,Is.EqualTo(4));owner.StopAsync();yield return Until(()=>owner.CleanupComplete);}}
  }
  [UnityTest]public IEnumerator StopDuringWorkIsNonblockingAndReleasesOnOwner()
  {
   using(var gate=new ManualResetEventSlim(false))
   {var factory=new Factory{WorkGate=gate};using(var owner=new InferenceOwner(factory,Thread.CurrentThread.ManagedThreadId))
    {owner.Start();yield return Until(()=>owner.State==OwnerState.Ready);owner.TrySubmit(new TokenRequest(1,new[]{1}));yield return Until(()=>Volatile.Read(ref factory.WorkStarted)>0);var watch=System.Diagnostics.Stopwatch.StartNew();owner.StopAsync();Assert.That(watch.Elapsed.TotalMilliseconds,Is.LessThan(100));yield return Until(()=>owner.CleanupComplete);Assert.That(factory.Disposals,Is.EqualTo(1));Assert.That(factory.DisposedThread,Is.EqualTo(owner.OwnerThreadId));}}
  }
  [UnityTest]public IEnumerator LoadFailureIsDataAndCleansThread()
  {var factory=new Factory{FailLoad=true};using(var owner=new InferenceOwner(factory,Thread.CurrentThread.ManagedThreadId)){owner.Start();yield return Until(()=>owner.CleanupComplete);Assert.That(owner.State,Is.EqualTo(OwnerState.Faulted));Assert.That(owner.StartupError,Does.Contain("fixture-load-failure"));Assert.That(owner.TrySubmit(new TokenRequest(1,new[]{1})),Is.False);}}
  [UnityTest]public IEnumerator RuntimeFailurePublishesFailureAndDisposes()
  {var factory=new Factory{FailWork=true};using(var owner=new InferenceOwner(factory,Thread.CurrentThread.ManagedThreadId)){owner.Start();yield return Until(()=>owner.State==OwnerState.Ready);owner.TrySubmit(new TokenRequest(1,new[]{1}));yield return Until(()=>owner.CleanupComplete);Assert.That(owner.TryTakeCompletion(out var result),Is.True);Assert.That(result.Outcome,Is.EqualTo(OwnerOutcome.Failed));Assert.That(result.Generation,Is.Null);Assert.That(result.ActualCompletedTick,Is.GreaterThan(0));Assert.That(factory.Disposals,Is.EqualTo(1));}}
  [UnityTest]public IEnumerator StopDuringLoadingCancelsFactoryWithoutMainWait()
  {
   using(var gate=new ManualResetEventSlim(false))
   {var factory=new Factory{LoadGate=gate};using(var owner=new InferenceOwner(factory,Thread.CurrentThread.ManagedThreadId)){owner.Start();yield return Until(()=>factory.CreatedThread!=0);owner.StopAsync();yield return Until(()=>owner.CleanupComplete);Assert.That(owner.State,Is.EqualTo(OwnerState.Stopped));Assert.That(factory.Disposals,Is.Zero);}}
  }
  [Test]public void StoppingUnusedOwnerNeverCreatesRuntime()
  {var factory=new Factory();var owner=new InferenceOwner(factory,Thread.CurrentThread.ManagedThreadId);owner.StopAsync();owner.StopAsync();Assert.That(owner.CleanupComplete,Is.True);Assert.That(factory.CreatedThread,Is.Zero);}
 }
}
