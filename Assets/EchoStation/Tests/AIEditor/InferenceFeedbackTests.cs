using System;
using EchoStation.AI.Execution;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class InferenceFeedbackTests
 {
  [TestCase(FeedbackPhase.Script,false,false)][TestCase(FeedbackPhase.Loading,true,true)][TestCase(FeedbackPhase.Ready,false,false)][TestCase(FeedbackPhase.Thinking,true,true)][TestCase(FeedbackPhase.Draining,true,false)][TestCase(FeedbackPhase.Failure,false,false)][TestCase(FeedbackPhase.CoolingDown,false,false)]
  public void EveryPhaseHasBoundedChineseFeedbackAndExactCancelOwnership(FeedbackPhase phase,bool thinking,bool cancel)
  {var state=InferenceFeedback.For(phase);Assert.That(state.Phase,Is.EqualTo(phase));Assert.That(state.Thinking,Is.EqualTo(thinking));Assert.That(state.CanCancel,Is.EqualTo(cancel));Assert.That(state.Message.Length,Is.InRange(1,80));}
  [Test]public void UnknownPhaseCannotCreateFeedback(){Assert.Throws<ArgumentOutOfRangeException>(()=>InferenceFeedback.For((FeedbackPhase)99));}
 }
}
