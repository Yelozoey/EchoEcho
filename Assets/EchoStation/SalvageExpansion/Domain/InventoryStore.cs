using System;
using System.Collections.Generic;
using System.Linq;

namespace EchoStation.SalvageExpansion
{
    public sealed class InventoryStore
    {
        readonly Dictionary<string, InventoryItemDefinition> definitions;
        IReadOnlyList<InventoryItemState> records;
        public IReadOnlyList<InventoryItemState> Records => records;
        public long Revision { get; private set; }
        public string HeldId { get { for(int i=0;i<records.Count;i++)if(records[i].Location==InventoryItemLocation.Held)return records[i].Id;return null; } }
        public IReadOnlyList<InventoryItemDefinition> Definitions { get; }

        public InventoryStore(IEnumerable<InventoryItemDefinition> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            var copy = items.ToArray();
            if (copy.Length == 0 || copy.Any(i => i == null) || copy.Select(i => i.Id).Distinct().Count() != copy.Length)
                throw new ArgumentException("Definitions must be nonempty and unique.", nameof(items));
            definitions = copy.ToDictionary(i => i.Id, StringComparer.Ordinal);
            Definitions = Array.AsReadOnly(copy);
            records = Array.AsReadOnly(copy.Select(i => new InventoryItemState(i.Id, InventoryItemLocation.World)).ToArray());
        }
        public InventoryItemDefinition Definition(string id) => definitions.TryGetValue(id ?? "", out var item) ? item : throw new ArgumentException("Unknown item.");
        public InventoryItemState Get(string id) { for(int i=0;i<records.Count;i++)if(records[i].Id==id)return records[i];throw new ArgumentException("Unknown item."); }
        public InventoryBoard Board(string excludingId = null) => new InventoryBoard(records.Where(r => r.OccupiesGrid && r.Id != excludingId)
            .SelectMany(r => Definition(r.Id).Footprint.RotateClockwise(r.Rotation).Cells.Select(c => new InventoryCell(r.X + c.X, r.Y + c.Y))));

