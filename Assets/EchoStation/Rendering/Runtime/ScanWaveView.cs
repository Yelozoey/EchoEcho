using UnityEngine;
namespace EchoStation.Rendering
{
    [RequireComponent(typeof(Camera)),DisallowMultipleComponent]
    public sealed class ScanWaveView:MonoBehaviour
    {
        [SerializeField]private ScanWaveEmitter source;
        public ScanWaveEmitter Source{get=>source;set=>source=value;}
    }
}
