using System;
using EchoStation.World;
using UnityEngine;
using UnityEngine.AI;
namespace EchoStation.Station
{
    public enum StationDeviceKind { CabinTerminal, CabinDoor, Power, VentTerminal, CoreDoor, CoreTerminal }
    [RequireComponent(typeof(WorldTarget)),DisallowMultipleComponent]
    public sealed class StationDevice:MonoBehaviour
    {
        [SerializeField]private StationDeviceKind kind;
        public StationDeviceKind Kind=>kind;
        public void Configure(StationDeviceKind value){if(Application.isPlaying||((uint)value>(uint)StationDeviceKind.CoreTerminal))throw new InvalidOperationException("Configure device in owned editor scene only.");kind=value;}
    }
}
