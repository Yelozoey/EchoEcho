using EchoStation.World;

namespace EchoStation.Echo
{
    // Only the persistence adapter assembly is a friend. No intent/LLM API exposes this bridge.
    internal static class EchoPersistence
    {
        internal static void Restore(EchoScanController controller,WorldTarget[] all,WorldTarget[] scanned)
        {
            controller.Scanner.ResetForPersistence();var wave=controller.GetComponent<EchoStation.Rendering.ScanWaveEmitter>();if(wave)wave.State.Cancel();controller.Scanner.Ledger.ClearForPersistence();foreach(var target in all){var clue=target.GetComponent<ClueSource>();if(clue&&clue.Kind==ClueKind.Scan)clue.ResetForPersistence();}foreach(var target in scanned)if(!controller.Scanner.Ledger.RestoreCommit(target))throw new System.InvalidOperationException("Previously validated persistence target could not restore: "+target.Id);
        }
        internal static bool TryReadCanonical(WorldTarget target,bool scanned,out ClueRecord record,bool allowInactive=false)
        {
            record=null;var clue=target?target.GetComponent<ClueSource>():null;if(!clue||!allowInactive&&!clue.isActiveAndEnabled||(clue.Kind==ClueKind.Scan&&!scanned))return false;record=clue.ReadCanonicalForPersistence();return record!=null&&record.Id==target.Id;
        }
    }
}
