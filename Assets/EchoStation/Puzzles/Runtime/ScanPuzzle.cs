using System;
using System.Collections.Generic;
namespace EchoStation.Puzzles
{
 public static class ScanPuzzle
 {
  public static PuzzleResult Confirm(PuzzleProgress progress,IEnumerable<string> verifiedScanIds)
  {
   if(progress==null||verifiedScanIds==null)return PuzzleResult.Invalid;if(progress.Has("pz04"))return PuzzleResult.AlreadyComplete;var ids=new HashSet<string>(verifiedScanIds,StringComparer.Ordinal);
   if(!ids.Contains("scan_mark_01")||!ids.Contains("scan_lab_calibration_01"))return PuzzleResult.MissingCredential;return progress.Mark("pz04");
  }
 }
}
