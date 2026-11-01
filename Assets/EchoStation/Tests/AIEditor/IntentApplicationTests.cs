using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using EchoStation.AI.Authorization;
using EchoStation.AI.Context;
using EchoStation.AI.Execution;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Echo;
using EchoStation.Memory;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class IntentApplicationTests
 {
  // Controlled coordination fixture, never included in model quality measurements.
  private sealed class Factory:ILocalModelFactory
  {
   public string Raw="{\"intent\":\"follow\"}";public bool MissingPrepared,WrongSnapshot,Incomplete;public int Disposed;
   public ILocalModelRuntime Create(int main,CancellationToken token)=>new Runtime(this);
   private sealed class Runtime:ILocalModelRuntime
   {
    private readonly Factory f;public Runtime(Factory factory){f=factory;}
    public GenerationResult Generate(TokenRequest request,CancellationToken token)
    {
     var snapshot=f.WrongSnapshot?Snapshot("跟随我"):request.Snapshot;
     var prepared=f.MissingPrepared?null:(PreparedPrompt)Activator.CreateInstance(typeof(PreparedPrompt),BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{snapshot,"controlled coordination fixture",new[]{1},snapshot.Targets.ToArray(),"",0,0},null);
     return new GenerationResult(f.Raw,new List<int>{151645},f.Incomplete?GenerationStop.TokenLimit:GenerationStop.Eos,0,"controlled-test",prepared);
    }
    public void Dispose(){Interlocked.Increment(ref f.Disposed);}
   }
  }
  private InferenceSessionHost host;private Factory factory;
  private static InferenceSnapshot Snapshot(string input)=>new InferenceSnapshot(new MemoryStore(),input,0,"lab",Array.Empty<TargetCandidate>());
  private static IEnumerator Until(Func<bool> condition,InferenceSessionHost owner)
  {var time=Stopwatch.StartNew();while(!condition()&&time.Elapsed.TotalSeconds<5){owner.Poll();yield return null;}Assert.That(condition(),Is.True,"controlled application transition timeout");}
  private IEnumerator Complete(InferenceSnapshot snapshot,Action<StampedCompletion> receive)
  {
   host=new InferenceSessionHost(factory,Thread.CurrentThread.ManagedThreadId);host.SetEnabled(true);yield return Until(()=>host.Phase==RequestPhase.Ready,host);
   Assert.That(host.TrySubmit(snapshot,out _),Is.EqualTo(Admission.Accepted));StampedCompletion completed=null;
   yield return Until(()=>{if(host.TryTakeCompletion(out var value))completed=value;return completed!=null;},host);receive(completed);
  }
  private IntentApplicationResult Apply(StampedCompletion completion,InferenceSnapshot snapshot,Func<IntentRequest,bool> execute)=>LlmIntentApplication.Apply(host,completion,snapshot,Array.Empty<AllowedTarget>(),Array.Empty<TrustedTargetIdentity>(),new ScriptKnowledgeResponder(new MemorySession(),new MemorySearch()),1,execute);
  [UnityTest]public IEnumerator CurrentDeliveredResultCanExecuteOnlyOnce()
  {
   factory=new Factory();var snapshot=Snapshot("跟随我");StampedCompletion completion=null;yield return Complete(snapshot,x=>completion=x);var count=0;
   var first=Apply(completion,snapshot,r=>{count++;Assert.That(r.Kind,Is.EqualTo(IntentKind.Follow));return true;});
   Assert.That(first.Status,Is.EqualTo(ApplicationStatus.Applied));Assert.That(first.IsFault,Is.False);Assert.That(Apply(completion,snapshot,_=>{count++;return true;}).Status,Is.EqualTo(ApplicationStatus.Stale));Assert.That(count,Is.EqualTo(1));
  }
  [UnityTest]public IEnumerator LoadAfterDeliveryPreventsActionAndReply()
  {factory=new Factory();var snapshot=Snapshot("跟随我");StampedCompletion completion=null;yield return Complete(snapshot,x=>completion=x);host.Invalidate(LifecycleChange.LoadGame);var calls=0;Assert.That(Apply(completion,snapshot,_=>{calls++;return true;}).Status,Is.EqualTo(ApplicationStatus.Stale));Assert.That(calls,Is.Zero);}
  [UnityTest]public IEnumerator MissingPreparedContextCannotUseModelOutput()
  {factory=new Factory{MissingPrepared=true};var snapshot=Snapshot("跟随我");StampedCompletion completion=null;yield return Complete(snapshot,x=>completion=x);var calls=0;var result=Apply(completion,snapshot,_=>{calls++;return true;});Assert.That(result.Status,Is.EqualTo(ApplicationStatus.InvalidCompletion));Assert.That(result.IsFault,Is.True);Assert.That(calls,Is.Zero);}
  [UnityTest]public IEnumerator DifferentSubmittedSnapshotCannotClaimSameInputText()
  {factory=new Factory{WrongSnapshot=true};var snapshot=Snapshot("跟随我");StampedCompletion completion=null;yield return Complete(snapshot,x=>completion=x);Assert.That(Apply(completion,snapshot,_=>{Assert.Fail("wrong snapshot must not execute");return true;}).Status,Is.EqualTo(ApplicationStatus.InvalidCompletion));}
  [UnityTest]public IEnumerator MalformedJsonUsesExactFallbackOnceWithoutMovement()
  {
   factory=new Factory{Raw="{\"intent\":\"follow\",\"content\":\"bad\"}"};var snapshot=Snapshot("跟随我");StampedCompletion completion=null;yield return Complete(snapshot,x=>completion=x);var count=0;
   var result=Apply(completion,snapshot,r=>{count++;Assert.That(r.Kind,Is.EqualTo(IntentKind.Answer));Assert.That(r.Content,Is.EqualTo(IntentJsonParser.FallbackLine));return true;});Assert.That(result.Status,Is.EqualTo(ApplicationStatus.ParseFallback));Assert.That(result.IsFault,Is.True);Apply(completion,snapshot,_=>{count++;return true;});Assert.That(count,Is.EqualTo(1));
  }
  [UnityTest]public IEnumerator TokenLimitedEvenValidJsonCannotDispatchMovement()
  {factory=new Factory{Incomplete=true};var snapshot=Snapshot("跟随我");StampedCompletion completion=null;yield return Complete(snapshot,x=>completion=x);Assert.That(Apply(completion,snapshot,r=>{Assert.That(r.Kind,Is.EqualTo(IntentKind.Answer));return true;}).Status,Is.EqualTo(ApplicationStatus.ParseFallback));}
  [UnityTest]public IEnumerator GameExecutorRejectionIsNotRecordedAsSuccessOrRetried()
  {factory=new Factory();var snapshot=Snapshot("跟随我");StampedCompletion completion=null;yield return Complete(snapshot,x=>completion=x);var count=0;var result=Apply(completion,snapshot,_=>{count++;return false;});Assert.That(result.Status,Is.EqualTo(ApplicationStatus.ExecutorRejected));Assert.That(result.Accepted,Is.False);Apply(completion,snapshot,_=>{count++;return true;});Assert.That(count,Is.EqualTo(1));}
  [UnityTest]public IEnumerator ForbiddenModelFollowBecomesRefusalNotScriptAction()
  {factory=new Factory();var snapshot=Snapshot("你去替我关闭实验室的门");StampedCompletion completion=null;yield return Complete(snapshot,x=>completion=x);Assert.That(Apply(completion,snapshot,r=>{Assert.That(r.Kind,Is.EqualTo(IntentKind.Refuse));return true;}).Guard,Is.EqualTo(GuardDisposition.Refuse));}
  [UnityTearDown]public IEnumerator Cleanup()
  {if(host!=null){host.Dispose();yield return Until(()=>host.CleanupComplete,host);Assert.That(factory.Disposed,Is.EqualTo(1));host=null;}}
 }
}
