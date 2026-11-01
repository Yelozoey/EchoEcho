using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Story;
namespace EchoStation.Puzzles
{
 public sealed class ProgressState
 {
  public string[] Completed=Array.Empty<string>(),Journal=Array.Empty<string>(),Scans=Array.Empty<string>(),Events=Array.Empty<string>();
  public string Zone="valley";
  // Physical thresholds beyond each blocking door, not the earlier descriptive zone boundary.
  public int AccessLevel;
  public bool CabinOpen,CoreOpen,VentOpen,Powered;
  public float VentElapsed;
 }
 public static class ProgressAudit
 {
  public const int ReservedEvents=12;
  public static readonly IReadOnlyList<string> DeviceIds=Array.AsReadOnly(new[]{"cabin_terminal_01","cabin_door_01","power_panel_01","vent_terminal_01","core_door_01","core_terminal_01"});
  public static string Validate(ProgressState s)
  {
   if(s==null)return "missing_state";
   if(!Unique(s.Completed,6)||!Unique(s.Journal,128)||!Unique(s.Scans,128)||!Unique(s.Events,512))return "invalid_ids";
   var progress=new PuzzleProgress();if(!progress.TryRestore(s.Completed))return "invalid_prerequisite";
   var journal=new HashSet<string>(s.Journal,StringComparer.Ordinal);var scans=new HashSet<string>(s.Scans,StringComparer.Ordinal);var events=new HashSet<string>(s.Events,StringComparer.Ordinal);
   var clues=new HashSet<string>(StationClues.All.Select(c=>c.Id),StringComparer.Ordinal);
   if(journal.Any(id=>!clues.Contains(id)&&!DeviceIds.Contains(id))||scans.Any(id=>!clues.Contains(id)&&!DeviceIds.Contains(id)))return "unknown_source";
   if(scans.Any(id=>!journal.Contains(id))||journal.Any(id=>DeviceIds.Contains(id)&&!scans.Contains(id)))return "scan_journal_mismatch";
   foreach(var c in StationClues.All)if(c.Kind==EchoStation.Memory.MemoryClueKind.Scan&&journal.Contains(c.Id)!=scans.Contains(c.Id))return "hidden_scan_mismatch";
   foreach(var p in StationScript.Puzzles){if(progress.Has(p.Id)!=events.Contains(p.CompletionId))return "progress_event_mismatch";if(progress.Has(p.Id)&&p.Sources.Any(id=>!journal.Contains(id)))return "missing_evidence";}
   if(progress.Has("pz04")!=(scans.Contains("scan_mark_01")&&scans.Contains("scan_lab_calibration_01")))return "scan_credential_mismatch";
   if(progress.Has("pz02")!=journal.Contains("terminal_data_01")||progress.Has("pz05")!=journal.Contains("terminal_vent_data_01"))return "terminal_receipt_mismatch";
   var cycle=new VentilationCycle();if(!cycle.TryRestore(s.Powered,s.VentElapsed)||s.Powered!=progress.Has("pz03")||s.VentOpen!=cycle.PassageReady||progress.Has("pz05")&&!cycle.PassageReady)return "ventilation_mismatch";
   if(s.CabinOpen&&!progress.Has("pz02")||s.CoreOpen&&!progress.Has("pz05"))return "unauthorized_door";
   if(s.AccessLevel<0||s.AccessLevel>3||s.AccessLevel>=1&&!s.CabinOpen||s.AccessLevel>=2&&!s.VentOpen||s.AccessLevel>=3&&!s.CoreOpen)return "inaccessible_position";
   if(!new[]{"valley","cabin","lab_a","vent","core"}.Contains(s.Zone)||!events.Contains("visited_"+s.Zone))return "zone_mismatch";
   return null;
  }
  private static bool Unique(string[] ids,int cap)=>ids!=null&&ids.Length<=cap&&ids.All(id=>!string.IsNullOrWhiteSpace(id))&&ids.Distinct(StringComparer.Ordinal).Count()==ids.Length;
 }
}
