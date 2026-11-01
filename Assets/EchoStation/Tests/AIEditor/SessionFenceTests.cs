using System;
using EchoStation.AI.Inference;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class SessionFenceTests
 {
  [TestCase(LifecycleChange.NewGame)]
  [TestCase(LifecycleChange.LoadGame)]
  [TestCase(LifecycleChange.ManualOverride)]
  [TestCase(LifecycleChange.WorldChanged)]
  [TestCase(LifecycleChange.Ending)]
  [TestCase(LifecycleChange.SceneUnloaded)]
  [TestCase(LifecycleChange.LLMDisabled)]
  [TestCase(LifecycleChange.LLMEnabled)]
  public void EveryLifecycleChangeInvalidatesAnAlreadyTakenResult(LifecycleChange reason)
  {var fence=new SessionFence();var old=fence.Issue();Assert.That(fence.IsCurrent(old),Is.True);fence.Change(reason);Assert.That(fence.IsCurrent(old),Is.False);Assert.That(fence.LastChange,Is.EqualTo(reason));var fresh=fence.Issue();Assert.That(fence.IsCurrent(fresh),Is.True);Assert.That(fresh.Request,Is.GreaterThan(old.Request));}
  [Test]public void NewGameAndLoadAreSeparateSessionsWhileWorldChangesKeepSession()
  {var fence=new SessionFence();var old=fence.Issue();fence.Change(LifecycleChange.WorldChanged);Assert.That(fence.Session,Is.EqualTo(old.Session));Assert.That(fence.Revision,Is.EqualTo(old.Revision+1));fence.Change(LifecycleChange.NewGame);Assert.That(fence.Session,Is.EqualTo(old.Session+1));Assert.That(fence.Revision,Is.Zero);fence.Change(LifecycleChange.LoadGame);Assert.That(fence.Session,Is.EqualTo(old.Session+2));}
  [Test]public void NewerRequestInvalidatesOlderEvenIfItsResultWasAlreadyTaken()
  {var fence=new SessionFence();Assert.That(fence.IsCurrent(default),Is.False);var first=fence.Issue();var second=fence.Issue();Assert.That(fence.IsCurrent(first),Is.False);Assert.That(fence.IsCurrent(second),Is.True);Assert.That(first.Session,Is.EqualTo(second.Session));Assert.That(first.Revision,Is.EqualTo(second.Revision));}
  [Test]public void UnknownLifecycleReasonCannotMutateCurrentStamp()
  {var fence=new SessionFence();var current=fence.Issue();Assert.Throws<ArgumentOutOfRangeException>(()=>fence.Change((LifecycleChange)99));Assert.That(fence.IsCurrent(current),Is.True);Assert.That(fence.LastChange,Is.Null);}
 }
}
