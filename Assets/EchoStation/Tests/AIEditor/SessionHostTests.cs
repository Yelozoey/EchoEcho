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
 public sealed class SessionHostTests
 {
  private sealed class Clock:IMonotonicClock{private long now=1000;public long Now=>Interlocked.Read(ref now);public long Frequency=>1000;public void Set(long value)=>Interlocked.Exchange(ref now,value);}
  private sealed class Factory:ILocalModelFactory
  {
   public readonly ManualResetEventSlim Load=new ManualResetEventSlim(true),Work=new ManualResetEventSlim(false),Release=new ManualResetEventSlim(true);public int Creates,Started,Active,Peak,Disposing,Disposed;public bool Fail;
   public ILocalModelRuntime Create(int main,CancellationToken c){Interlocked.Increment(ref Creates);Load.Wait();var active=Interlocked.Increment(ref Active);Peak=Math.Max(Peak,active);return new Runtime(this);}
   private sealed class Runtime:ILocalModelRuntime
   {
    private readonly Factory f;public Runtime(Factory value){f=value;}
    public GenerationResult Generate(TokenRequest r,CancellationToken c){Interlocked.Increment(ref f.Started);f.Work.Wait();if(f.Fail)throw new IOException("fixture-fatal");return new GenerationResult("fixture",new List<int>{151645},GenerationStop.Eos,0,"test-fixture");}
    public void Dispose(){Interlocked.Increment(ref f.Disposing);f.Release.Wait();Interlocked.Decrement(ref f.Active);Interlocked.Increment(ref f.Disposed);}
   }
   public void Unblock(){Load.Set();Work.Set();Release.Set();}
  }
  private static IEnumerator Until(Func<bool> p,InferenceSessionHost host=null){var w=Stopwatch.StartNew();while(!p()&&w.Elapsed.TotalSeconds<5){host?.Poll();yield return null;}Assert.That(p(),Is.True,"session-host fixture transition timeout");}
  [UnityTest]public IEnumerator EveryWorldChangeDropsRunningResultWithoutReplacingLoadedWeights()
  {
   foreach(var reason in new[]{LifecycleChange.NewGame,LifecycleChange.LoadGame,LifecycleChange.ManualOverride,LifecycleChange.WorldChanged,LifecycleChange.Ending})
   {
    var f=new Factory();var host=new InferenceSessionHost(f,Thread.CurrentThread.ManagedThreadId);
    try
    {host.SetEnabled(true);yield return Until(()=>host.Phase==RequestPhase.Ready,host);Assert.That(host.TrySubmit(new[]{1},out var old),Is.EqualTo(Admission.Accepted));yield return Until(()=>Volatile.Read(ref f.Started)==1);host.Invalidate(reason);Assert.That(host.IsCurrent(old),Is.False);Assert.That(host.TryTakeCompletion(out _),Is.False);f.Work.Set();yield return Until(()=>host.Phase==RequestPhase.Ready,host);Assert.That(host.TryTakeCompletion(out _),Is.False);Assert.That(f.Creates,Is.EqualTo(1));Assert.That(f.Disposed,Is.Zero);}
    finally{f.Unblock();host.Dispose();}yield return Until(()=>host.CleanupComplete,host);Assert.That(f.Active,Is.Zero);
   }
  }
  [UnityTest]public IEnumerator TakenResultRequiresFreshStampAndOnlyOneClaim()
  {
   var f=new Factory();f.Work.Set();var host=new InferenceSessionHost(f,Thread.CurrentThread.ManagedThreadId);
   try
   {
    host.SetEnabled(true);yield return Until(()=>host.Phase==RequestPhase.Ready,host);host.TrySubmit(new[]{1},out var first);Assert.That(host.TryClaim(first),Is.False,"not delivered yet");yield return Until(()=>host.Phase==RequestPhase.Ready,host);Assert.That(host.TryTakeCompletion(out var result),Is.True);Assert.That(result.Stamp.Request,Is.EqualTo(first.Request));Assert.That(host.TryClaim(first),Is.True);Assert.That(host.TryClaim(first),Is.False);Assert.That(host.IsCurrent(first),Is.False);
    host.Invalidate(LifecycleChange.NewGame);Assert.That(host.TryClaim(first),Is.False);Assert.That(host.TryTakeCompletion(out _),Is.False);
   }
   finally{f.Unblock();host.Dispose();}yield return Until(()=>host.CleanupComplete,host);
  }
  [UnityTest]public IEnumerator TakenButUnclaimedReplyBecomesStaleAfterLoad()
  {
   var f=new Factory();f.Work.Set();var host=new InferenceSessionHost(f,Thread.CurrentThread.ManagedThreadId);
   try{host.SetEnabled(true);yield return Until(()=>host.Phase==RequestPhase.Ready,host);host.TrySubmit(new[]{1},out var stamp);yield return Until(()=>host.Phase==RequestPhase.Ready,host);Assert.That(host.TryTakeCompletion(out _),Is.True);Assert.That(host.IsCurrent(stamp),Is.True);host.Invalidate(LifecycleChange.LoadGame);Assert.That(host.TryClaim(stamp),Is.False);}
   finally{f.Unblock();host.Dispose();}yield return Until(()=>host.CleanupComplete,host);
  }
  [UnityTest]public IEnumerator ToggleWaitsForFullDisposeAndPreservesCooldownAcrossOwners()
  {
   var f=new Factory();f.Work.Set();f.Release.Reset();var clock=new Clock();var host=new InferenceSessionHost(f,Thread.CurrentThread.ManagedThreadId,clock);
   try
   {
    host.SetEnabled(true);yield return Until(()=>host.Phase==RequestPhase.Ready,host);host.TrySubmit(new[]{1},out var old);yield return Until(()=>host.Phase==RequestPhase.Ready,host);Assert.That(host.TryTakeCompletion(out _),Is.True);host.SetEnabled(false);yield return Until(()=>Volatile.Read(ref f.Disposing)==1);clock.Set(5000);host.SetEnabled(true);host.Poll();Assert.That(f.Creates,Is.EqualTo(1));Assert.That(host.IsCurrent(old),Is.False);Assert.That(host.TrySubmit(new[]{1},out _),Is.Not.EqualTo(Admission.Accepted));f.Release.Set();yield return Until(()=>host.Phase==RequestPhase.Ready,host);
    Assert.That(f.Creates,Is.EqualTo(2));Assert.That(f.Peak,Is.EqualTo(1));Assert.That(host.Cooldown.LastObserved,Is.EqualTo(5000));clock.Set(5299);Assert.That(host.TrySubmit(new[]{1},out _),Is.EqualTo(Admission.CoolingDown));clock.Set(5300);Assert.That(host.TrySubmit(new[]{1},out _),Is.EqualTo(Admission.Accepted));
   }
   finally{f.Unblock();host.Dispose();}yield return Until(()=>host.CleanupComplete,host);Assert.That(f.Active,Is.Zero);
  }
  [UnityTest]public IEnumerator ToggleDuringUninterruptibleLoadingNeverCreatesOverlappingRuntime()
  {
   var f=new Factory();f.Load.Reset();var host=new InferenceSessionHost(f,Thread.CurrentThread.ManagedThreadId);
   try
   {host.SetEnabled(true);yield return Until(()=>Volatile.Read(ref f.Creates)==1);host.SetEnabled(false);host.SetEnabled(true);Assert.That(f.Creates,Is.EqualTo(1));f.Load.Set();yield return Until(()=>host.Phase==RequestPhase.Ready,host);Assert.That(f.Creates,Is.EqualTo(2));Assert.That(f.Disposed,Is.EqualTo(1));Assert.That(f.Peak,Is.EqualTo(1));}
   finally{f.Unblock();host.Dispose();}yield return Until(()=>host.CleanupComplete,host);
  }
  [UnityTest]public IEnumerator FatalSessionStaysFaultedUntilExplicitRestart()
  {
   var f=new Factory{Fail=true};f.Work.Set();var host=new InferenceSessionHost(f,Thread.CurrentThread.ManagedThreadId);
   try
   {host.SetEnabled(true);yield return Until(()=>host.Phase==RequestPhase.Ready,host);host.TrySubmit(new[]{1},out _);yield return Until(()=>host.Phase==RequestPhase.Faulted,host);Assert.That(host.TryTakeCompletion(out var failed),Is.True);Assert.That(failed.Completion.Outcome,Is.EqualTo(RequestOutcome.BackendFailed));host.Poll();host.Poll();Assert.That(f.Creates,Is.EqualTo(1));f.Fail=false;Assert.That(host.Restart(),Is.True);yield return Until(()=>host.Phase==RequestPhase.Ready,host);Assert.That(f.Creates,Is.EqualTo(2));Assert.That(f.Peak,Is.EqualTo(1));}
   finally{f.Unblock();host.Dispose();}yield return Until(()=>host.CleanupComplete,host);
  }
  [UnityTest]public IEnumerator SceneUnloadAndDisposeDisableHostAndCannotRestartDisposedInstance()
  {
   var f=new Factory();var host=new InferenceSessionHost(f,Thread.CurrentThread.ManagedThreadId);
   try{host.SetEnabled(true);yield return Until(()=>host.Phase==RequestPhase.Ready,host);host.TrySubmit(new[]{1},out var stamp);host.Invalidate(LifecycleChange.SceneUnloaded);Assert.That(host.Enabled,Is.False);Assert.That(host.IsCurrent(stamp),Is.False);f.Unblock();yield return Until(()=>host.CleanupComplete,host);host.Dispose();Assert.Throws<ObjectDisposedException>(()=>host.SetEnabled(true));Assert.Throws<ObjectDisposedException>(()=>host.TrySubmit(new[]{1},out _));host.Poll();Assert.That(f.Creates,Is.EqualTo(1));}
   finally{f.Unblock();host.Dispose();}yield return Until(()=>host.CleanupComplete,host);
  }
 }
}
