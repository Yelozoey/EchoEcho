using System;
using System.Collections.Generic;
using System.Linq;

namespace EchoStation.SalvageExpansion
{
    public sealed class InventoryFootprint
    {
        public int Width { get; }
        public int Height { get; }
        public int Area => Cells.Count;
        public IReadOnlyList<InventoryCell> Cells { get; }

        public InventoryFootprint(IEnumerable<InventoryCell> cells)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            var copy = cells.ToArray();
            if (copy.Length == 0 || copy.Any(c => c.X < 0 || c.Y < 0) || copy.Distinct().Count() != copy.Length)
                throw new ArgumentException("Footprint needs distinct nonnegative occupied cells.", nameof(cells));

            int minX = copy.Min(c => c.X), minY = copy.Min(c => c.Y);
            long width = (long)copy.Max(c => c.X) - minX + 1;
            long height = (long)copy.Max(c => c.Y) - minY + 1;
            if (width > int.MaxValue || height > int.MaxValue)
                throw new ArgumentException("Footprint bounds exceed integer coordinates.", nameof(cells));
            Width = (int)width;
            Height = (int)height;
            Cells = Array.AsReadOnly(copy.Select(c => new InventoryCell(c.X - minX, c.Y - minY))
                .OrderBy(c => c.Y).ThenBy(c => c.X).ToArray());
        }

        public InventoryFootprint RotateClockwise(int quarterTurns = 1)
        {
            int turns = ((quarterTurns % 4) + 4) % 4;
            var result = this;
            for (int i = 0; i < turns; i++)
            {
                var previous = result;
                result = new InventoryFootprint(previous.Cells.Select(c =>
                    new InventoryCell(previous.Height - 1 - c.Y, c.X)));
            }
            return result;
        }
    }
}
