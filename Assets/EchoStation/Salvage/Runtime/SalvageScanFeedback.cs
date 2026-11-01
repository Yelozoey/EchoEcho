using EchoStation.Echo;
using EchoStation.Rendering;
using UnityEngine;
namespace EchoStation.Salvage
{
    [DefaultExecutionOrder(80),RequireComponent(typeof(SalvageScanner),typeof(ScanWaveEmitter))]
    public sealed class SalvageScanFeedback:MonoBehaviour
    {
        SalvageScanner scanner;ScanWaveEmitter wave;EchoRig rig;
        void Start(){scanner=GetComponent<SalvageScanner>();wave=GetComponent<ScanWaveEmitter>();rig=GetComponent<EchoRig>();scanner.PhaseChanged+=Changed;}
        void Changed(ScanPhase phase){if(phase==ScanPhase.Aiming)wave.State.Begin(rig.ScanOrigin.position);else if(phase==ScanPhase.Cancelled||phase==ScanPhase.Failed)wave.State.Cancel();}
        void OnDisable(){if(scanner)scanner.PhaseChanged-=Changed;if(wave)wave.State.Cancel();}
    }
}
