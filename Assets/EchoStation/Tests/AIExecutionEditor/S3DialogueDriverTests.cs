using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using EchoStation.AI.Context;
using EchoStation.AI.GameIntegration;
using EchoStation.AI.Inference;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Station;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Station;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public sealed class S3DialogueDriverTests:StationPlayFixture
 {
  internal sealed class Clock:IMonotonicClock{private long tick=1000;public long Now=>Interlocked.Read(ref tick);public long Frequency=>1000;public void Advance()=>Interlocked.Add(ref tick,300);}
  // Explicit coordination dependency, never selected by the Player component build.
  internal sealed class Factory:ILocalModelFactory
  {
   public readonly ManualResetEventSlim Load=new ManualResetEventSlim(true),Work=new ManualResetEventSlim(true);public string Raw="{\"intent\":\"follow\"}";public int Creates,Requests,Disposed,Active,Peak;
   public ILocalModelRuntime Create(int main,CancellationToken cancellation)
   {Interlocked.Increment(ref Creates);Load.Wait();var active=Interlocked.Increment(ref Active);for(;;){var old=Volatile.Read(ref Peak);if(old>=active||Interlocked.CompareExchange(ref Peak,active,old)==old)break;}return new Runtime(this);}
   private sealed class Runtime:ILocalModelRuntime
   {
    private readonly Factory f;public Runtime(Factory factory){f=factory;}
    public GenerationResult Generate(TokenRequest request,CancellationToken cancellation)
    {
     Interlocked.Increment(ref f.Requests);f.Work.Wait();var prepared=(PreparedPrompt)Activator.CreateInstance(typeof(PreparedPrompt),BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{request.Snapshot,"driver controlled coordination",new[]{1},request.Snapshot.Targets.ToArray(),"",0,0},null);
     return (GenerationResult)Activator.CreateInstance(typeof(GenerationResult),BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{f.Raw,new List<int>{151645},GenerationStop.Eos,0d,"driver-controlled-fixture",prepared,Thread.CurrentThread.ManagedThreadId,0d},null);
    }
    public void Dispose(){Interlocked.Decrement(ref f.Active);Interlocked.Increment(ref f.Disposed);}
   }
  }
  private S3DialogueDriver driver;private Factory factory;private Clock clock;
  private IEnumerator Setup()
  {yield return Boot();factory=new Factory();clock=new Clock();driver=station.gameObject.AddComponent<S3DialogueDriver>();driver.ConfigureForTests(factory,clock);yield return Until(()=>driver.Ready,5,"Driver initialization");Assert.That(station.Dialogue.SetOpen(true),Is.True);yield return R04ShadowProbe.Frame();}
  private void Send(string text){station.Dialogue.InputField.text=text;Assert.That(station.Dialogue.SubmitInput(),Is.True);}
  private IEnumerator ReadyOwner(){Assert.That(driver.SetEnabled(true),Is.True);yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,5,"Controlled Owner ready");}
  [UnityTest]public IEnumerator GlobalOptionAndOwnAcceptedMemoryKeepOneConsumerAndOneOwner()
  {
   yield return new EnterPlayMode();yield return Setup();Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(factory.Creates,Is.Zero);GameRuntimeOptions.UseLLM=true;yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,5,"Global option starts Owner");Send("跟随我");yield return Until(()=>driver.Session.DeliveredGenerationCount==1,5,"Automatic driver applies");
   Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(driver.Session.LastApplication.Accepted,Is.True);Assert.That(GameRuntimeOptions.UseLLM,Is.True);Assert.That(driver.Session.ConsecutiveFaults,Is.Zero);Assert.That(factory.Requests,Is.EqualTo(1));Assert.That(factory.Creates,Is.EqualTo(1));driver.SetEnabled(false);yield return Until(()=>driver.Session.CleanupComplete,5,"Toggle release");Assert.That(GameRuntimeOptions.UseLLM,Is.False);
  }
  [UnityTest]public IEnumerator DriverInstallsOneModeToggleAndReenableDoesNotDuplicateIt()
  {
   yield return new EnterPlayMode();yield return Setup();Assert.That(station.GetComponent<LlmModeToggle>(),Is.Not.Null);Assert.That(station.GetComponent<StationControls>().Canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true).Count(b=>b.name=="S3_AI_Mode"),Is.EqualTo(1));driver.enabled=false;driver.enabled=true;yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();Assert.That(station.GetComponent<StationControls>().Canvas.GetComponentsInChildren<UnityEngine.UI.Button>(true).Count(b=>b.name=="S3_AI_Mode"),Is.EqualTo(1));Assert.That(factory.Creates,Is.Zero);Assert.That(GameRuntimeOptions.UseLLM,Is.False);
  }
  [UnityTest]public IEnumerator KnownChangeAndMemoryReplacementDropOldRepliesAndDetachOldStore()
  {
   yield return new EnterPlayMode();yield return Setup();factory.Work.Reset();yield return ReadyOwner();Send("跟随我");yield return Until(()=>Volatile.Read(ref factory.Requests)==1,5,"First controlled request");station.Memories.Store.AddEvent("fixture_world_change",station.Elapsed,"发现新的可信信息。");factory.Work.Set();yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,5,"World change drains");Assert.That(driver.Session.DeliveredGenerationCount,Is.Zero);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));
   clock.Advance();factory.Work.Reset();Send("跟随我");yield return Until(()=>Volatile.Read(ref factory.Requests)==2,5,"Second controlled request");var old=station.Memories.Store;Assert.That(station.Memories.TryRestore(station.Memories.CaptureJson(),out _),Is.True);Assert.That(station.Knowledge.Synchronize(station.Memories.Store),Is.True);factory.Work.Set();yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,5,"Memory restore drains");Assert.That(driver.Session.DeliveredGenerationCount,Is.Zero);
   clock.Advance();factory.Work.Reset();Send("跟随我");yield return Until(()=>Volatile.Read(ref factory.Requests)==3,5,"Third controlled request");old.AddEvent("fixture_obsolete_store",station.Elapsed,"旧对象发生变化。");factory.Work.Set();yield return Until(()=>driver.Session.DeliveredGenerationCount==1,5,"Old store no longer cancels new request");Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));
  }
  [UnityTest]public IEnumerator ThirdFaultMirrorsGlobalFalseAndDoesNotReenableOnFollowingFrames()
  {
   yield return new EnterPlayMode();yield return Setup();factory.Raw="{bad-json";yield return ReadyOwner();for(var i=1;i<=3;i++){if(i>1)clock.Advance();Send("跟随我");var expected=i;yield return Until(()=>driver.Session.DeliveredGenerationCount==expected,5,"Malformed driver completion");Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));}
   Assert.That(GameRuntimeOptions.UseLLM,Is.False);for(var i=0;i<5;i++)yield return R04ShadowProbe.Frame();Assert.That(driver.Enabled,Is.False);Assert.That(factory.Creates,Is.EqualTo(1));Assert.That(factory.Requests,Is.EqualTo(3));Send("跟随我");Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(factory.Requests,Is.EqualTo(3));
   factory.Raw="{\"intent\":\"follow\"}";driver.SetEnabled(true);yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,5,"Explicit reopen");Assert.That(driver.Session.ConsecutiveFaults,Is.Zero);Assert.That(GameRuntimeOptions.UseLLM,Is.True);Assert.That(factory.Peak,Is.EqualTo(1));
  }
  [UnityTest]public IEnumerator DisableReenableDuringKernelCannotOverlapOwnersOrKeepThinkingFlag()
  {
   yield return new EnterPlayMode();yield return Setup();factory.Work.Reset();yield return ReadyOwner();Send("跟随我");yield return Until(()=>Volatile.Read(ref factory.Requests)==1,5,"Controlled request");driver.enabled=false;Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(station.Echo.GetComponent<EchoPresentation>().InferenceThinking,Is.False);driver.enabled=true;yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();Assert.That(driver.SetEnabled(true),Is.True);Assert.That(factory.Creates,Is.EqualTo(1));factory.Work.Set();yield return Until(()=>driver.Session.Phase==RequestPhase.Ready&&Volatile.Read(ref factory.Creates)==2,5,"Restart after full release");Assert.That(factory.Peak,Is.EqualTo(1));Assert.That(driver.Session.DeliveredGenerationCount,Is.Zero);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(station.Dialogue.Canvas.GetComponentsInChildren<UnityEngine.UI.Text>(true).Count(t=>t.name=="AI_Status"),Is.EqualTo(1));
  }
  [UnityTest]public IEnumerator EndingSelectionInvalidatesBeforePollAndConfirmedEndingTurnsFeatureOff()
  {
   yield return new EnterPlayMode();yield return Setup();factory.Work.Reset();yield return ReadyOwner();Send("跟随我");yield return Until(()=>Volatile.Read(ref factory.Requests)==1,5,"Controlled request");station.Progress.TryRestore(new[]{"pz02","pz03","pz04","pz05"});var ending=station.GetComponent<StationEndingPanel>();ending.Choice.Select(station.Progress,"seal_exit");yield return R04ShadowProbe.Frame();factory.Work.Set();yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,5,"Ending selection drains");Assert.That(driver.Session.DeliveredGenerationCount,Is.Zero);ending.Choice.Confirm(station.Progress);yield return R04ShadowProbe.Frame();Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(driver.SetEnabled(true),Is.False);
  }
  [UnityTest]public IEnumerator CancelLoadingSignalsStopWithoutMainThreadWaitOrLaterAction()
  {
   yield return new EnterPlayMode();yield return Setup();factory.Load.Reset();driver.SetEnabled(true);yield return Until(()=>Volatile.Read(ref factory.Creates)==1,5,"Controlled load");var watch=System.Diagnostics.Stopwatch.StartNew();driver.FeedbackView.CancelButton.onClick.Invoke();Assert.That(watch.Elapsed.TotalMilliseconds,Is.LessThan(100));Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(factory.Disposed,Is.Zero);factory.Load.Set();yield return Until(()=>driver.Session.CleanupComplete,5,"Cancelled load cleanup");Assert.That(factory.Requests,Is.Zero);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));
  }
  [UnityTearDown]public IEnumerator ReleaseDriverBeforeStation()
  {
   if(factory!=null){factory.Load.Set();factory.Work.Set();}if(driver&&driver.Ready){driver.enabled=false;var session=driver.Session;session.Dispose();yield return Until(()=>session.CleanupComplete,5,"Driver cleanup");Assert.That(factory.Disposed,Is.EqualTo(factory.Creates));Assert.That(factory.Active,Is.Zero);}
  }
 }
}
