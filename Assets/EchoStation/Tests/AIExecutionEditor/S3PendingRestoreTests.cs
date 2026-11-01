using System.Collections;
using System.IO;
using EchoStation.AI.Inference;
using EchoStation.Echo;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public sealed class S3PendingRestoreTests:S3PendingSaveFixture
 {
  [UnityTest]public IEnumerator LoadCompletesWithoutWaitingForKernelAndOldResultNeverApplies()
  {
   yield return new EnterPlayMode();yield return BootS3();BindTemporarySlot();Assert.That(persistence.TrySave(),Is.True,persistence.LastMessage);var bytes=File.ReadAllBytes(persistence.Files.PrimaryPath);yield return UseControlledOwner();yield return StartPendingFollow();var elapsed=System.Diagnostics.Stopwatch.StartNew();yield return persistence.Load();
   Assert.That(persistence.LastSucceeded,Is.True,persistence.LastMessage);Assert.That(elapsed.Elapsed.TotalSeconds,Is.LessThan(3));Assert.That(factory.Disposed,Is.Zero,"Kernel still owns native stand-in");Assert.That(factory.Active,Is.EqualTo(1));Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(driver.Session.DeliveredGenerationCount,Is.Zero);Assert.That(GameRuntimeOptions.UseLLM,Is.True,"Choice is not loaded from progress file");
   factory.Work.Set();yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,5,"Old load request drains");Assert.That(driver.Session.DeliveredGenerationCount,Is.Zero);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(File.ReadAllBytes(persistence.Files.PrimaryPath),Is.EqualTo(bytes));Assert.That(driver.Session.ConsecutiveFaults,Is.Zero);driver.SetEnabled(false);yield return Until(()=>driver.Session.CleanupComplete,5,"Restored session disable");
  }
 }
}
