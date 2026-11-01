using System;
using System.Linq;
using EchoStation.Story;
namespace EchoStation.Puzzles
{
 public enum EndingResult{Pending,Confirmed,Locked,Unknown,NoSelection,AlreadyEnded,Cancelled}
 public sealed class EndingChoice
 {
  public string PendingId{get;private set;}
  public string EndingId{get;private set;}
  public string[] Missing(PuzzleProgress progress,string id)
  {var spec=StationScript.Endings.FirstOrDefault(e=>e.Id==id);return spec==null||progress==null?new[]{"unknown"}:spec.Required.Where(p=>!progress.Has(p)).ToArray();}
  public EndingResult Select(PuzzleProgress progress,string id)
  {if(EndingId!=null)return EndingResult.AlreadyEnded;if(!StationScript.Endings.Any(e=>e.Id==id))return EndingResult.Unknown;if(Missing(progress,id).Length>0)return EndingResult.Locked;PendingId=id;return EndingResult.Pending;}
  public EndingResult Confirm(PuzzleProgress progress)
  {if(EndingId!=null)return EndingResult.AlreadyEnded;if(PendingId==null)return EndingResult.NoSelection;if(Missing(progress,PendingId).Length>0)return EndingResult.Locked;EndingId=PendingId;PendingId=null;return EndingResult.Confirmed;}
  public EndingResult Cancel(){if(EndingId!=null)return EndingResult.AlreadyEnded;PendingId=null;return EndingResult.Cancelled;}
  public void Reset(){PendingId=EndingId=null;}
 }
}
