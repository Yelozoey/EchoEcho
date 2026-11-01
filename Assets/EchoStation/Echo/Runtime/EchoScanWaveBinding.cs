using EchoStation.Rendering;
using UnityEngine;

namespace EchoStation.Echo
{
    [RequireComponent(typeof(EchoScanController),typeof(ScanWaveEmitter)),DisallowMultipleComponent]
    public sealed class EchoScanWaveBinding:MonoBehaviour
    {
        private EchoScanEngine scanner;private ScanWaveEmitter emitter;private EchoRig rig;
        private void OnEnable()
        {
            rig=GetComponent<EchoRig>();emitter=GetComponent<ScanWaveEmitter>();scanner=GetComponent<EchoScanController>().Scanner;scanner.PhaseChanged+=Changed;if(scanner.IsBusy)Changed(ScanPhase.Aiming);
        }
        private void Changed(ScanPhase phase)
        {
            if(phase==ScanPhase.Aiming)emitter.State.Begin(rig.ScanOrigin.position);
            else if(phase==ScanPhase.Cancelled||phase==ScanPhase.Failed)emitter.State.Cancel();
        }
        private void OnDisable(){if(scanner!=null)scanner.PhaseChanged-=Changed;if(emitter)emitter.State.Cancel();}
    }
}
