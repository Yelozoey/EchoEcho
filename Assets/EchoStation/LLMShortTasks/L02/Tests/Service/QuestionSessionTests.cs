using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using EchoStation.AI.Context;
using EchoStation.AI.Inference;
using EchoStation.L01;
using NUnit.Framework;
namespace EchoStation.L02.Tests
{
    public sealed class QuestionTestClock:IMonotonicClock
    {long ticks=100;public long Now=>Interlocked.Read(ref ticks);public long Frequency=>1000;public void Advance(long value)=>Interlocked.Add(ref ticks,value);}
    public sealed class QuestionTestFactory:ILocalModelFactory
    {
        public string Raw="{\"intent\":\"answer\",\"content\":\"用扳手处理。\"}";public volatile bool Hold;public int Main,Worker,DisposedThread;public volatile bool Entered;
        public ILocalModelRuntime Create(int main,CancellationToken token){Main=main;Worker=Thread.CurrentThread.ManagedThreadId;Assert.That(Worker,Is.Not.EqualTo(main));return new Runtime(this);}
        sealed class Runtime:ILocalModelRuntime
        {
            readonly QuestionTestFactory f;public Runtime(QuestionTestFactory factory){f=factory;}
            public GenerationResult Generate(TokenRequest request,CancellationToken token)
            {
                f.Entered=true;while(f.Hold){token.ThrowIfCancellationRequested();Thread.Sleep(2);}token.ThrowIfCancellationRequested();
                var summary=request.Snapshot.Known.Clues[0].Summary;
                var prepared=(PreparedPrompt)Activator.CreateInstance(typeof(PreparedPrompt),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{request.Snapshot,"test",new[]{1},request.Snapshot.Targets.ToArray(),summary,1,20},null);
                return (GenerationResult)Activator.CreateInstance(typeof(GenerationResult),BindingFlags.Instance|BindingFlags.NonPublic,null,new object[]{f.Raw,new List<int>{1},GenerationStop.Eos,.01,"test",prepared,Thread.CurrentThread.ManagedThreadId,0d,null},null);
            }
            public void Dispose(){f.DisposedThread=Thread.CurrentThread.ManagedThreadId;}
        }
    }
    public class QuestionSessionTests
    {
        static EchoTextContext Known()=>new EchoTextContext(new[]{new EchoTextTarget("valve","阀门",true,"扳手可关闭阀门。")},"valve",90);
        static QuestionCapture Capture()=>SceneQuestion.Select("这个怎么处理",Known(),"valve","v1").Capture;
        static void Until(Func<bool> done){var timer=System.Diagnostics.Stopwatch.StartNew();while(!done()){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(3));Thread.Sleep(2);}}
        [Test]public void RealWorkerRepliesReadOnlyAndEnforcesCompletionCooldown()
        {
            var factory=new QuestionTestFactory();var clock=new QuestionTestClock();var session=new SceneQuestionSession(factory,Thread.CurrentThread.ManagedThreadId,clock);
            try{session.SetEnabled(true);Until(()=>{session.Poll(Known(),"v1");return session.Phase==RequestPhase.Ready;});Assert.That(session.Submit(Capture()),Is.EqualTo(Admission.Accepted));Assert.That(session.Submit(Capture()),Is.EqualTo(Admission.Busy));QuestionReply reply=null;Until(()=>{session.Poll(Known(),"v1");return session.TryTake(out reply);});Assert.That(reply.Success,Is.True,reply.Error);Assert.That(reply.Content,Does.Contain("扳手"));Assert.That(factory.Worker,Is.Not.EqualTo(factory.Main));Assert.That(session.Submit(Capture()),Is.EqualTo(Admission.CoolingDown));clock.Advance(300);Assert.That(session.Submit(Capture()),Is.EqualTo(Admission.Accepted));}
            finally{session.Dispose();Until(()=>session.CleanupComplete);}Assert.That(factory.DisposedThread,Is.EqualTo(factory.Worker));
        }
        [Test]public void CancelAndChangedStateDropLateCompletionWithoutRetry()
        {
            var f=new QuestionTestFactory{Hold=true};var s=new SceneQuestionSession(f,Thread.CurrentThread.ManagedThreadId);try{s.SetEnabled(true);Until(()=>{s.Poll(Known(),"v1");return s.Phase==RequestPhase.Ready;});s.Submit(Capture());Until(()=>f.Entered);s.Invalidate(LifecycleChange.LoadGame);f.Hold=false;Until(()=>{s.Poll(Known(),"v1");return s.Phase==RequestPhase.Ready;});Assert.That(s.TryTake(out _),Is.False);Assert.That(s.Busy,Is.False);}
            finally{s.Dispose();Until(()=>s.CleanupComplete);}
        }
        [Test]public void RevisionChangeRejectsInFlightReply()
        {
            var f=new QuestionTestFactory{Hold=true};var s=new SceneQuestionSession(f,Thread.CurrentThread.ManagedThreadId);try{s.SetEnabled(true);Until(()=>{s.Poll(Known(),"v1");return s.Phase==RequestPhase.Ready;});s.Submit(Capture());Until(()=>f.Entered);s.Poll(Known(),"v2");Assert.That(s.TryTake(out var reply),Is.True);Assert.That(reply.Success,Is.False);Assert.That(reply.Error,Does.Contain("变化"));Assert.That(s.Faults,Is.Zero);}
            finally{f.Hold=false;s.Dispose();Until(()=>s.CleanupComplete);}
        }
        [Test]public void ThreeNonQuestionIntentsDisableAiWithoutExecutingAnything()
        {
            var f=new QuestionTestFactory{Raw="{\"intent\":\"follow\"}"};var clock=new QuestionTestClock();var s=new SceneQuestionSession(f,Thread.CurrentThread.ManagedThreadId,clock);try{s.SetEnabled(true);Until(()=>{s.Poll(Known(),"v1");return s.Phase==RequestPhase.Ready;});for(int i=0;i<3;i++){Assert.That(s.Submit(Capture()),Is.EqualTo(Admission.Accepted));QuestionReply reply=null;Until(()=>{s.Poll(Known(),"v1");return s.TryTake(out reply);});Assert.That(reply.Success,Is.False);clock.Advance(300);}Assert.That(s.Enabled,Is.False);Assert.That(s.Faults,Is.EqualTo(3));}
            finally{s.Dispose();Until(()=>s.CleanupComplete);}
        }
        [Test]public void RequestTimeoutCancelsWorkerAndHasNoAutomaticResubmit()
        {
            var f=new QuestionTestFactory{Hold=true};var clock=new QuestionTestClock();var s=new SceneQuestionSession(f,Thread.CurrentThread.ManagedThreadId,clock);try{s.SetEnabled(true);Until(()=>{s.Poll(Known(),"v1");return s.Phase==RequestPhase.Ready;});s.Submit(Capture());Until(()=>f.Entered);clock.Advance(20001);s.Poll(Known(),"v1");Assert.That(s.TryTake(out var reply),Is.True);Assert.That(reply.Error,Does.Contain("RequestTimeout"));Assert.That(s.Busy,Is.False);Until(()=>{s.Poll(Known(),"v1");return s.Phase==RequestPhase.Ready;});Assert.That(s.TryTake(out _),Is.False);}
            finally{f.Hold=false;s.Dispose();Until(()=>s.CleanupComplete);}
        }
    }
}
