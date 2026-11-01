using System.Text.RegularExpressions;
using EchoStation.Story;
namespace EchoStation.Puzzles
{
 public static class TerminalPuzzle
 {
  private static readonly Regex Code=new Regex("\\A[0-9]{4}\\z",RegexOptions.CultureInvariant);
  public static PuzzleResult Submit(PuzzleProgress progress,string terminal,string input,bool verifiedScans=false)
  {
   if(progress==null||terminal!="cabin_terminal_01"&&terminal!="vent_terminal_01")return PuzzleResult.Invalid;
   var cabin=terminal=="cabin_terminal_01";var id=cabin?"pz02":"pz05";if(progress.Has(id))return PuzzleResult.AlreadyComplete;
   if(!cabin&&(!progress.Has("pz04")||!verifiedScans))return PuzzleResult.MissingCredential;
   if(!cabin&&!progress.Has("pz03"))return PuzzleResult.PrerequisiteMissing;
   if(input==null||!Code.IsMatch(input))return PuzzleResult.WrongCode;
   StationClues.TryGet(cabin?"note_maintenance_01":"scan_lab_calibration_01",out var source);if(input!=source.Password)return PuzzleResult.WrongCode;
   return progress.Mark(id);
  }
  public static bool CanOpenDoor(PuzzleProgress progress,string door)=>progress!=null&&(door=="cabin_door_01"&&progress.Has("pz02")||door=="core_door_01"&&progress.Has("pz05"));
 }
}
