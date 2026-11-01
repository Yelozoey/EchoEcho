using System;
using UnityEngine;
namespace EchoStation.SalvageNext
{
    public sealed class RoomVisual:MonoBehaviour
    {
        [SerializeField]int index;
        [SerializeField]Bounds region;
        public int Index=>index;
        public Bounds Region=>region;
        public void Configure(int value,float height=3){if(value<0||value>2||height<3||height>8||float.IsNaN(height))throw new ArgumentOutOfRangeException();index=value;region=new Bounds(new Vector3(6+12*value,height/2,12),new Vector3(12,height,24));}
    }
}
