using System;
using NUnit.Framework;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextRoomDemandTests
    {
        static StreamPoint Point(double x,double y=1,double z=12)=>new StreamPoint(x,y,z);
        [Test]public void BoundsDistancesAndExactPreloadBoundaryUseThreeDimensions()
        {Assert.That(RoomDemandPlanner.DistanceSquared(1,Point(2)),Is.EqualTo(100));Assert.That(RoomDemandPlanner.DistanceSquared(1,Point(12)),Is.Zero);Assert.That(RoomDemandPlanner.DistanceSquared(0,Point(6,6,28)),Is.EqualTo(25));var p=new RoomDemandPlanner();var flags=new bool[3];p.Refresh(1,0,Point(2),Point(2),null,flags);Assert.That(p.Wanted(0)&&p.Wanted(1)&&!p.Wanted(2),Is.True);p.Refresh(1,0,Point(1.99),Point(1.99),null,flags);Assert.That(p.Wanted(1),Is.False);}
        [Test]public void ResidentOrPendingHandlesUseEighteenMetreHysteresis()
        {var p=new RoomDemandPlanner();var flags=new[]{false,true,false};p.Refresh(1,0,Point(-6),Point(-6),null,flags);Assert.That(p.Wanted(1),Is.True);p.Refresh(1,0,Point(-6.01),Point(-6.01),null,flags);Assert.That(p.Wanted(1),Is.False);p.Refresh(1,0,Point(-1),Point(-1),null,new bool[3]);Assert.That(p.Wanted(1),Is.False);p.Refresh(1,0,Point(-1),Point(-1),null,flags);Assert.That(p.Wanted(1),Is.True);}
        [Test]public void PlayerEchoAndOnlyCurrentValidatedTaskContributeUnion()
        {var p=new RoomDemandPlanner();var flags=new bool[3];p.Refresh(5,9,Point(0),Point(0),new StreamTaskLease(5,9,Point(35)),flags);Assert.That(p.UsedTaskLease,Is.True);Assert.That(p.Wanted(0)&&p.Wanted(2),Is.True);p.Refresh(5,10,Point(0),Point(0),new StreamTaskLease(5,9,Point(35)),flags);Assert.That(p.UsedTaskLease,Is.False);Assert.That(p.Wanted(2),Is.False);p.Refresh(6,10,Point(0),Point(0),new StreamTaskLease(5,10,Point(35)),flags);Assert.That(p.Wanted(2),Is.False);p.Refresh(6,10,Point(0),Point(35),null,flags);Assert.That(p.Wanted(2),Is.True);}
        [Test]public void RetiredThenReacquiredAndRestoredWorldRejectOldLoadTickets()
        {var p=new RoomDemandPlanner();var flags=new bool[3];p.Refresh(1,0,Point(0),Point(0),null,flags);var first=p.Ticket(0);Assert.That(p.IsCurrent(first),Is.True);p.Refresh(1,0,Point(100),Point(100),null,flags);Assert.That(p.IsCurrent(first),Is.False);p.Refresh(1,0,Point(0),Point(0),null,flags);var second=p.Ticket(0);Assert.That(second.Serial,Is.Not.EqualTo(first.Serial));Assert.That(p.IsCurrent(first),Is.False);p.Refresh(2,0,Point(0),Point(0),null,flags);Assert.That(p.IsCurrent(second),Is.False);Assert.That(p.IsCurrent(default),Is.False);}
        [Test]public void UnchangedDemandDoesNotInvalidateTicketWhenNewTaskStillNeedsSameRoom()
        {var p=new RoomDemandPlanner();var flags=new bool[3];p.Refresh(1,1,Point(0),Point(0),null,flags);var ticket=p.Ticket(0);Assert.That(p.Refresh(1,2,Point(.1),Point(.2),null,flags),Is.False);Assert.That(p.IsCurrent(ticket),Is.True);Assert.Throws<InvalidOperationException>(()=>p.Ticket(2));Assert.Throws<ArgumentOutOfRangeException>(()=>p.Wanted(3));}
        [Test]public void InvalidInputsAreRejectedAndThousandSteadyRefreshesAllocateZeroBytes()
        {Assert.Throws<ArgumentOutOfRangeException>(()=>new StreamPoint(double.NaN,0,0));Assert.Throws<ArgumentOutOfRangeException>(()=>new RoomDemandPlanner(18,10));Assert.Throws<ArgumentOutOfRangeException>(()=>new RoomDemandPlanner(double.PositiveInfinity,20));var p=new RoomDemandPlanner();var flags=new bool[3];var point=Point(0);Assert.Throws<ArgumentException>(()=>p.Refresh(1,0,point,point,null,new bool[2]));p.Refresh(1,0,point,point,null,flags);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)p.Refresh(1,0,point,point,null,flags);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);}
    }
}
