using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace EchoStation.SalvageExpansion.Tests
{
    public sealed class InventoryGeometryTests
    {
        static InventoryCell C(int x, int y) => new InventoryCell(x, y);
        static InventoryFootprint Shape(params InventoryCell[] cells) => new InventoryFootprint(cells);
        static InventoryFootprint L() => Shape(C(0, 0), C(0, 1), C(0, 2), C(1, 2));
        static InventoryFootprint Rect(int width, int height) => new InventoryFootprint(
            Enumerable.Range(0, height).SelectMany(y => Enumerable.Range(0, width).Select(x => C(x, y))));

        [Test] public void ShapeRejectsNullEmptyDuplicateAndNegativeCells()
        {
            Assert.Throws<ArgumentNullException>(() => new InventoryFootprint(null));
            Assert.Throws<ArgumentException>(() => Shape());
            Assert.Throws<ArgumentException>(() => Shape(C(0, 0), C(0, 0)));
            Assert.Throws<ArgumentException>(() => Shape(C(-1, 0)));
            Assert.Throws<ArgumentException>(() => Shape(C(0, -1)));
        }

        [Test] public void ShapeNormalizesAndDefensivelyCopiesInput()
        {
            var cells = new[] { C(4, 5), C(4, 6), C(5, 6) };
            var shape = Shape(cells);
            cells[0] = C(99, 99);
            Assert.That(shape.Width, Is.EqualTo(2));
            Assert.That(shape.Height, Is.EqualTo(2));
            Assert.That(shape.Cells, Is.EqualTo(new[] { C(0, 0), C(0, 1), C(1, 1) }));
            Assert.Throws<NotSupportedException>(() => ((IList<InventoryCell>)shape.Cells)[0] = C(9, 9));
        }

        [Test] public void ShapeRejectsUnrepresentableBoundsWithoutOverflow()
        {
            Assert.Throws<ArgumentException>(() => Shape(C(0, 0), C(int.MaxValue, 0)));
            Assert.Throws<ArgumentException>(() => Shape(C(0, 0), C(0, int.MaxValue)));
        }

        [Test] public void ClockwiseRotationMatchesScreenCoordinates()
        {
            var shape = L().RotateClockwise();
            Assert.That(shape.Width, Is.EqualTo(3));
            Assert.That(shape.Height, Is.EqualTo(2));
            Assert.That(shape.Cells, Is.EqualTo(new[] { C(0, 0), C(1, 0), C(2, 0), C(0, 1) }));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void RotationPreservesAreaAndFourTurnsRestoreShape(int turns)
        {
            var source = L();
            var rotated = source.RotateClockwise(turns);
            Assert.That(rotated.Area, Is.EqualTo(4));
            Assert.That(rotated.Cells.Distinct().Count(), Is.EqualTo(4));
            Assert.That(rotated.RotateClockwise(4 - turns).Cells, Is.EqualTo(source.Cells));
            Assert.That(source.Cells, Is.EqualTo(new[] { C(0, 0), C(0, 1), C(0, 2), C(1, 2) }));
        }

        [Test] public void RotationCountsWrapInBothDirectionsIncludingIntegerExtremes()
        {
            Assert.That(L().RotateClockwise(-1).Cells, Is.EqualTo(L().RotateClockwise(3).Cells));
            Assert.That(L().RotateClockwise(5).Cells, Is.EqualTo(L().RotateClockwise(1).Cells));
            Assert.That(L().RotateClockwise(int.MinValue).Cells, Is.EqualTo(L().Cells));
            Assert.That(L().RotateClockwise(int.MaxValue).Cells, Is.EqualTo(L().RotateClockwise(3).Cells));
        }

        [Test] public void FullBoardAndBottomRightEdgeFit()
        {
            var board = new InventoryBoard();
            Assert.That(board.CheckPlacement(Rect(6, 4), 0, 0), Is.EqualTo(InventoryPlacementStatus.Valid));
            Assert.That(board.CheckPlacement(Rect(2, 2), 4, 2), Is.EqualTo(InventoryPlacementStatus.Valid));
            Assert.That(board.CheckPlacement(Rect(1, 1), 5, 3), Is.EqualTo(InventoryPlacementStatus.Valid));
        }

        [TestCase(-1, 0)] [TestCase(0, -1)] [TestCase(6, 0)] [TestCase(0, 4)]
        [TestCase(int.MaxValue, 0)] [TestCase(0, int.MaxValue)] [TestCase(int.MinValue, 0)]
        public void InvalidAnchorIsRejectedWithoutWrapping(int x, int y)
        {
            Assert.That(new InventoryBoard().CheckPlacement(Rect(2, 2), x, y), Is.EqualTo(InventoryPlacementStatus.OutOfBounds));
        }

        [Test] public void RotationCanMakeTallItemFitWithoutChangingSource()
        {
            var shape = Rect(1, 6);
            var board = new InventoryBoard();
            Assert.That(board.CheckPlacement(shape, 0, 0), Is.EqualTo(InventoryPlacementStatus.OutOfBounds));
            Assert.That(board.CheckPlacement(shape, 0, 3, 1), Is.EqualTo(InventoryPlacementStatus.Valid));
            Assert.That(shape.Width, Is.EqualTo(1));
            Assert.That(shape.Height, Is.EqualTo(6));
        }

        [Test] public void OnlyOccupiedShapeCellsCollideAndConcavityCanBeUsed()
        {
            var board = new InventoryBoard(new[] { C(1, 0), C(1, 1) });
            Assert.That(board.CheckPlacement(L(), 0, 0), Is.EqualTo(InventoryPlacementStatus.Valid));
            Assert.That(board.CheckPlacement(Rect(2, 3), 0, 0), Is.EqualTo(InventoryPlacementStatus.Overlap));
            Assert.That(new InventoryBoard(new[] { C(0, 1) }).CheckPlacement(L(), 0, 0), Is.EqualTo(InventoryPlacementStatus.Overlap));
        }

        [Test] public void BoardCopiesInputAndQueriesNeverCommitChanges()
        {
            var cells = new List<InventoryCell> { C(5, 3) };
            var board = new InventoryBoard(cells);
            cells.Clear();
            Assert.That(board.IsOccupied(5, 3), Is.True);
            Assert.That(board.CheckPlacement(L(), 0, 0), Is.EqualTo(InventoryPlacementStatus.Valid));
            Assert.That(board.CheckPlacement(Rect(1, 1), 5, 3), Is.EqualTo(InventoryPlacementStatus.Overlap));
            Assert.That(board.CheckPlacement(L(), 6, 3), Is.EqualTo(InventoryPlacementStatus.OutOfBounds));
            Assert.That(board.OccupiedCells, Is.EqualTo(new[] { C(5, 3) }));
            Assert.That(board.IsOccupied(0, 0), Is.False);
            Assert.Throws<NotSupportedException>(() => ((IList<InventoryCell>)board.OccupiedCells).Clear());
        }

        [Test] public void BoardRejectsInvalidSnapshotsAndReadCoordinates()
        {
            Assert.Throws<ArgumentNullException>(() => new InventoryBoard(null));
            Assert.Throws<ArgumentException>(() => new InventoryBoard(new[] { C(0, 0), C(0, 0) }));
            foreach (var cell in new[] { C(-1, 0), C(6, 0), C(0, -1), C(0, 4), C(int.MaxValue, 0) })
                Assert.Throws<ArgumentException>(() => new InventoryBoard(new[] { cell }));
            var board = new InventoryBoard();
            Assert.Throws<ArgumentOutOfRangeException>(() => board.IsOccupied(6, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => board.IsOccupied(0, -1));
            Assert.Throws<ArgumentNullException>(() => board.CheckPlacement(null, 0, 0));
        }

        [Test] public void PlacementMatchesIndependentPerCellOracleAcrossLayoutsAndAnchors()
        {
            var random = new Random(3102026);
            var shapes = new[] { L(), Rect(1, 1), Rect(2, 3), Shape(C(1, 0), C(0, 1), C(1, 1), C(2, 1)) };
            for (int sample = 0; sample < 12; sample++)
            {
                var occupied = new HashSet<InventoryCell>();
                for (int y = 0; y < 4; y++) for (int x = 0; x < 6; x++)
                    if (random.Next(4) == 0) occupied.Add(C(x, y));
                var board = new InventoryBoard(occupied);
                foreach (var shape in shapes) for (int rotation = 0; rotation < 4; rotation++)
                for (int y = -1; y <= 4; y++) for (int x = -1; x <= 6; x++)
                {
                    var world = shape.RotateClockwise(rotation).Cells.Select(c => C(x + c.X, y + c.Y)).ToArray();
                    var expected = world.Any(c => c.X < 0 || c.X >= 6 || c.Y < 0 || c.Y >= 4)
                        ? InventoryPlacementStatus.OutOfBounds
                        : world.Any(occupied.Contains) ? InventoryPlacementStatus.Overlap : InventoryPlacementStatus.Valid;
                    Assert.That(board.CheckPlacement(shape, x, y, rotation), Is.EqualTo(expected),
                        "layout=" + sample + ", rotation=" + rotation + ", anchor=" + C(x, y));
                }
            }
        }
    }
}
