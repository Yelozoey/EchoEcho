using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using EchoStation.AI.Context;
using EchoStation.AI.Execution;
using EchoStation.AI.GameIntegration;
using EchoStation.AI.Inference;
using EchoStation.Dialogue.Integration;
using EchoStation.Echo;
using EchoStation.Station;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Station;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public sealed class StationLlmSessionTests:StationPlayFixture
 {
  private sealed class Clock:IMonotonicClock{private long tick=1000;public long Now=>Interlocked.Read(ref tick);public long Frequency=>1000;public void Advance()=>Interlocked.Add(ref tick,300);}
  private sealed class Factory:ILocalModelFactory
  {
   public readonly ManualResetEventSlim Work=new ManualResetEventSlim(true);public string Raw="{\"intent\":\"follow\"}";public int Creates,Requests,Finished,Disposed,CreateThread,RunThread,DisposeThread;
   public ILocalModelRuntime Create(int main,CancellationToken cancellation){CreateThread=Thread.CurrentThread.ManagedThreadId;Interlocked.Increment(ref Creates);return new Runtime(this);}
   private sealed class Runtime:ILocalModelRuntime
   {
    private readonly Factory f;public Runtime(Factory factory){f=factory;}
    public GenerationResult Generate(TokenRequest request,CancellationToken cancellation)
    {
     f.RunThread=Thread.CurrentThread.ManagedThreadId;Interlocked.Increment(ref f.Requests);f.Work.Wait();
     var prepared=(PreparedPrompt)Activator.CreateInstance(typeof(PreparedPrompt),BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{request.Snapshot,"controlled session coordination",new[]{1},request.Snapshot.Targets.ToArray(),"",0,0},null);
     var result=(GenerationResult)Activator.CreateInstance(typeof(GenerationResult),BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{f.Raw,new List<int>{151645},GenerationStop.Eos,0d,"controlled-session-fixture",prepared,Thread.CurrentThread.ManagedThreadId,0d},null);Interlocked.Increment(ref f.Finished);return result;
    }
    public void Dispose(){f.DisposeThread=Thread.CurrentThread.ManagedThreadId;Interlocked.Increment(ref f.Disposed);}
   }
  }
  private StationLlmSession session;private Factory factory;private Clock clock;
  private IEnumerator Setup()
  {yield return Boot();factory=new Factory();clock=new Clock();session=new StationLlmSession(station,factory,Thread.CurrentThread.ManagedThreadId,clock);Assert.That(station.Dialogue.SetOpen(true),Is.True);yield return R04ShadowProbe.Frame();}
  private void Send(string text){station.Dialogue.InputField.text=text;Assert.That(station.Dialogue.SubmitInput(),Is.True);}
  private IEnumerator PollUntil(Func<bool> condition,string message)
  {var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){session.Poll();if(timer.Elapsed.TotalSeconds>5)Assert.Fail(message);yield return R04ShadowProbe.Frame();}}
  [UnityTest]public IEnumerator DefaultScriptHasNoModelCreationAndExplicitEnableUsesSingleBackgroundOwner()
  {
   yield return new EnterPlayMode();yield return Setup();Send("跟随我");Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(factory.Creates,Is.Zero);Assert.That(session.Enabled,Is.False);
   session.SetEnabled(true);yield return PollUntil(()=>session.Phase==RequestPhase.Ready,"Owner load");Send("原地待命");yield return PollUntil(()=>session.DeliveredGenerationCount==1,"Controlled semantic result");
   // Deliberately misclassified follow for a wait input must not execute follow again.
   Assert.That(session.LastApplication.DeliveredKind,Is.EqualTo(IntentKind.Answer));Assert.That(factory.Creates,Is.EqualTo(1));Assert.That(factory.CreateThread,Is.Not.EqualTo(Thread.CurrentThread.ManagedThreadId));Assert.That(factory.RunThread,Is.EqualTo(factory.CreateThread));
  }
  [UnityTest]public IEnumerator BusyInputHasNoQueueAndOnlyFirstRequestCanExecute()
  {
   yield return new EnterPlayMode();yield return Setup();factory.Work.Reset();session.SetEnabled(true);yield return PollUntil(()=>session.Phase==RequestPhase.Ready,"Owner load");Send("跟随我");yield return Until(()=>Volatile.Read(ref factory.Requests)==1,5,"Controlled work start");Send("原地待命");Assert.That(factory.Requests,Is.EqualTo(1));Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(session.ConsecutiveFaults,Is.Zero);
   factory.Work.Set();yield return PollUntil(()=>session.DeliveredGenerationCount==1,"First completion");Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(factory.Requests,Is.EqualTo(1));
  }
  [UnityTest]public IEnumerator ThreeMalformedOutputsDisableFutureRoutingWithoutReplayingAnyFailedInput()
  {
   yield return new EnterPlayMode();yield return Setup();factory.Raw="{bad-json";session.SetEnabled(true);yield return PollUntil(()=>session.Phase==RequestPhase.Ready,"Owner load");
   for(var i=1;i<=3;i++){if(i>1)clock.Advance();Send("跟随我");var expected=i;yield return PollUntil(()=>session.DeliveredGenerationCount==expected,"Malformed completion");Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(session.ConsecutiveFaults,Is.EqualTo(i));}
   Assert.That(session.Enabled,Is.False);Assert.That(session.AutomaticallyDisabled,Is.True);Assert.That(factory.Requests,Is.EqualTo(3));Send("跟随我");Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(factory.Requests,Is.EqualTo(3));
  }
  [UnityTest]public IEnumerator FreshInputArrivingWithThirdFaultUsesScriptButOldFaultedInputDoesNot()
  {
   yield return new EnterPlayMode();yield return Setup();factory.Raw="{bad-json";session.SetEnabled(true);yield return PollUntil(()=>session.Phase==RequestPhase.Ready,"Owner load");
   for(var i=1;i<=2;i++){if(i>1)clock.Advance();Send("跟随我");var expected=i;yield return PollUntil(()=>session.DeliveredGenerationCount==expected,"Malformed completion");}
   clock.Advance();Send("跟随我");yield return Until(()=>Volatile.Read(ref factory.Finished)==3,5,"Third raw completion");Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Send("原地待命");Assert.That(session.Enabled,Is.False);Assert.That(session.DeliveredGenerationCount,Is.EqualTo(3));Assert.That(station.GetComponent<ScriptDialogueController>().LastDispatchedKind,Is.EqualTo(IntentKind.Wait));Assert.That(factory.Requests,Is.EqualTo(3));
  }
  [UnityTest]public IEnumerator ManualOverrideDropsPendingResultAndNeverReleasesNativeOnMain()
  {
   yield return new EnterPlayMode();yield return Setup();factory.Work.Reset();session.SetEnabled(true);yield return PollUntil(()=>session.Phase==RequestPhase.Ready,"Owner load");Send("跟随我");yield return Until(()=>Volatile.Read(ref factory.Requests)==1,5,"Controlled work start");
   station.GetComponent<ScriptDialogueController>().SubmitPlayerIntent(ScriptCommands.Wait());factory.Work.Set();yield return PollUntil(()=>session.Phase==RequestPhase.Ready,"Override drains");Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(session.DeliveredGenerationCount,Is.Zero);Assert.That(factory.Disposed,Is.Zero);Assert.That(session.ConsecutiveFaults,Is.Zero);
  }
  [UnityTest]public IEnumerator CancelIsNonblockingAndKeepsOwnerUntilControlledKernelReturns()
  {
   yield return new EnterPlayMode();yield return Setup();factory.Work.Reset();session.SetEnabled(true);yield return PollUntil(()=>session.Phase==RequestPhase.Ready,"Owner load");Send("跟随我");yield return Until(()=>Volatile.Read(ref factory.Requests)==1,5,"Controlled work start");var timer=System.Diagnostics.Stopwatch.StartNew();session.Cancel();Assert.That(timer.Elapsed.TotalMilliseconds,Is.LessThan(100));Assert.That(factory.Disposed,Is.Zero);Assert.That(session.Feedback,Is.EqualTo(FeedbackPhase.Draining));factory.Work.Set();yield return PollUntil(()=>session.Phase==RequestPhase.Ready,"Cancel drains");Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(session.DeliveredGenerationCount,Is.Zero);Assert.That(session.ConsecutiveFaults,Is.Zero);
  }
  [UnityTearDown]public IEnumerator ReleaseSessionBeforeStationFixture()
  {
   if(factory!=null)factory.Work.Set();if(session!=null){session.Dispose();yield return PollUntil(()=>session.CleanupComplete,"Session owner release");Assert.That(factory.Disposed,Is.EqualTo(factory.Creates));if(factory.Creates>0)Assert.That(factory.DisposeThread,Is.EqualTo(factory.CreateThread));session=null;}
  }
 }
}
