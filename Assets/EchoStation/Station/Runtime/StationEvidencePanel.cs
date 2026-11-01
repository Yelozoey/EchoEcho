using System.Linq;
using EchoStation.Player;
using EchoStation.Puzzles;
using EchoStation.Story;
using UnityEngine;
using UnityEngine.UI;
namespace EchoStation.Station
{
 [DisallowMultipleComponent]
 public sealed class StationEvidencePanel:MonoBehaviour
 {
  private StationSession station;private JournalPanel journal;private Canvas canvas;private Text state;
  public Button TimelineButton {get;private set;}public Button CauseButton {get;private set;}
  private void Awake()
  {
   station=GetComponent<StationSession>();journal=GetComponent<JournalPanel>();canvas=RuntimeUi.CreateCanvas("ES_EvidenceActions",gameObject.scene,101);
   TimelineButton=RuntimeUi.Button(RuntimeUi.Rect("Timeline",canvas.transform,new Vector2(590,45),new Vector2(-310,55),new Vector2(.5f,0),new Vector2(.5f,0)),"核对入口时间线",()=>Confirm("pz01"));
   CauseButton=RuntimeUi.Button(RuntimeUi.Rect("Cause",canvas.transform,new Vector2(590,45),new Vector2(310,55),new Vector2(.5f,0),new Vector2(.5f,0)),"核对事故资料",()=>Confirm("pz06"));
   state=RuntimeUi.Label(RuntimeUi.Rect("EvidenceStatus",canvas.transform,new Vector2(1500,35),new Vector2(0,15),new Vector2(.5f,0),new Vector2(.5f,0)),17,TextAnchor.MiddleCenter);canvas.enabled=false;
  }
  private string[] Sources()=>station.Journal.Store.ExportSummaries().Select(s=>s.id).ToArray();
  public PuzzleResult Confirm(string id)
  {
   if(!station.Ready||!journal.IsOpen)return PuzzleResult.Invalid;
   if(!station.Progress.Has(id)&&!station.CanRecordPuzzle(id)){station.SetStatus("事件记忆容量已满，当前核对未提交。");return PuzzleResult.Invalid;}
   var result=EvidencePuzzle.Confirm(station.Progress,id,Sources());
   if(result==PuzzleResult.Completed)station.SayLine(id=="pz01"?"timeline_done":"cause_done");else if(result==PuzzleResult.AlreadyComplete)station.SayLine("already_done");else station.SayLine("need_evidence");Refresh();return result;
  }
  private void Update(){canvas.enabled=journal.IsOpen;if(journal.IsOpen)Refresh();}
  private void Refresh()
  {
   var sources=Sources();var timeline=StationScript.Puzzles[0].Sources.Count(s=>sources.Contains(s));var cause=StationScript.Puzzles[5].Sources.Count(s=>sources.Contains(s));
   TimelineButton.interactable=!station.Progress.Has("pz01")&&timeline==2;CauseButton.interactable=!station.Progress.Has("pz06")&&cause==3&&station.Progress.Has("pz05");
   state.text="入口来源 "+timeline+"/2"+(station.Progress.Has("pz01")?" · 已核对":"")+"    事故来源 "+cause+"/3"+(station.Progress.Has("pz06")?" · 已核对":"")+"    资料齐全后由你确认互证。";
  }
  private void OnDestroy(){if(canvas)Destroy(canvas.gameObject);}
 }
}
