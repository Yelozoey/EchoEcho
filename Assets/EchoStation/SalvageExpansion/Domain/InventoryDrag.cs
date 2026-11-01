using System;
using System.Linq;

namespace EchoStation.SalvageExpansion
{
    public sealed class InventoryDrag
    {
        readonly InventoryStore store;
        long revision;
        public string Id { get; private set; }
        public int Rotation { get; private set; }
        public int GrabX { get; private set; }
        public int GrabY { get; private set; }
        public bool Active => Id != null;
        public bool Stale => Active && store.Revision != revision;
        public InventoryDrag(InventoryStore store) { this.store = store ?? throw new ArgumentNullException(nameof(store)); }
        public bool Begin(string id, int grabX, int grabY)
        {
            var row = store.Records.FirstOrDefault(r => r.Id == id);
            if (row == null || !row.OccupiesGrid || !store.Definition(id).Footprint.RotateClockwise(row.Rotation).Cells.Contains(new InventoryCell(grabX, grabY))) return false;
            Id = id; Rotation = row.Rotation; GrabX = grabX; GrabY = grabY; revision = store.Revision; return true;
        }
        public void Rotate()
        {
            if (!Active) return;
            int height = store.Definition(Id).Footprint.RotateClockwise(Rotation).Height;
            int x = height - 1 - GrabY; GrabY = GrabX; GrabX = x; Rotation = (Rotation + 1) % 4;
        }
        public bool Preview(int hoveredX, int hoveredY, out string error)
        {
            if (!Active || Stale) { error = "物品状态已变化，请重试。"; return false; }
            return store.CanStow(Id, hoveredX - GrabX, hoveredY - GrabY, Rotation, out error);
        }
        public bool Commit(int hoveredX, int hoveredY, out string error)
        {
            if (!Preview(hoveredX, hoveredY, out error)) { Cancel(); return false; }
            string id = Id; int x = hoveredX - GrabX, y = hoveredY - GrabY, rotation = Rotation;
            Cancel(); return store.TryStow(id, x, y, rotation, out error);
        }
        public void Cancel() { Id = null; Rotation = GrabX = GrabY = 0; }
    }
}
