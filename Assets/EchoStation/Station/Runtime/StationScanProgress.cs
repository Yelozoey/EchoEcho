using EchoStation.Echo;
using EchoStation.Puzzles;
using UnityEngine;
namespace EchoStation.Station
{
 [DisallowMultipleComponent]
 public sealed class StationScanProgress:MonoBehaviour
 {
  private StationSession station;private EchoScanLedger ledger;
  private void Awake()=>station=GetComponent<StationSession>();
  private void Update(){if(ledger==null&&station.Ready){ledger=station.Echo.GetComponent<EchoScanController>().Scanner.Ledger;ledger.Discovered+=OnDiscovery;Synchronize(false);}}
  private void OnDiscovery(EchoScanReceipt receipt)
  {
   if(receipt.TargetId!="scan_mark_01"&&receipt.TargetId!="scan_lab_calibration_01")return;
   if(station.Knowledge.TryGetKnownClue(receipt.TargetId,out _))station.SayLine(receipt.TargetId=="scan_mark_01"?"scan_cabin":ledger.HasDiscovered("scan_mark_01")?"scan_lab":"scan_lab_needs_cabin");Synchronize(true);
  }
  public PuzzleResult Synchronize(bool announce=false)
  {
   if(!station.Ready||ledger==null)return PuzzleResult.Invalid;
   if(!station.Knowledge.TryGetKnownClue("scan_mark_01",out _)||!station.Knowledge.TryGetKnownClue("scan_lab_calibration_01",out _))return PuzzleResult.MissingCredential;
   if(!station.Progress.Has("pz04")&&!station.CanRecordPuzzle("pz04")){station.SetStatus("扫描已归档，事件容量不足，校准确认未提交。");return PuzzleResult.Invalid;}
   var result=ScanPuzzle.Confirm(station.Progress,ledger.GetDiscoveredIds());if(result==PuzzleResult.Completed&&announce)station.SayLine("scan_credential_complete");return result;
  }
  private void OnDisable(){if(ledger!=null)ledger.Discovered-=OnDiscovery;ledger=null;}
 }
}
