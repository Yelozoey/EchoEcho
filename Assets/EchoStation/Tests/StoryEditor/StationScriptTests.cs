using System;
using System.Linq;
using EchoStation.Memory;
using EchoStation.Story;
using NUnit.Framework;

namespace EchoStation.Tests.Story
{
    public sealed class StationScriptTests
    {
        [Test] public void D06_B01_SixInstancesHaveExactZonesAndResolvableAcyclicDependencies()
        {
            Assert.That(StationScript.Puzzles.Count, Is.EqualTo(6));
            CollectionAssert.AreEqual(new[]{"valley","cabin","lab_a","lab_a","vent","core"}, StationScript.Puzzles.Select(p=>p.Zone));
            foreach(var p in StationScript.Puzzles) {
                Assert.That(MemoryLimits.IsValidId(p.CompletionId), Is.True);
                Assert.That(p.Sources.All(s=>StationClues.TryGet(s,out _)), Is.True);
                Assert.That(p.Previous.All(d=>StationScript.Puzzles.TakeWhile(a=>a.Id!=p.Id).Any(a=>a.Id==d)), Is.True);
            }
            Assert.That(StationScript.Puzzles.Single(p=>p.Id=="pz02").Sources, Is.Empty, "Cabin allows guessed code without forged reading.");
            CollectionAssert.AreEquivalent(new[]{"scan_mark_01","scan_lab_calibration_01"}, StationScript.Puzzles.Single(p=>p.Id=="pz04").Sources);
        }
        [Test] public void D06_B02_ThreeExplicitEndingRequirementsMatchApproval()
        {
            Assert.That(StationScript.Endings.Select(e=>e.Id).Distinct().Count(), Is.EqualTo(3));
            CollectionAssert.AreEquivalent(new[]{"pz02","pz03","pz04","pz05"}, StationScript.Endings[0].Required);
            CollectionAssert.AreEquivalent(new[]{"pz02","pz03","pz04","pz05","pz06"}, StationScript.Endings[1].Required);
            CollectionAssert.AreEquivalent(StationScript.Puzzles.Select(p=>p.Id), StationScript.Endings[2].Required);
            CollectionAssert.AreEqual(new[]{false,true,false},StationScript.Endings.Select(e=>e.EchoStays));
            Assert.That(StationScript.Endings.All(e=>e.Consequence.Length<=280),Is.True);
        }
        [Test] public void D06_B03_EveryHintIsBoundedResolvableAndNeverContainsCode()
        {
            Assert.That(StationScript.Hints.Count,Is.EqualTo(12)); Assert.That(StationScript.Hints.Select(h=>h.Id).Distinct().Count(),Is.EqualTo(12));
            foreach(var h in StationScript.Hints) {
                Assert.That(h.Levels.Count,Is.EqualTo(3)); Assert.That(h.Levels.All(l=>l.Length<=280&&!l.Contains("7316")&&!l.Contains("0428")),Is.True);
                Assert.That(h.Required.All(r=>r.Category==MemoryCategory.Clue&&StationClues.TryGet(r.Id,out _)),Is.True);
                if(h.CompletedEventId!=null)Assert.That(StationScript.Puzzles.Any(p=>p.CompletionId==h.CompletedEventId),Is.True);
            }
        }
        [Test] public void D06_B04_AllScriptLinesAreUniqueBoundedAndGateSpecificFacts()
        {
            Assert.That(StationScript.Lines.Count,Is.GreaterThanOrEqualTo(24)); Assert.That(StationScript.Lines.Select(l=>l.Id).Distinct().Count(),Is.EqualTo(StationScript.Lines.Count));
            var store=new MemoryStore();
            foreach(var line in StationScript.Lines) {
                Assert.That(MemoryLimits.IsValidId(line.Id),Is.True);Assert.That(line.Text.Length,Is.InRange(1,280));
                Assert.That(line.Text,Does.Not.Contain("7316"));Assert.That(line.Text,Does.Not.Contain("0428"));
                foreach(var id in line.KnownClues)Assert.That(StationClues.TryGet(id,out _),Is.True);
                Assert.That(StationScript.KnownLine(line.Id,store.CaptureSnapshot()),Is.EqualTo(line.KnownClues.Count==0?line.Text:"我目前没有相关记录。"));
            }
            Assert.That(StationScript.KnownLine("not_a_line",store.CaptureSnapshot()),Is.EqualTo("我目前没有相关记录。"));
        }
    }
}
