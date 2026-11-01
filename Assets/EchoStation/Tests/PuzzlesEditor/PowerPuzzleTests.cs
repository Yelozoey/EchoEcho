using System;
using EchoStation.Puzzles;
using NUnit.Framework;
namespace EchoStation.Tests.Puzzles
{
 public sealed class PowerPuzzleTests
 {
  [Test]public void P03_A01_ExactlyOneOfEightCircuitCombinationsCanPowerVent(){for(var bits=0;bits<8;bits++){var p=new PuzzleProgress();p.TryRestore(new[]{"pz02"});var result=PowerPuzzle.Submit(p,(bits&1)!=0,(bits&2)!=0,(bits&4)!=0);Assert.That(result,Is.EqualTo(bits==1?PuzzleResult.Completed:PuzzleResult.WrongCircuit));Assert.That(p.Has("pz03"),Is.EqualTo(bits==1));}}
  [Test]public void P03_A02_RequiresCabinPermissionAndDoesNotOpenAnyDoor(){var p=new PuzzleProgress();Assert.That(PowerPuzzle.Submit(p,true,false,false),Is.EqualTo(PuzzleResult.PrerequisiteMissing));Assert.That(p.Capture(),Is.Empty);Assert.That(PowerPuzzle.Submit(null,true,false,false),Is.EqualTo(PuzzleResult.Invalid));}
  [Test]public void P03_A03_SuccessCannotBeRelockedOrRepublished(){var p=new PuzzleProgress();p.TryRestore(new[]{"pz02"});var n=0;p.Completed+=_=>n++;PowerPuzzle.Submit(p,true,false,false);Assert.That(PowerPuzzle.Submit(p,false,true,true),Is.EqualTo(PuzzleResult.AlreadyComplete));Assert.That(n,Is.EqualTo(1));Assert.That(p.Has("pz03"),Is.True);}
  [Test]public void P03_A04_ActualCycleIsClosedUntilExactlyThreeSecondsAndBeginIsIdempotent(){var c=new VentilationCycle();c.Step(10);Assert.That(c.Elapsed,Is.Zero);Assert.That(c.PassageReady,Is.False);c.Begin();c.Step(1);c.Begin();Assert.That(c.Elapsed,Is.EqualTo(1));c.Step(1.99f);Assert.That(c.PassageReady,Is.False);c.Step(.01f);Assert.That(c.PassageReady,Is.True);c.Step(100);Assert.That(c.Fraction,Is.EqualTo(1));}
  [Test]public void P03_A05_MidClearRestoreContinuesAndInvalidRestoreDoesNotPartiallyApply(){var c=new VentilationCycle();Assert.That(c.TryRestore(true,1.2f),Is.True);c.Step(1.8f);Assert.That(c.PassageReady,Is.True);foreach(var value in new[]{-1f,4f,float.NaN,float.PositiveInfinity}){Assert.That(c.TryRestore(true,value),Is.False);Assert.That(c.Elapsed,Is.EqualTo(3));}Assert.That(c.TryRestore(false,1),Is.False);Assert.That(c.Powered,Is.True);Assert.That(c.TryRestore(false,0),Is.True);Assert.That(c.PassageReady,Is.False);}
  [Test]public void P03_A06_InvalidTimeNeverChangesState(){var c=new VentilationCycle();c.Begin();foreach(var value in new[]{-.1f,float.NaN,float.PositiveInfinity})Assert.Throws<ArgumentOutOfRangeException>(()=>c.Step(value));Assert.That(c.Elapsed,Is.Zero);}
 }
}
