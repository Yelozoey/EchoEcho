using EchoStation.AI.Execution;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class UseLlmRoutingTests
 {
  [Test]public void StartupIsScriptAndDoesNotNeedModel(){var route=new UseLlmRouting();Assert.That(route.Enabled,Is.False);Assert.That(route.Route,Is.EqualTo(InputRoute.Script));Assert.That(route.ConsecutiveFaults,Is.Zero);}
  [Test]public void ThirdConsecutiveFaultChangesOnlyFutureInputRouting()
  {var route=new UseLlmRouting();route.SetEnabled(true);Assert.That(route.RecordFault(),Is.False);Assert.That(route.RecordFault(),Is.False);Assert.That(route.Route,Is.EqualTo(InputRoute.Inference));Assert.That(route.RecordFault(),Is.True);Assert.That(route.Route,Is.EqualTo(InputRoute.Script));Assert.That(route.ConsecutiveFaults,Is.EqualTo(3));Assert.That(route.AutomaticallyDisabled,Is.True);}
  [Test]public void SuccessSeparatesFaultSequences()
  {var route=new UseLlmRouting();route.SetEnabled(true);route.RecordFault();route.RecordFault();route.RecordSuccess();Assert.That(route.ConsecutiveFaults,Is.Zero);route.RecordFault();Assert.That(route.ConsecutiveFaults,Is.EqualTo(1));Assert.That(route.Enabled,Is.True);}
  [Test]public void DisabledFailuresAreIgnoredAndExplicitReenableStartsClean()
  {var route=new UseLlmRouting();Assert.That(route.RecordFault(),Is.False);Assert.That(route.ConsecutiveFaults,Is.Zero);route.SetEnabled(true);route.RecordFault();route.RecordFault();route.RecordFault();Assert.That(route.RecordFault(),Is.False);route.SetEnabled(true);Assert.That(route.Enabled,Is.True);Assert.That(route.ConsecutiveFaults,Is.Zero);Assert.That(route.AutomaticallyDisabled,Is.False);}
  [Test]public void RepeatedEnableDoesNotEraseExistingFaults()
  {var route=new UseLlmRouting();route.SetEnabled(true);route.RecordFault();route.SetEnabled(true);Assert.That(route.ConsecutiveFaults,Is.EqualTo(1));route.SetEnabled(false);Assert.That(route.Route,Is.EqualTo(InputRoute.Script));}
 }
}
