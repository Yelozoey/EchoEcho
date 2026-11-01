using System;
namespace EchoStation.SalvageNext
{
    public readonly struct StreamPoint
    {
        public double X{get;}public double Y{get;}public double Z{get;}
        public StreamPoint(double x,double y,double z){if(!double.IsFinite(x)||!double.IsFinite(y)||!double.IsFinite(z))throw new ArgumentOutOfRangeException();X=x;Y=y;Z=z;}
    }
    public readonly struct StreamTaskLease
    {
        public ulong World{get;}public ulong Generation{get;}public StreamPoint Point{get;}
        public StreamTaskLease(ulong world,ulong generation,StreamPoint point){World=world;Generation=generation;Point=point;}
    }
    public readonly struct RoomLoadTicket
    {
        public int Room{get;}public ulong World{get;}public ulong Serial{get;}
        internal RoomLoadTicket(int room,ulong world,ulong serial){Room=room;World=world;Serial=serial;}
    }
    /// <summary>Pure demand policy, no Unity objects, loading or gameplay authority.</summary>
    public sealed class RoomDemandPlanner
    {
        readonly bool[] wanted=new bool[3];readonly ulong[] versions=new ulong[3];
        readonly double preloadSquared,unloadSquared;ulong world,serial;bool initialized;
        public double PreloadDistance{get;}public double UnloadDistance{get;}
        public bool UsedTaskLease{get;private set;}
        public RoomDemandPlanner(double preload=10,double unload=18){if(!double.IsFinite(preload)||!double.IsFinite(unload)||preload<=0||unload<=preload)throw new ArgumentOutOfRangeException();PreloadDistance=preload;UnloadDistance=unload;preloadSquared=preload*preload;unloadSquared=unload*unload;}
        static void Index(int room){if(room<0||room>2)throw new ArgumentOutOfRangeException(nameof(room));}
        static double Axis(double value,double minimum,double maximum)=>value<minimum?minimum-value:value>maximum?value-maximum:0;
        public static double DistanceSquared(int room,StreamPoint point){Index(room);double x=Axis(point.X,room*12,room*12+12),y=Axis(point.Y,0,3),z=Axis(point.Z,0,24);return x*x+y*y+z*z;}
        public bool Wanted(int room){Index(room);return initialized&&wanted[room];}
        public RoomLoadTicket Ticket(int room){Index(room);if(!initialized||!wanted[room])throw new InvalidOperationException("Unneeded room cannot receive a load ticket.");return new RoomLoadTicket(room,world,versions[room]);}
        public bool IsCurrent(RoomLoadTicket ticket)=>initialized&&ticket.Room>=0&&ticket.Room<3&&wanted[ticket.Room]&&ticket.World==world&&ticket.Serial==versions[ticket.Room];
        public bool Refresh(ulong worldEpoch,ulong taskGeneration,StreamPoint player,StreamPoint echo,StreamTaskLease? task,bool[] retained)
        {
            if(retained==null||retained.Length!=3)throw new ArgumentException("Three handle/resident flags required.");bool epochChanged=!initialized||world!=worldEpoch;bool changed=epochChanged;UsedTaskLease=task.HasValue&&task.Value.World==worldEpoch&&task.Value.Generation==taskGeneration;
            for(int i=0;i<3;i++){double distance=Math.Min(DistanceSquared(i,player),DistanceSquared(i,echo));if(UsedTaskLease)distance=Math.Min(distance,DistanceSquared(i,task.Value.Point));bool next=distance<=(retained[i]?unloadSquared:preloadSquared);if(epochChanged||next!=wanted[i]){versions[i]=++serial;changed=true;}wanted[i]=next;}world=worldEpoch;initialized=true;return changed;
        }
    }
}
