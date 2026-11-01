using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Dialogue.Integration;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Player;
using EchoStation.Puzzles;
using EchoStation.Story;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace EchoStation.Station
{
 [DisallowMultipleComponent]
 public sealed class StationEndingPanel:MonoBehaviour
 {
  private StationSession station;private PlayerInputReader input;private PlayerInteraction interaction;private Canvas canvas;private GameObject dialog;private Text title,detail;private readonly Button[] options=new Button[3];private Button cancel;private string[] archived=Array.Empty<string>();
  public EndingChoice Choice {get;}=new EndingChoice();public bool IsOpen{get;private set;}public bool HasEnded=>Choice.EndingId!=null;
  public Button ConfirmButton{get;private set;}public IReadOnlyList<string> ArchivedEvidenceIds=>Array.AsReadOnly(archived);
  public Button ChoiceButton(int index)=>index>=0&&index<3?options[index]:null;
  public event Action TitleRequested;
  private RectTransform Rect(Transform p,string name,float w,float h,float x,float y)=>RuntimeUi.Rect(name,p,new Vector2(w,h),new Vector2(x,y),new Vector2(0,1),new Vector2(0,1));
  private void Awake()
  {
   station=GetComponent<StationSession>();input=GetComponent<PlayerInputReader>();interaction=GetComponent<PlayerInteraction>();canvas=RuntimeUi.CreateCanvas("ES_Ending",gameObject.scene,145);var shade=RuntimeUi.Fill("Shade",canvas.transform,Vector2.zero);RuntimeUi.Image(shade,new Color(0,0,0,.9f));dialog=shade.gameObject;var p=RuntimeUi.Rect("Panel",shade,new Vector2(1260,900),Vector2.zero,new Vector2(.5f,.5f),new Vector2(.5f,.5f));RuntimeUi.Image(p,new Color(.025f,.045f,.07f));title=RuntimeUi.Label(Rect(p,"Title",1160,65,50,-25),30);detail=RuntimeUi.Label(Rect(p,"Detail",1160,145,50,-110),21);
   for(var i=0;i<3;i++){var ending=StationScript.Endings[i];options[i]=RuntimeUi.Button(Rect(p,"Choice_"+ending.Id,1160,110,50,-285-i*125),"",()=>Select(ending.Id),21);}
   ConfirmButton=RuntimeUi.Button(Rect(p,"Confirm",550,65,50,-685),"确认当前处置并结束复查",()=>Confirm(),22);cancel=RuntimeUi.Button(Rect(p,"Cancel",550,65,660,-685),"取消，返回补查  [Esc]",Close,22);
   RuntimeUi.Button(Rect(p,"TitleButton",340,60,50,-795),"返回标题 / 新局",()=>TitleRequested?.Invoke());RuntimeUi.Button(Rect(p,"Load",340,60,460,-795),"读取选择前的手动档",()=>GetComponent<StationControls>().RequestLoad());RuntimeUi.Label(Rect(p,"SaveNotice",340,70,870,-785),17).text="结局不会自动覆盖手动档。\n完整归档仅为游戏内本地状态。";dialog.SetActive(false);
  }
  private void OnEnable(){if(interaction)interaction.InformationRead+=OnDevice;}
  private void OnDisable(){if(interaction)interaction.InformationRead-=OnDevice;if(!HasEnded)Close();}
  private void OnDevice(string _){if(!station.Ready||!input.CanPlay||HasEnded)return;var target=interaction.Selection.Target;var device=target?target.GetComponent<StationDevice>():null;if(!device||device.Kind!=StationDeviceKind.CoreTerminal)return;IsOpen=true;dialog.SetActive(true);input.SetUiBlocked(true);Choice.Cancel();Refresh();station.SayLine("ending_confirm");}
  private void Refresh()
  {
   title.text=HasEnded?StationScript.Endings.Single(e=>e.Id==Choice.EndingId).Title:"核心处置终端";
   for(var i=0;i<3;i++){var spec=StationScript.Endings[i];var missing=Choice.Missing(station.Progress,spec.Id);options[i].interactable=!HasEnded&&missing.Length==0;var condition=missing.Length==0?"条件已满足":("尚缺："+string.Join("、",missing.Select(id=>StationScript.Puzzles.Single(p=>p.Id==id).Title)));options[i].GetComponentInChildren<Text>().text=spec.Title+" — "+condition+"\n"+spec.Consequence;}
   ConfirmButton.interactable=!HasEnded&&Choice.PendingId!=null;cancel.interactable=!HasEnded;
   if(!HasEnded)detail.text=Choice.PendingId==null?"请选择处置方式。条件不足时可返回日志补查；选择后还需要确认。":StationScript.Endings.Single(e=>e.Id==Choice.PendingId).Consequence+"\n确认后结束本次复查；不会自动保存。";
  }
  public EndingResult Select(string id){if(!IsOpen||!input.HasApplicationFocus)return EndingResult.NoSelection;var result=Choice.Select(station.Progress,id);Refresh();return result;}
  public EndingResult Confirm()
  {
   if(HasEnded)return EndingResult.AlreadyEnded;if(!IsOpen||!input.HasApplicationFocus||Choice.PendingId==null)return EndingResult.NoSelection;
   var spec=StationScript.Endings.Single(e=>e.Id==Choice.PendingId);if(Choice.Missing(station.Progress,spec.Id).Length>0)return EndingResult.Locked;
   if(station.Memories.Store.Events.Count>=MemoryLimits.MaximumEvents){detail.text="事件记忆空间不足，尚未确认处置。";return EndingResult.Locked;}
   if(!GetComponent<ScriptDialogueController>().SubmitPlayerIntent(spec.EchoStays?ScriptCommands.Wait():ScriptCommands.Follow())){detail.text="Echo安排尚未确认，请重试。";return EndingResult.Locked;}
   var result=Choice.Confirm(station.Progress);if(result!=EndingResult.Confirmed)return result;
   if(station.Memories.Store.AddEvent("ending_"+spec.Id,station.Elapsed,"玩家选择"+spec.Title+"。",MemoryPriority.Critical)!=MemoryWriteResult.Added)throw new InvalidOperationException("Validated ending event rejected.");
   archived=spec.Id=="archive_exit"?station.Journal.Store.Entries.Select(e=>e.Id).ToArray():Array.Empty<string>();Refresh();detail.text=spec.Consequence+"\n已保留"+station.Journal.Store.Entries.Count+"条调查记录。"+(archived.Length>0?"完整证据已记录至游戏内随身存储。":"")+"\n可返回标题、开始新局或读取选择前的手动档。";station.SetStatus("本次复查结束："+spec.Title);return result;
  }
  public void Close(){if(!IsOpen||HasEnded)return;Choice.Cancel();IsOpen=false;dialog.SetActive(false);input.SetUiBlocked(false);station.SayLine("ending_cancel");}
  public void ResetForRestore(){Choice.Reset();archived=Array.Empty<string>();IsOpen=false;if(dialog)dialog.SetActive(false);if(input)input.SetUiBlocked(false);}
  internal void HideForTitle(){if(!HasEnded)Choice.Cancel();IsOpen=false;if(dialog)dialog.SetActive(false);}
  internal void RestoreOutcome(string id,string[] evidence,bool visible){if(id==null)return;Choice.Select(station.Progress,id);Choice.Confirm(station.Progress);archived=(string[])evidence.Clone();IsOpen=visible;dialog.SetActive(visible);Refresh();var spec=StationScript.Endings.Single(e=>e.Id==id);detail.text=spec.Consequence+"\n复查已结束；可返回标题或读取手动档。";}
  private void Update(){if(IsOpen&&!HasEnded&&(!input.HasApplicationFocus||Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame))Close();}
  private void OnDestroy(){if(canvas)Destroy(canvas.gameObject);}
 }
}
