using System.Linq;
using NUnit.Framework;
namespace EchoStation.SalvageExpansion.Tests
{
    public sealed class InventoryDragTests
    {
        static InventoryStore Store()
        {
            var s = new InventoryStore(new[] { new InventoryItemDefinition("l", "L形", InventoryItemKind.Tool, new InventoryFootprint(new[] { new InventoryCell(0,0),new InventoryCell(0,1),new InventoryCell(1,1) }), 1), new InventoryItemDefinition("b", "盒", InventoryItemKind.Salvage, new InventoryFootprint(new[] { new InventoryCell(0,0) }), 1) });
            s.TryStow("l",0,0,0,out _);s.TryStow("b",3,0,0,out _);return s;
        }
        [Test] public void DragPreviewCancelAndInvalidCommitNeverMoveOriginal()
        {
            var s=Store();var drag=new InventoryDrag(s);long before=s.Revision;Assert.That(drag.Begin("l",0,1),Is.True);
            Assert.That(drag.Preview(4,3,out _),Is.True);drag.Cancel();Assert.That(s.Revision,Is.EqualTo(before));Assert.That(s.Get("l").X,Is.Zero);
            drag.Begin("l",0,0);Assert.That(drag.Commit(-1,0,out _),Is.False);Assert.That(drag.Active,Is.False);Assert.That(s.Revision,Is.EqualTo(before));
        }
        [Test] public void RotationKeepsGrabbedOccupiedCellUnderPointer()
        {
            var s=Store();var drag=new InventoryDrag(s);drag.Begin("l",0,1);drag.Rotate();
            Assert.That(drag.GrabX,Is.Zero);Assert.That(drag.GrabY,Is.Zero);Assert.That(drag.Commit(4,2,out _),Is.True);
            Assert.That(s.Get("l").X,Is.EqualTo(4));Assert.That(s.Get("l").Y,Is.EqualTo(2));Assert.That(s.Get("l").Rotation,Is.EqualTo(1));
        }
        [Test] public void StaleDragCannotOverwriteAnotherValidMove()
        {
            var s=Store();var drag=new InventoryDrag(s);drag.Begin("l",0,0);s.TryStow("b",5,3,0,out _);long version=s.Revision;
            Assert.That(drag.Commit(3,1,out _),Is.False);Assert.That(s.Get("b").X,Is.EqualTo(5));Assert.That(s.Revision,Is.EqualTo(version));Assert.That(s.Get("l").X,Is.Zero);
        }
        [Test] public void DragRejectsEmptyCornerAndWorldItems()
        {
            var s=Store();var drag=new InventoryDrag(s);Assert.That(drag.Begin("l",1,0),Is.False);s.TryDrop("b",out _);Assert.That(drag.Begin("b",0,0),Is.False);
        }
    }
}
