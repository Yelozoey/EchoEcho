using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoStation.Station
{
    public sealed class StationZone
    {
        public string Id { get; }
        public string Name { get; }
        public float StartZ { get; }
        public float EndZ { get; }
        public Vector3 Center { get; }
        public Vector3 Size { get; }
        internal StationZone(string id,string name,float start,float end,float width,float y)
        {Id=id;Name=name;StartZ=start;EndZ=end;Center=new Vector3(0,y,(start+end)*.5f);Size=new Vector3(width,.2f,end-start);}
    }
    public static class StationLayout
    {
        public const string ScenePath="Assets/EchoStation/Station/Scenes/ES_Station_Greybox.unity";
        public const string DataFolder="Assets/EchoStation/Station/Generated";
        public static Vector3 PlayerSpawn=>new Vector3(0,.05f,-35);
        public static Vector3 EchoSpawn=>new Vector3(1.5f,0,-34);
        public static IReadOnlyList<StationZone> Zones {get;}=Array.AsReadOnly(new[]{
            new StationZone("valley","入口山谷",-40,-7,16,0),new StationZone("cabin","地面观测小屋",-7,4,10,0),
            new StationZone("lab_a","地下A实验室",4,38,12,-4),new StationZone("vent","通风通道",38,58,5,-4),new StationZone("core","核心控制室",58,76,14,-4)});
        public static string ZoneAt(Vector3 point)
        {for(var i=0;i<Zones.Count;i++){var z=Zones[i];if(point.z>=z.StartZ&&point.z<z.EndZ)return z.Id;}return null;}
    }
}
