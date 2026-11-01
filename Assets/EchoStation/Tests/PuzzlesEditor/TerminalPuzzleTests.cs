using System.Linq;
using EchoStation.Puzzles;
using NUnit.Framework;
namespace EchoStation.Tests.Puzzles
{
 public sealed class TerminalPuzzleTests
 {
  [Test]public void P02_A01_CabinGuessWorksWithoutReadingAndGrantsOnlyCabinPermission(){var p=new PuzzleProgress();Assert.That(TerminalPuzzle.Submit(p,"cabin_terminal_01","7316"),Is.EqualTo(PuzzleResult.Completed));CollectionAssert.AreEqual(new[]{"pz02"},p.Capture());Assert.That(TerminalPuzzle.CanOpenDoor(p,"cabin_door_01"),Is.True);Assert.That(TerminalPuzzle.CanOpenDoor(p,"core_door_01"),Is.False);}
  [Test]public void P02_A02_WrongOrMalformedCodesNeverCreateProgress(){foreach(var c in new[]{null,"","731","73160"," 7316","７３１６","7316\n","0000"}){var p=new PuzzleProgress();Assert.That(TerminalPuzzle.Submit(p,"cabin_terminal_01",c),Is.EqualTo(PuzzleResult.WrongCode));Assert.That(p.Capture(),Is.Empty);}}
  [Test]public void P02_A03_GuessedVentCodeCannotBypassActualScans(){var p=new PuzzleProgress();Assert.That(TerminalPuzzle.Submit(p,"vent_terminal_01","0428"),Is.EqualTo(PuzzleResult.MissingCredential));Assert.That(p.TryRestore(new[]{"pz02","pz03","pz04"}),Is.True);Assert.That(TerminalPuzzle.Submit(p,"vent_terminal_01","0428",false),Is.EqualTo(PuzzleResult.MissingCredential));Assert.That(p.Has("pz05"),Is.False);}
  [Test]public void P02_A04_EvenVerifiedCredentialRequiresVentPower(){var p=new PuzzleProgress();p.TryRestore(new[]{"pz04"});Assert.That(TerminalPuzzle.Submit(p,"vent_terminal_01","0428",true),Is.EqualTo(PuzzleResult.PrerequisiteMissing));Assert.That(p.Has("pz05"),Is.False);}
  [Test]public void P02_A05_VentCorrectWrongAndRepeatedSubmissionAreDeterministic(){var p=new PuzzleProgress();p.TryRestore(new[]{"pz02","pz03","pz04"});var count=0;p.Completed+=_=>count++;Assert.That(TerminalPuzzle.Submit(p,"vent_terminal_01","7316",true),Is.EqualTo(PuzzleResult.WrongCode));Assert.That(TerminalPuzzle.Submit(p,"vent_terminal_01","0428",true),Is.EqualTo(PuzzleResult.Completed));Assert.That(TerminalPuzzle.Submit(p,"vent_terminal_01","bad",false),Is.EqualTo(PuzzleResult.AlreadyComplete));Assert.That(count,Is.EqualTo(1));Assert.That(TerminalPuzzle.CanOpenDoor(p,"core_door_01"),Is.True);}
  [Test]public void P02_A06_InvalidDevicesAndDoorIdsNeverAct(){var p=new PuzzleProgress();foreach(var id in new[]{null,"power_panel_01","core_terminal_01","other"})Assert.That(TerminalPuzzle.Submit(p,id,"7316"),Is.EqualTo(PuzzleResult.Invalid));Assert.That(TerminalPuzzle.Submit(null,"cabin_terminal_01","7316"),Is.EqualTo(PuzzleResult.Invalid));Assert.That(TerminalPuzzle.CanOpenDoor(p,"other"),Is.False);Assert.That(TerminalPuzzle.CanOpenDoor(null,"cabin_door_01"),Is.False);Assert.That(p.Capture(),Is.Empty);}
 }
}
