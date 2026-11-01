using System;
using System.Collections.Generic;
using System.Linq;

namespace EchoStation.SalvageExpansion
{
    public enum InventoryPlacementStatus { Valid, OutOfBounds, Overlap }

    // Geometry snapshot only. Item ownership and committed moves belong to B02.
    public sealed class InventoryBoard
    {
        public const int Width = 6;
        public const int Height = 4;
        readonly uint occupied;
        public IReadOnlyList<InventoryCell> OccupiedCells { get; }

        public InventoryBoard() : this(Array.Empty<InventoryCell>()) { }

        public InventoryBoard(IEnumerable<InventoryCell> occupiedCells)
        {
            if (occupiedCells == null) throw new ArgumentNullException(nameof(occupiedCells));
            var copy = occupiedCells.ToArray();
            foreach (var cell in copy)
            {
                if (cell.X < 0 || cell.X >= Width || cell.Y < 0 || cell.Y >= Height)
                    throw new ArgumentException("Occupied cell is outside the 6x4 board.", nameof(occupiedCells));
                uint bit = Bit(cell.X, cell.Y);
                if ((occupied & bit) != 0)
                    throw new ArgumentException("Occupied cells must be distinct.", nameof(occupiedCells));
                occupied |= bit;
            }
            OccupiedCells = Array.AsReadOnly(copy.OrderBy(c => c.Y).ThenBy(c => c.X).ToArray());
        }

        public bool IsOccupied(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                throw new ArgumentOutOfRangeException(nameof(x), "Cell is outside the board.");
            return (occupied & Bit(x, y)) != 0;
        }

        public InventoryPlacementStatus CheckPlacement(InventoryFootprint footprint, int x, int y, int quarterTurns = 0)
        {
            if (footprint == null) throw new ArgumentNullException(nameof(footprint));
            var rotated = footprint.RotateClockwise(quarterTurns);
            if (x < 0 || y < 0 || (long)x + rotated.Width > Width || (long)y + rotated.Height > Height)
                return InventoryPlacementStatus.OutOfBounds;
            foreach (var cell in rotated.Cells)
                if ((occupied & Bit(x + cell.X, y + cell.Y)) != 0)
                    return InventoryPlacementStatus.Overlap;
            return InventoryPlacementStatus.Valid;
        }

        static uint Bit(int x, int y) => 1u << (y * Width + x);
    }
}
