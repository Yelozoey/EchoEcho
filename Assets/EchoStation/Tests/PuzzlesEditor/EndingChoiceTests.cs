using System.Linq;
using EchoStation.Puzzles;
using EchoStation.Story;
using NUnit.Framework;
namespace EchoStation.Tests.Puzzles
{
 public sealed class EndingChoiceTests
 {
  [TestCase("seal_exit")][TestCase("monitor_stay")][TestCase("archive_exit")]
  public void F02_A01_ExactlyApprovedMinimumConditionsUnlockEachEnding(string id){var p=new PuzzleProgress();Assert.That(p.TryRestore(StationScript.Endings.Single(e=>e.Id==id).Required),Is.True);var c=new EndingChoice();Assert.That(c.Select(p,id),Is.EqualTo(EndingResult.Pending));Assert.That(c.EndingId,Is.Null);Assert.That(c.Confirm(p),Is.EqualTo(EndingResult.Confirmed));Assert.That(c.EndingId,Is.EqualTo(id));}
  [Test]public void F02_A02_MissingUnknownAndNoSelectionDoNotEnd(){var c=new EndingChoice();var p=new PuzzleProgress();Assert.That(c.Select(p,"seal_exit"),Is.EqualTo(EndingResult.Locked));Assert.That(c.Missing(p,"seal_exit").Length,Is.EqualTo(4));Assert.That(c.Select(p,"random"),Is.EqualTo(EndingResult.Unknown));Assert.That(c.Confirm(p),Is.EqualTo(EndingResult.NoSelection));Assert.That(c.EndingId,Is.Null);}
  [Test]public void F02_A03_CancelAndChangedPrerequisiteRequireNewConfirmation(){var c=new EndingChoice();var p=new PuzzleProgress();p.TryRestore(StationScript.Endings[0].Required);c.Select(p,"seal_exit");Assert.That(c.Cancel(),Is.EqualTo(EndingResult.Cancelled));Assert.That(c.Confirm(p),Is.EqualTo(EndingResult.NoSelection));c.Select(p,"seal_exit");p.TryRestore(new string[0]);Assert.That(c.Confirm(p),Is.EqualTo(EndingResult.Locked));Assert.That(c.EndingId,Is.Null);}
  [Test]public void F02_A04_AllConditionsNeverAutoChooseAndFinalResultIsMutuallyExclusive(){var p=new PuzzleProgress();p.TryRestore(StationScript.Puzzles.Select(s=>s.Id));var c=new EndingChoice();Assert.That(c.EndingId,Is.Null);c.Select(p,"archive_exit");c.Confirm(p);Assert.That(c.Select(p,"monitor_stay"),Is.EqualTo(EndingResult.AlreadyEnded));Assert.That(c.Confirm(p),Is.EqualTo(EndingResult.AlreadyEnded));Assert.That(c.Cancel(),Is.EqualTo(EndingResult.AlreadyEnded));Assert.That(c.EndingId,Is.EqualTo("archive_exit"));c.Reset();Assert.That(c.EndingId,Is.Null);Assert.That(c.PendingId,Is.Null);}
 }
}