        public bool CanStow(string id, int x, int y, int rotation, out string error)
        {
            var state = records.FirstOrDefault(r => r.Id == id);
            if (state == null) return Fail("未知物品。", out error);
            if (state.Location == InventoryItemLocation.Delivered || state.Location == InventoryItemLocation.Installed)
                return Fail("物品已交付或仍在设备插口。", out error);
            if (Definition(id).Kilograms > 8) return Fail("超过8kg单件上限。", out error);
            if (rotation < 0 || rotation > 3) return Fail("无效旋转方向。", out error);
            var result = Board(id).CheckPlacement(Definition(id).Footprint, x, y, rotation);
            if (result != InventoryPlacementStatus.Valid) return Fail(result == InventoryPlacementStatus.Overlap ? "目标格已被占用。" : "超出背包边界。", out error);
            error = ""; return true;
        }
        public bool TryStow(string id, int x, int y, int rotation, out string error)
        {
            if (!CanStow(id, x, y, rotation, out error)) return false;
            return Replace(new InventoryItemState(id, InventoryItemLocation.Backpack, x, y, rotation), out error);
        }
        public bool TryQuickStow(string id, out string error)
        {
            var state = records.FirstOrDefault(r => r.Id == id);
            if (state == null) return Fail("未知物品。", out error);
            for (int y = 0; y < InventoryBoard.Height; y++) for (int x = 0; x < InventoryBoard.Width; x++)
                if (CanStow(id, x, y, state.Rotation, out _)) return TryStow(id, x, y, state.Rotation, out error);
            return Fail("当前方向没有足够空间，请整理背包。", out error);
        }
        public bool TryEquip(string id, out string error)
        {
            var state = records.FirstOrDefault(r => r.Id == id);
            if (HeldId != null || state == null || state.Location != InventoryItemLocation.Backpack) return Fail("需要先卸下手持物，且目标必须在背包中。", out error);
            return Replace(new InventoryItemState(id, InventoryItemLocation.Held, state.X, state.Y, state.Rotation), out error);
        }
        public bool TryUnequip(out string error)
        {
            if (HeldId == null) return Fail("当前没有手持物。", out error);
            var state = Get(HeldId);
            return Replace(new InventoryItemState(state.Id, InventoryItemLocation.Backpack, state.X, state.Y, state.Rotation), out error);
        }
        public bool TryDrop(string id, out string error)
        {
            var state = records.FirstOrDefault(r => r.Id == id);
            if (state == null || !state.OccupiesGrid) return Fail("只能放下背包或手持物。", out error);
            return Replace(new InventoryItemState(id, InventoryItemLocation.World, rotation: state.Rotation), out error);
        }
        public bool TryInstall(string id, string slot, out string error)
        {
            var state = records.FirstOrDefault(r => r.Id == id);
            if (!InventoryItemDefinition.ValidId(slot) || state == null || !state.OccupiesGrid || Definition(id).Kind != InventoryItemKind.Battery ||
                records.Any(r => r.Location == InventoryItemLocation.Installed && r.Slot == slot)) return Fail("插口或电池状态无效。", out error);
            return Replace(new InventoryItemState(id, InventoryItemLocation.Installed, rotation: state.Rotation, slot: slot), out error);
        }
        public bool TryRecoverInstalled(string id, string slot, int x, int y, int rotation, out string error)
        {
            var state = records.FirstOrDefault(r => r.Id == id);
            if (state == null || state.Location != InventoryItemLocation.Installed || state.Slot != slot) return Fail("电池不在该插口。", out error);
            return Replace(new InventoryItemState(id, InventoryItemLocation.Backpack, x, y, rotation), out error);
        }
        public bool TryDeliver(IEnumerable<string> ids, out string error)
        {
            if (ids == null) return Fail("交付清单为空。", out error);
            var copy = ids.ToArray();
            if (copy.Length == 0 || copy.Distinct().Count() != copy.Length || copy.Any(id => !definitions.ContainsKey(id ?? "") || !Definition(id).Deliverable || !Get(id).OccupiesGrid))
                return Fail("交付清单包含无效、重复或非回收物。", out error);
            var set = new HashSet<string>(copy, StringComparer.Ordinal);
            return TryRestore(records.Select(r => set.Contains(r.Id) ? new InventoryItemState(r.Id, InventoryItemLocation.Delivered) : r), out error);
        }
        bool Replace(InventoryItemState state, out string error) => TryRestore(records.Select(r => r.Id == state.Id ? state : r), out error);
        public bool TryRestore(IEnumerable<InventoryItemState> snapshot, out string error)
        {
            if (snapshot == null) return Fail("库存快照为空。", out error);
            var copy = snapshot.ToArray();
            if (copy.Length != definitions.Count || copy.Any(r => r == null || r.Id == null || !definitions.ContainsKey(r.Id)) || copy.Select(r => r.Id).Distinct().Count() != copy.Length)
                return Fail("库存快照的物品ID不完整或重复。", out error);
            if (copy.Count(r => r.Location == InventoryItemLocation.Held) > 1) return Fail("只能有一件手持物。", out error);
            var occupied = new List<InventoryCell>(); var slots = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in copy)
            {
                if (!Enum.IsDefined(typeof(InventoryItemLocation), row.Location) || row.Rotation < 0 || row.Rotation > 3) return Fail("无效位置或旋转状态。", out error);
                var item = Definition(row.Id);
                if (row.OccupiesGrid)
                {
                    if (row.Slot != null || item.Kilograms > 8 || new InventoryBoard(occupied).CheckPlacement(item.Footprint, row.X, row.Y, row.Rotation) != InventoryPlacementStatus.Valid)
                        return Fail("背包快照越界、重叠或参数无效。", out error);
                    occupied.AddRange(item.Footprint.RotateClockwise(row.Rotation).Cells.Select(c => new InventoryCell(row.X + c.X, row.Y + c.Y)));
                }
                else
                {
                    if (row.X != -1 || row.Y != -1) return Fail("非背包物品不能占格。", out error);
                    if (row.Location == InventoryItemLocation.Installed)
                    {
                        if (item.Kind != InventoryItemKind.Battery || !InventoryItemDefinition.ValidId(row.Slot) || !slots.Add(row.Slot)) return Fail("无效或重复设备插口。", out error);
                    }
                    else if (row.Slot != null) return Fail("非设备物品不能占插口。", out error);
                    if (row.Location == InventoryItemLocation.Delivered && !item.Deliverable) return Fail("工具和电池不能作为回收物交付。", out error);
                }
            }
            if (Revision == long.MaxValue) return Fail("库存版本已达上限。", out error);
            records = Array.AsReadOnly(copy); Revision++; error = ""; return true;
        }
        static bool Fail(string message, out string error) { error = message; return false; }
    }
}
