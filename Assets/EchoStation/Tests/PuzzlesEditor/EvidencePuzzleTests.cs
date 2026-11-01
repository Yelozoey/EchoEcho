using System;
using System.Linq;
using EchoStation.Puzzles;
using EchoStation.Story;
using NUnit.Framework;
namespace EchoStation.Tests.Puzzles
{
 public sealed class EvidencePuzzleTests
 {
  [Test]public void P01_A01_BothValleySourcesRequireExplicitConfirmAndOrderDoesNotMatter(){var p=new PuzzleProgress();var sources=StationScript.Puzzles.Single(s=>s.Id=="pz01").Sources.Reverse().ToArray();Assert.That(p.Has("pz01"),Is.False);Assert.That(EvidencePuzzle.Confirm(p,"pz01",sources),Is.EqualTo(PuzzleResult.Completed));Assert.That(p.Has("pz01"),Is.True);Assert.That(p.Has("pz02"),Is.False);}
  [Test]public void P01_A02_EachMissingSourceAndUnrelatedSourcesCannotComplete(){foreach(var id in new[]{"pz01","pz06"}){var spec=StationScript.Puzzles.Single(s=>s.Id==id);for(var i=0;i<spec.Sources.Count;i++){var p=new PuzzleProgress();Assert.That(EvidencePuzzle.Confirm(p,id,spec.Sources.Where((_,n)=>n!=i).Concat(new[]{"unrelated_source"})),Is.EqualTo(PuzzleResult.MissingClues));Assert.That(p.Capture(),Is.Empty);}}}
  [Test]public void P01_A03_CoreRequiresMainRouteAndAllThreeSources(){var p=new PuzzleProgress();var sources=StationScript.Puzzles.Single(s=>s.Id=="pz06").Sources;Assert.That(EvidencePuzzle.Confirm(p,"pz06",sources),Is.EqualTo(PuzzleResult.PrerequisiteMissing));Assert.That(p.TryRestore(new[]{"pz02","pz03","pz04","pz05"}),Is.True);Assert.That(EvidencePuzzle.Confirm(p,"pz06",sources),Is.EqualTo(PuzzleResult.Completed));Assert.That(p.Has("pz01"),Is.False);}
  [Test]public void P01_A04_RepeatedConfirmationPublishesOnlyOneCompletion(){var p=new PuzzleProgress();var changed=0;p.Completed+=_=>changed++;var ids=StationScript.Puzzles[0].Sources;Assert.That(EvidencePuzzle.Confirm(p,"pz01",ids.Concat(ids)),Is.EqualTo(PuzzleResult.Completed));Assert.That(EvidencePuzzle.Confirm(p,"pz01",Array.Empty<string>()),Is.EqualTo(PuzzleResult.AlreadyComplete));Assert.That(changed,Is.EqualTo(1));}
  [Test]public void P01_A05_ProgressSnapshotIsIndependentAndInvalidRestoreIsAtomic(){var p=new PuzzleProgress();Assert.That(p.TryRestore(new[]{"pz02"}),Is.True);var before=p.Capture();foreach(var invalid in new[]{new[]{"pz03"},new[]{"pz02","pz02"},new[]{"other"},new[]{"pz06"}}){Assert.That(p.TryRestore(invalid),Is.False);CollectionAssert.AreEqual(before,p.Capture());}before[0]="other";Assert.That(p.Has("pz02"),Is.True);Assert.That(p.TryRestore(Array.Empty<string>()),Is.True);Assert.That(p.Capture(),Is.Empty);}
  [Test]public void P01_A06_UnsupportedActionsAndNullDataDoNotCreateProgress(){var p=new PuzzleProgress();foreach(var id in new[]{"pz02","pz04","other",null})Assert.That(EvidencePuzzle.Confirm(p,id,Array.Empty<string>()),Is.EqualTo(PuzzleResult.Invalid));Assert.That(EvidencePuzzle.Confirm(null,"pz01",Array.Empty<string>()),Is.EqualTo(PuzzleResult.Invalid));Assert.That(EvidencePuzzle.Confirm(p,"pz01",null),Is.EqualTo(PuzzleResult.Invalid));Assert.That(p.Capture(),Is.Empty);Assert.That(typeof(EvidencePuzzle).Assembly.GetReferencedAssemblies().Any(a=>a.Name.StartsWith("UnityEngine",StringComparison.Ordinal)),Is.False);}
 }
}
