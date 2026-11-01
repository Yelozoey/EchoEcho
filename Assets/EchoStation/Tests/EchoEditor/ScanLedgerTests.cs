using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using EchoStation.Echo;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Echo
{
    public class ScanLedgerTests
    {
        private readonly List<GameObject> objects=new List<GameObject>();private readonly List<ClueDefinition> definitions=new List<ClueDefinition>();
        private WorldTarget Target(bool hidden=true,bool allowed=true,bool withClue=true)
        {
            var go=new GameObject("ScanLedgerFixture");go.SetActive(false);objects.Add(go);var target=go.AddComponent<WorldTarget>();target.Configure("scan_ledger_fixture","扫描夹具");var permissions=go.AddComponent<EchoTargetPermissions>();permissions.Configure(allowed,false);
            if(withClue)
            {
                var data=ScriptableObject.CreateInstance<ClueDefinition>();definitions.Add(data);data.Configure(target.Id,"线索","这是一份仅供玩家日志查看的完整原文。","简短扫描摘要。",hidden?ClueKind.Scan:ClueKind.Note);
                var renderer=go.AddComponent<MeshRenderer>();go.AddComponent<ClueSource>().Configure(data,renderer);
            }
            else permissions.SetScanFact("设备信号异常，建议保持距离。");go.SetActive(true);return target;
        }
        [Test]public void E08_A01_HiddenInformationCannotBeReadBeforeEchoCommitAndBecomesVisibleAfter()
        {
            var target=Target();var clue=target.GetComponent<ClueSource>();var renderer=target.GetComponent<MeshRenderer>();var original=target.transform.position;
            Assert.That(clue.TryRead(out _)||renderer.enabled,Is.False);var ledger=new EchoScanLedger();Assert.That(ledger.TryCommit(target,out var receipt),Is.True);
            Assert.That(receipt.HiddenMark&&receipt.IsNewDiscovery&&clue.IsRevealed&&renderer.enabled,Is.True);Assert.That(clue.TryRead(out var record),Is.True);Assert.That(receipt.Summary,Is.EqualTo(record.Summary));Assert.That(receipt.Summary,Is.Not.EqualTo(record.FullText));Assert.That(target.transform.position,Is.EqualTo(original));
        }
        [Test]public void E08_A02_DuplicateCommitKeepsOneDiscoveryEventAndStableIdSummary()
        {
            var target=Target();var ledger=new EchoScanLedger();var events=0;ledger.Discovered+=receipt=>events++;Assert.That(ledger.TryCommit(target,out var first),Is.True);Assert.That(ledger.TryCommit(target,out var second),Is.True);
            Assert.That(first.IsNewDiscovery,Is.True);Assert.That(second.IsNewDiscovery,Is.False);Assert.That(second.TargetId,Is.EqualTo(first.TargetId));Assert.That(second.Summary,Is.EqualTo(first.Summary));Assert.That(events,Is.EqualTo(1));Assert.That(ledger.GetDiscoveredIds(),Is.EqualTo(new[]{target.Id}));
        }
        [TestCase(false)][TestCase(true)]public void E08_A03_NoteAndEquipmentResultsOnlyCarrySummaries(bool equipment)
        {
            var target=Target(false,true,!equipment);var ledger=new EchoScanLedger();Assert.That(ledger.TryCommit(target,out var receipt),Is.True);Assert.That(receipt.HiddenMark,Is.False);Assert.That(receipt.Summary.Length,Is.LessThanOrEqualTo(120));
            Assert.That(typeof(EchoScanReceipt).GetProperties().Any(p=>p.Name.Contains("Full")||p.PropertyType==typeof(ClueRecord)||p.PropertyType==typeof(ClueDefinition)),Is.False);
        }
        [Test]public void E08_A04_UnapprovedOrInactiveTargetCannotRevealOrEmitReceipt()
        {
            var target=Target(true,false);var clue=target.GetComponent<ClueSource>();var ledger=new EchoScanLedger();Assert.That(ledger.TryCommit(target,out _),Is.False);Assert.That(clue.IsRevealed||ledger.HasDiscovered(target.Id),Is.False);
            target.GetComponent<EchoTargetPermissions>().Configure(true,false);target.gameObject.SetActive(false);Assert.That(ledger.TryCommit(target,out _),Is.False);Assert.That(clue.IsRevealed,Is.False);Assert.That(ledger.TryCommit(null,out _),Is.False);
        }
        [Test]public void E08_A05_PlayerFacingApisHaveNoPublicRevealOrCommitEntryPoint()
        {
            Assert.That(typeof(ClueSource).GetMethod("TryRevealFromEcho",BindingFlags.Public|BindingFlags.Instance),Is.Null);Assert.That(typeof(EchoScanLedger).GetMethod("TryCommit",BindingFlags.Public|BindingFlags.Instance),Is.Null);
            var friends=typeof(ClueSource).Assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute),false).Cast<InternalsVisibleToAttribute>().Select(a=>a.AssemblyName).ToArray();Assert.That(friends,Does.Contain("EchoStation.Echo.Runtime"));Assert.That(friends.Any(n=>n.Contains("Player")),Is.False);
        }
        [TearDown]public void After(){foreach(var go in objects)if(go)Object.DestroyImmediate(go);foreach(var data in definitions)if(data)Object.DestroyImmediate(data);objects.Clear();definitions.Clear();}
    }
}
