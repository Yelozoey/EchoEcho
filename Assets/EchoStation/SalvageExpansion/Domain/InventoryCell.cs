using System;

namespace EchoStation.SalvageExpansion
{
    public readonly struct InventoryCell : IEquatable<InventoryCell>
    {
        public int X { get; }
        public int Y { get; }

        public InventoryCell(int x, int y) { X = x; Y = y; }
        public bool Equals(InventoryCell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is InventoryCell other && Equals(other);
        public override int GetHashCode() => unchecked((X * 397) ^ Y);
        public override string ToString() => "(" + X + "," + Y + ")";
    }
}
