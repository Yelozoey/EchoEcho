using System;
using NUnit.Framework;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextDisplayTransactionTests
    {
        sealed class Backend:IDisplayBackend
        {
            public DisplaySelection Current=DisplaySelection.Default(),Last;public bool Immediate=true,ApplyOk=true,ThrowApply,ThrowCapture;public int Calls;
            public DisplaySelection Capture(){if(ThrowCapture)throw new InvalidOperationException("capture");return Current;}
            public bool TryApply(DisplaySelection value,out string error){Calls++;Last=value;if(Immediate)Current=value;if(ThrowApply)throw new InvalidOperationException("apply");error=ApplyOk?"":"denied";return ApplyOk;}
        }
        sealed class Writer:IDisplaySettingsWriter{public int Writes;public bool Ok=true,Throws;public DisplaySelection Saved;public bool TryWrite(DisplaySelection data,out string error){Writes++;if(Throws)throw new InvalidOperationException("write");Saved=data;error=Ok?"":"disk";return Ok;}}
        Backend b;Writer w;DisplayTransaction t;DisplaySelection desired;
        [SetUp]public void Setup(){b=new Backend();w=new Writer();t=new DisplayTransaction(b,w);desired=new DisplaySelection(RecoveryQuality.High,1600,900,DisplayWindowMode.Borderless);}
        [Test]public void ExplicitConfirmAfterObservedMatchIsOnlyCommit()
        {Assert.That(t.Begin(desired,0),Is.True);Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Applying));Assert.That(t.Confirm(0,true),Is.False);Assert.That(w.Writes,Is.Zero);t.Tick(.2,true);Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Applying));t.Tick(1.2,true);Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Confirming));Assert.That(t.Confirm(1.3,true),Is.True);Assert.That(w.Saved,Is.EqualTo(desired));Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Idle));}
        [Test]public void NativeApplyTimeoutRestoresExactCustomWindowWithoutWriting()
        {var custom=new DisplaySelection(RecoveryQuality.Low,1371,817,DisplayWindowMode.Windowed);b.Current=custom;b.Immediate=false;t.Begin(desired,0);t.Tick(3,true);Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Reverting));Assert.That(b.Last,Is.EqualTo(custom));t.Tick(3.1,true);Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Idle));Assert.That(w.Writes,Is.Zero);}
        [TestCase(false)][TestCase(true)]public void CancelOrFocusLossRollsBack(bool lost)
        {t.Begin(desired,0);t.Tick(.1,true);if(lost)t.Tick(1,false);else t.Cancel(1);t.Tick(1.1,true);Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Idle));Assert.That(b.Current,Is.EqualTo(DisplaySelection.Default()));Assert.That(w.Writes,Is.Zero);}
        [Test]public void ConfirmationTimeoutDoesNotWrite(){t.Begin(desired,0);t.Tick(0,true);t.Tick(1,true);t.Tick(16,true);t.Tick(16.1,true);Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Idle));Assert.That(w.Writes,Is.Zero);}
        [Test]public void ExternalResizeInvalidatesConfirmation(){t.Begin(desired,0);t.Tick(0,true);t.Tick(1,true);b.Current=new DisplaySelection(RecoveryQuality.High,1400,800,DisplayWindowMode.Windowed);Assert.That(t.Confirm(1,true),Is.False);t.Tick(1.1,true);Assert.That(b.Current,Is.EqualTo(t.Before));Assert.That(w.Writes,Is.Zero);}
        [TestCase(false)][TestCase(true)]public void FailedOrThrowingWriterRestoresAndDoesNotReportCommit(bool throws)
        {w.Ok=false;w.Throws=throws;t.Begin(desired,0);t.Tick(0,true);t.Tick(1,true);Assert.That(t.Confirm(1,true),Is.False);t.Tick(1.1,true);Assert.That(b.Current,Is.EqualTo(t.Before));Assert.That(t.Message,Does.Contain("配置未保存"));Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Idle));}
        [TestCase(false)][TestCase(true)]public void PartialApplyFailureAlsoRollsBack(bool throws)
        {b.ApplyOk=false;b.ThrowApply=throws;t.Begin(desired,0);Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Reverting));t.Tick(.1,true);Assert.That(b.Current,Is.EqualTo(t.Before));Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Idle));Assert.That(w.Writes,Is.Zero);}
        [Test]public void FailedRollbackIsExplicitAndCannotStartAnotherRequest()
        {t.Begin(desired,0);t.Tick(0,true);t.Tick(1,true);b.Immediate=false;t.Cancel(1);t.Tick(4,true);Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Failed));Assert.That(t.Message,Does.Contain("无法确认"));Assert.That(t.Begin(desired,5),Is.False);Assert.That(w.Writes,Is.Zero);}
        [Test]public void PendingRequestCannotBeOverwrittenAndInvalidTimeRejected()
        {t.Begin(desired,0);Assert.That(t.Begin(DisplaySelection.Default(),1),Is.False);Assert.That(b.Calls,Is.EqualTo(1));Assert.Throws<ArgumentException>(()=>t.Tick(.5,true));Assert.Throws<ArgumentException>(()=>t.Tick(double.NaN,true));Assert.Throws<ArgumentException>(()=>t.Tick(double.PositiveInfinity,true));}
        [Test]public void TransientNativeMatchMustStayStableBeforeConfirm(){t.Begin(desired,0);t.Tick(0,true);b.Current=t.Before;t.Tick(.5,true);b.Current=desired;t.Tick(1,true);t.Tick(1.9,true);Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Applying));t.Tick(2,true);Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Confirming));Assert.That(w.Writes,Is.Zero);}
        [Test]public void CaptureFailureNeverAppliesOrWrites(){b.ThrowCapture=true;Assert.That(t.Begin(desired,0),Is.False);Assert.That(t.Phase,Is.EqualTo(DisplayPhase.Idle));Assert.That(b.Calls,Is.Zero);Assert.That(w.Writes,Is.Zero);}
    }
}
