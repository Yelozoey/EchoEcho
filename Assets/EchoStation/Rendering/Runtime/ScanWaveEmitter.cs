using UnityEngine;
namespace EchoStation.Rendering
{
    [DisallowMultipleComponent]
    public sealed class ScanWaveEmitter:MonoBehaviour
    {
        private ScanWaveState state;
        public ScanWaveState State=>state??(state=new ScanWaveState());
        private void Update()=>State.Tick(Time.deltaTime);
        private void OnDisable()=>State.Cancel();
    }
}
