using System;
using System.Linq;
using NUnit.Framework;

namespace EchoStation.SalvageExpansion.Tests
{
    public sealed class InventoryTransactionTests
    {
        static InventoryFootprint Rect(int w, int h) => new InventoryFootprint(Enumerable.Range(0, h).SelectMany(y => Enumerable.Range(0, w).Select(x => new InventoryCell(x, y))));
        static InventoryItemDefinition D(string id, InventoryItemKind kind = InventoryItemKind.Salvage, int w = 1, int h = 1, float kg = 1) => new InventoryItemDefinition(id, id, kind, Rect(w, h), kg, 2);
        static InventoryStore Store() => new InventoryStore(new[] { D("tool", InventoryItemKind.Tool, 1, 3), D("battery", InventoryItemKind.Battery, 1, 2), D("core", InventoryItemKind.Core, 2, 3), D("loot"), D("heavy", kg: 12) });
        static string State(InventoryStore store) => store.Revision + ":" + string.Join(";", store.Records.Select(r => r.Id + "/" + r.Location + "/" + r.X + "/" + r.Y + "/" + r.Rotation + "/" + r.Slot));
        [Test] public void DefinitionsAndRegistrationRejectInvalidOrDuplicateItems()
        {
            Assert.Throws<ArgumentException>(() => new InventoryStore(new[] { D("a"), D("a") }));
            Assert.Throws<ArgumentException>(() => D("../bad"));
            Assert.Throws<ArgumentException>(() => D("a", kg: float.NaN));
            Assert.Throws<ArgumentException>(() => new InventoryStore(Array.Empty<InventoryItemDefinition>()));
        }
        [Test] public void FailedMovesDoNotChangeRecordsOrRevision()
        {
            var s = Store(); Assert.That(s.TryStow("tool", 0, 0, 0, out _), Is.True); string before = State(s);
            Assert.That(s.TryStow("core", 0, 0, 0, out _), Is.False);
            Assert.That(s.TryStow("core", 5, 3, 0, out _), Is.False);
            Assert.That(s.TryStow("core", 2, 0, 4, out _), Is.False);
            Assert.That(s.TryStow("heavy", 5, 3, 0, out _), Is.False);
            Assert.That(State(s), Is.EqualTo(before));
        }
        [Test] public void MoveCanReuseItsOwnCellsWithoutMovingOtherItems()
        {
            var s = Store(); s.TryStow("core", 0, 0, 0, out _); s.TryStow("loot", 5, 3, 0, out _);
            Assert.That(s.TryStow("core", 0, 0, 1, out _), Is.True);
            Assert.That(s.Get("core").Rotation, Is.EqualTo(1)); Assert.That(s.Get("loot").X, Is.EqualTo(5));
        }
        [Test] public void HeldItemReservesOriginalCellsAndOnlyOneCanBeEquipped()
        {
            var s = Store(); s.TryStow("tool", 0, 0, 0, out _); s.TryStow("loot", 1, 0, 0, out _);
            Assert.That(s.TryEquip("tool", out _), Is.True); string before = State(s);
            Assert.That(s.TryEquip("loot", out _), Is.False); Assert.That(s.TryStow("core", 0, 0, 0, out _), Is.False);
            Assert.That(State(s), Is.EqualTo(before)); Assert.That(s.TryUnequip(out _), Is.True);
            Assert.That(s.Get("tool").Location, Is.EqualTo(InventoryItemLocation.Backpack)); Assert.That(s.Get("tool").X, Is.Zero);
        }
        [Test] public void QuickStowUsesCurrentOrientationAndFirstRowMajorSpace()
        {
            var s = Store(); s.TryStow("loot", 0, 0, 0, out _);
            Assert.That(s.TryQuickStow("battery", out _), Is.True); Assert.That(s.Get("battery").X, Is.EqualTo(1)); Assert.That(s.Get("battery").Y, Is.Zero);
            Assert.That(s.Get("battery").Rotation, Is.Zero);
        }
        [Test] public void FullBagQuickStowDoesNotAutoDiscardOrRotate()
        {
            var s = new InventoryStore(new[] { D("full", w: 6, h: 4), D("other", w: 1, h: 2) }); s.TryQuickStow("full", out _); string before = State(s);
            Assert.That(s.TryQuickStow("other", out _), Is.False); Assert.That(State(s), Is.EqualTo(before));
        }
        [Test] public void InstallAndRecoveryKeepSingleBatteryOwnership()
        {
            var s = Store(); s.TryQuickStow("battery", out _);
            Assert.That(s.TryInstall("battery", "fan", out _), Is.True); Assert.That(s.Board().OccupiedCells.Count, Is.Zero);
            string before = State(s); Assert.That(s.TryInstall("battery", "data", out _), Is.False); Assert.That(s.TryQuickStow("battery", out _), Is.False);
            Assert.That(s.TryRecoverInstalled("battery", "data", 0, 0, 0, out _), Is.False); Assert.That(State(s), Is.EqualTo(before));
            Assert.That(s.TryRecoverInstalled("battery", "fan", 0, 0, 0, out _), Is.True); Assert.That(s.Get("battery").Slot, Is.Null);
        }
        [Test] public void OccupiedSocketAndInvalidRecoveryCannotDuplicateOrLoseItems()
        {
            var s = new InventoryStore(new[] { D("a", InventoryItemKind.Battery), D("b", InventoryItemKind.Battery), D("full", w: 6, h: 4) });
            s.TryQuickStow("a", out _); s.TryInstall("a", "fan", out _); s.TryQuickStow("b", out _); string before = State(s);
            Assert.That(s.TryInstall("b", "fan", out _), Is.False); Assert.That(State(s), Is.EqualTo(before)); s.TryDrop("b", out _); s.TryQuickStow("full", out _); before = State(s);
            Assert.That(s.TryRecoverInstalled("a", "fan", 0, 0, 0, out _), Is.False); Assert.That(State(s), Is.EqualTo(before));
        }
        [Test] public void BatchDeliveryIsAtomicAndRejectsEquipmentOrDuplicateIds()
        {
            var s = Store(); s.TryQuickStow("core", out _); s.TryQuickStow("loot", out _); s.TryQuickStow("tool", out _); string before = State(s);
            Assert.That(s.TryDeliver(new[] { "core", "tool" }, out _), Is.False); Assert.That(s.TryDeliver(new[] { "core", "core" }, out _), Is.False);
            Assert.That(State(s), Is.EqualTo(before)); Assert.That(s.TryDeliver(new[] { "core", "loot" }, out _), Is.True);
            before = State(s); Assert.That(s.TryDeliver(new[] { "core" }, out _), Is.False); Assert.That(s.TryQuickStow("core", out _), Is.False); Assert.That(State(s), Is.EqualTo(before));
        }
        [Test] public void DropCanHappenOnlyOnceAndRetainsOrientation()
        {
            var s = Store(); s.TryStow("tool", 0, 0, 1, out _); s.TryEquip("tool", out _);
            Assert.That(s.TryDrop("tool", out _), Is.True); Assert.That(s.HeldId, Is.Null); Assert.That(s.Get("tool").Rotation, Is.EqualTo(1));
            string before = State(s); Assert.That(s.TryDrop("tool", out _), Is.False); Assert.That(State(s), Is.EqualTo(before));
        }
        [Test] public void SnapshotRejectsIncompleteDuplicateOverlappingAndMultipleHeldStatesAtomically()
        {
            var s = Store(); string before = State(s); var records = s.Records.ToArray();
            Assert.That(s.TryRestore(records.Skip(1), out _), Is.False);
            Assert.That(s.TryRestore(records.Take(4).Concat(new[] { records[0] }), out _), Is.False);
            records[0] = new InventoryItemState("tool", InventoryItemLocation.Held, 0, 0); records[1] = new InventoryItemState("battery", InventoryItemLocation.Held, 1, 0);
            Assert.That(s.TryRestore(records, out _), Is.False);
            records[0] = new InventoryItemState("tool", InventoryItemLocation.Backpack, 0, 0); records[1] = new InventoryItemState("battery", InventoryItemLocation.Backpack, 0, 0);
            Assert.That(s.TryRestore(records, out _), Is.False); Assert.That(State(s), Is.EqualTo(before));
        }
        [Test] public void SnapshotRejectsInvalidLocationsSlotsEquipmentDeliveryAndOversizeWithoutPartialCommit()
        {
            var s = Store(); string before = State(s);
            foreach (var invalid in new[] { new InventoryItemState("tool", (InventoryItemLocation)99), new InventoryItemState("tool", InventoryItemLocation.Delivered), new InventoryItemState("tool", InventoryItemLocation.Installed, slot: "fan"), new InventoryItemState("tool", InventoryItemLocation.World, 0, 0), new InventoryItemState("tool", InventoryItemLocation.Backpack, 0, 0, -1), new InventoryItemState("heavy", InventoryItemLocation.Backpack, 0, 0) })
            {
                Assert.That(s.TryRestore(s.Records.Select(r => r.Id == invalid.Id ? invalid : r), out _), Is.False); Assert.That(State(s), Is.EqualTo(before));
            }
        }
    }
}
