using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Story;
namespace EchoStation.Puzzles
{
 public enum PuzzleResult { Completed, AlreadyComplete, MissingClues, PrerequisiteMissing, Invalid, WrongCode, MissingCredential, WrongCircuit }
 public sealed class PuzzleProgress
 {
  private HashSet<string> completed=new HashSet<string>(StringComparer.Ordinal);
  public event Action<string> Completed;
  public bool Has(string id)=>id!=null&&completed.Contains(id);
  public string[] Capture()=>completed.OrderBy(s=>s,StringComparer.Ordinal).ToArray();
  public bool TryRestore(IEnumerable<string> ids)
  {
   if(ids==null)return false;var all=ids.Take(7).ToArray();if(all.Length>6||all.Any(s=>!StationScript.Puzzles.Any(p=>p.Id==s))||all.Distinct(StringComparer.Ordinal).Count()!=all.Length)return false;
   var candidate=new HashSet<string>(all,StringComparer.Ordinal);if(StationScript.Puzzles.Any(p=>candidate.Contains(p.Id)&&p.Previous.Any(d=>!candidate.Contains(d))))return false;completed=candidate;return true;
  }
  internal PuzzleResult Mark(string id)
  {
   var spec=StationScript.Puzzles.FirstOrDefault(p=>p.Id==id);if(spec==null)return PuzzleResult.Invalid;if(Has(id))return PuzzleResult.AlreadyComplete;if(spec.Previous.Any(d=>!Has(d)))return PuzzleResult.PrerequisiteMissing;
   completed.Add(id);Completed?.Invoke(id);return PuzzleResult.Completed;
  }
 }
 public static class EvidencePuzzle
 {
  public static PuzzleResult Confirm(PuzzleProgress progress,string puzzleId,IEnumerable<string> knownSources)
  {
   if(progress==null||knownSources==null||puzzleId!="pz01"&&puzzleId!="pz06")return PuzzleResult.Invalid;if(progress.Has(puzzleId))return PuzzleResult.AlreadyComplete;
   var spec=StationScript.Puzzles.First(p=>p.Id==puzzleId);var ids=new HashSet<string>(knownSources,StringComparer.Ordinal);if(spec.Sources.Any(id=>!ids.Contains(id)))return PuzzleResult.MissingClues;return progress.Mark(puzzleId);
  }
 }
}
