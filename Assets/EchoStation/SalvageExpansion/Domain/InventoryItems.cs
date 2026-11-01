using System;

namespace EchoStation.SalvageExpansion
{
    public enum InventoryItemKind { Tool, Battery, Core, Salvage }
    public enum InventoryItemLocation { World, Backpack, Held, Installed, Delivered }

    public sealed class InventoryItemDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public InventoryItemKind Kind { get; }
        public InventoryFootprint Footprint { get; }
        public float Kilograms { get; }
        public int Value { get; }
        public bool Deliverable => Kind == InventoryItemKind.Core || Kind == InventoryItemKind.Salvage;

        public InventoryItemDefinition(string id, string name, InventoryItemKind kind, InventoryFootprint footprint, float kilograms, int value = 0)
        {
            if (!ValidId(id) || string.IsNullOrWhiteSpace(name) || !Enum.IsDefined(typeof(InventoryItemKind), kind) ||
                footprint == null || !float.IsFinite(kilograms) || kilograms <= 0 || value < 0)
                throw new ArgumentException("Invalid inventory item definition.");
            Id = id; Name = name; Kind = kind; Footprint = footprint; Kilograms = kilograms; Value = value;
        }
        public static bool ValidId(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length > 64) return false;
            foreach (char c in value) if (!(c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || c == '_')) return false;
            return true;
        }
    }

    public sealed class InventoryItemState
    {
        public string Id { get; }
        public InventoryItemLocation Location { get; }
        public int X { get; }
        public int Y { get; }
        public int Rotation { get; }
        public string Slot { get; }
        public InventoryItemState(string id, InventoryItemLocation location, int x = -1, int y = -1, int rotation = 0, string slot = null)
        { Id = id; Location = location; X = x; Y = y; Rotation = rotation; Slot = slot; }
        public bool OccupiesGrid => Location == InventoryItemLocation.Backpack || Location == InventoryItemLocation.Held;
    }
}
