using System;
using EchoStation.Puzzles;
using NUnit.Framework;
namespace EchoStation.Tests.Puzzles
{
 public sealed class ScanPuzzleTests
 {
  [Test]public void P04_A01_BothOrdersAndDuplicatesProduceSameCredential(){foreach(var ids in new[]{new[]{"scan_mark_01","scan_lab_calibration_01"},new[]{"scan_lab_calibration_01","scan_mark_01","scan_mark_01"}}){var p=new PuzzleProgress();Assert.That(ScanPuzzle.Confirm(p,ids),Is.EqualTo(PuzzleResult.Completed));CollectionAssert.AreEqual(new[]{"pz04"},p.Capture());}}
  [Test]public void P04_A02_SingleNormalEquipmentAndWrongIdentityCannotGrantCredential(){foreach(var ids in new[]{Array.Empty<string>(),new[]{"scan_mark_01"},new[]{"scan_lab_calibration_01"},new[]{"power_panel_01","vent_terminal_01"},new[]{"scan_mark_01","scan_lab_calibration_02"}}){var p=new PuzzleProgress();Assert.That(ScanPuzzle.Confirm(p,ids),Is.EqualTo(PuzzleResult.MissingCredential));Assert.That(p.Capture(),Is.Empty);}}
  [Test]public void P04_A03_RepeatedReplayDoesNotPublishAgainOrAlterOtherPuzzles(){var p=new PuzzleProgress();p.TryRestore(new[]{"pz02"});var n=0;p.Completed+=_=>n++;ScanPuzzle.Confirm(p,new[]{"scan_mark_01","scan_lab_calibration_01"});Assert.That(ScanPuzzle.Confirm(p,Array.Empty<string>()),Is.EqualTo(PuzzleResult.AlreadyComplete));Assert.That(n,Is.EqualTo(1));Assert.That(p.Has("pz03"),Is.False);Assert.That(p.Has("pz05"),Is.False);}
  [Test]public void P04_A04_InvalidInputsDoNotMutateProgress(){var p=new PuzzleProgress();Assert.That(ScanPuzzle.Confirm(p,null),Is.EqualTo(PuzzleResult.Invalid));Assert.That(ScanPuzzle.Confirm(null,Array.Empty<string>()),Is.EqualTo(PuzzleResult.Invalid));Assert.That(p.Capture(),Is.Empty);}
 }
}
