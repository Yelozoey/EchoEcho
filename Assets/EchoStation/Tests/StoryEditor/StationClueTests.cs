using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Memory;
using EchoStation.Story;
using NUnit.Framework;

namespace EchoStation.Tests.Story
{
    public sealed class StationClueTests
    {
        private static IEnumerable<StationClue> Sources() => StationClues.All;
        [TestCaseSource(nameof(Sources))] public void D06_A01_EachSourceHasBoundedDistinctTextAndValidIdentity(StationClue source)
        {
            Assert.That(MemoryLimits.IsValidId(source.Id), Is.True); Assert.That(source.FullText.Length, Is.InRange(120, 240)); Assert.That(source.Summary.Length, Is.InRange(1, 120)); Assert.That(source.FullText, Is.Not.EqualTo(source.Summary));
            Assert.That(StationClues.TryGet(source.Id, out var canonical), Is.True); Assert.That(canonical, Is.SameAs(source));
            if (source.Password != null) { Assert.That(source.FullText, Does.Contain(source.Password)); Assert.That(source.Summary, Does.Not.Contain(source.Password)); }
        }
        [Test] public void D06_A02_FourteenSourcesHaveExactFiveZoneDistributionAndUniqueIds()
        {
            Assert.That(StationClues.All.Count, Is.EqualTo(14)); Assert.That(StationClues.All.Select(c => c.Id).Distinct().Count(), Is.EqualTo(14));
            foreach (var pair in new[] { ("valley", 2), ("cabin", 4), ("lab_a", 4), ("vent", 2), ("core", 2) }) Assert.That(StationClues.All.Count(c => c.ZoneId == pair.Item1), Is.EqualTo(pair.Item2));
        }
        [Test] public void D06_A03_OnlyApprovedCodesAndTwoScanMarkersExistInContent()
        {
            var codes = StationClues.All.Where(c => c.Password != null).ToArray(); CollectionAssert.AreEquivalent(new[] { "7316", "0428" }, codes.Select(c => c.Password));
            var scans = StationClues.All.Where(c => c.Kind == MemoryClueKind.Scan).ToArray(); Assert.That(scans.Length, Is.EqualTo(2));
            Assert.That(StationClues.TryGet("scan_mark_01", out var first), Is.True); Assert.That(first.KeyMarker, Is.EqualTo("scan_lab_calibration_01"));
            Assert.That(StationClues.TryGet(first.KeyMarker, out var second), Is.True); Assert.That(second.Password, Is.EqualTo("0428")); Assert.That(second.KeyMarker, Is.EqualTo("vent_calibration_valid"));
        }
        [Test] public void D06_A04_ExternalCollectionsAndMalformedContentCannotRewriteCatalog()
        {
            Assert.Throws<NotSupportedException>(() => ((IList<StationClue>)StationClues.All).Clear()); Assert.That(StationClues.TryGet(null, out _), Is.False); Assert.That(StationClues.TryGet("unknown", out _), Is.False);
            Assert.Throws<ArgumentException>(() => new StationClue("valid_id", "other_zone", "标题", new string('文', 150), "摘要", MemoryClueKind.Note));
            Assert.Throws<ArgumentException>(() => new StationClue("valid_id", "cabin", "标题", "太短", "摘要", MemoryClueKind.Note));
            Assert.That(typeof(StationClues).Assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("UnityEngine", StringComparison.Ordinal)), Is.False);
        }
    }
}
