using System.Linq;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Player;
using EchoStation.Puzzles;
using EchoStation.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace EchoStation.Station
{
 [DisallowMultipleComponent]
 public sealed class StationTerminalPanel:MonoBehaviour
 {
  private StationSession station;private PlayerInputReader input;private PlayerInteraction interaction;private Canvas canvas;private GameObject dialog;private Text heading,status;private StationDevice device;
  public bool IsOpen {get;private set;}public GameTextInput CodeInput {get;private set;}public Button SubmitButton {get;private set;}
  private RectTransform Rect(Transform parent,string name,float w,float h,float x,float y)=>RuntimeUi.Rect(name,parent,new Vector2(w,h),new Vector2(x,y),new Vector2(0,1),new Vector2(0,1));
  private void Awake()
  {
   station=GetComponent<StationSession>();input=GetComponent<PlayerInputReader>();interaction=GetComponent<PlayerInteraction>();canvas=RuntimeUi.CreateCanvas("ES_Terminal",gameObject.scene,135);var shade=RuntimeUi.Fill("Shade",canvas.transform,Vector2.zero);RuntimeUi.Image(shade,new Color(0,0,0,.8f));dialog=shade.gameObject;
   var panel=RuntimeUi.Rect("Panel",shade,new Vector2(960,500),Vector2.zero,new Vector2(.5f,.5f),new Vector2(.5f,.5f));RuntimeUi.Image(panel,new Color(.025f,.045f,.07f));heading=RuntimeUi.Label(Rect(panel,"Heading",860,50,40,-25),28);status=RuntimeUi.Label(Rect(panel,"Status",860,105,40,-100),20);
   var field=Rect(panel,"Code",520,65,40,-230);RuntimeUi.Image(field,new Color(.07f,.12f,.17f));field.gameObject.SetActive(false);var text=RuntimeUi.Label(RuntimeUi.Fill("Text",field,new Vector2(16,10)),26);text.verticalOverflow=VerticalWrapMode.Truncate;var placeholder=RuntimeUi.Label(RuntimeUi.Fill("Placeholder",field,new Vector2(16,10)),20);placeholder.text="输入4位维护码";CodeInput=field.gameObject.AddComponent<GameTextInput>();CodeInput.textComponent=text;CodeInput.placeholder=placeholder;CodeInput.targetGraphic=field.GetComponent<Image>();CodeInput.characterLimit=4;CodeInput.contentType=InputField.ContentType.IntegerNumber;CodeInput.lineType=InputField.LineType.SingleLine;CodeInput.SubmitRequested+=OnSubmit;CodeInput.CancelRequested+=Close;field.gameObject.SetActive(true);
   SubmitButton=RuntimeUi.Button(Rect(panel,"Submit",300,55,600,-230),"确认输入  [Enter]",()=>SubmitCode());RuntimeUi.Button(Rect(panel,"Close",300,55,40,-350),"返回探索  [Esc]",Close);RuntimeUi.Label(Rect(panel,"Help",510,75,380,-340),18).text="Echo不能代输。授权后仍需你另行E开门。\n成功读取的数据会归档，手动存档仍用F5。";dialog.SetActive(false);
  }
  private void OnEnable(){if(interaction)interaction.InformationRead+=OnDevice;}
  private void OnDisable(){if(interaction)interaction.InformationRead-=OnDevice;Close();}
  private void OnDevice(string _)
  {
   if(!station.Ready||!input.CanPlay)return;var target=interaction.Selection.Target;if(!target)return;var next=target.GetComponent<StationDevice>();if(!next)return;
   if(next.Kind==StationDeviceKind.CabinDoor||next.Kind==StationDeviceKind.CoreDoor){if(TerminalPuzzle.CanOpenDoor(station.Progress,target.Id)){target.GetComponent<StationGate>().Apply(true);station.SetStatus("通路已由你开启，可以沿原路返回。");}else station.SetStatus("通路尚未授权，请先检查对应维护终端。");return;}
   if(next.Kind!=StationDeviceKind.CabinTerminal&&next.Kind!=StationDeviceKind.VentTerminal)return;device=next;IsOpen=true;dialog.SetActive(true);input.SetUiBlocked(true);heading.text=target.DisplayName;CodeInput.text="";CodeInput.readOnly=false;SubmitButton.interactable=true;status.text="请输入本终端维护码并确认。操作可以取消。";CodeInput.Select();CodeInput.ActivateInputField();
  }
  private void OnSubmit()=>SubmitCode();
  public PuzzleResult SubmitCode()
  {
   if(!IsOpen||!input.HasApplicationFocus||CodeInput.IsComposing||!device)return PuzzleResult.Invalid;
   var cabin=device.Kind==StationDeviceKind.CabinTerminal;var puzzle=cabin?"pz02":"pz05";var dataId=cabin?"terminal_data_01":"terminal_vent_data_01";
   var source=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ClueSource>(true)).SingleOrDefault(c=>c.ClueId==dataId);
   JournalEntry prospective=null;if(source&&source.TryRead(out var canonical))prospective=JournalEntry.FromClue(canonical);
   var journalKnown=station.Journal.Store.TryGet(dataId,out var oldEntry);var memoryKnown=station.Memories.Store.TryGetClue(dataId,out var oldFact);
   var conflict=prospective==null||journalKnown&&!oldEntry.Matches(prospective)||memoryKnown&&(oldFact.Title!=prospective.Title||oldFact.Summary!=prospective.Summary||oldFact.Kind!=(MemoryClueKind)(int)prospective.Kind||oldFact.Password!=null||oldFact.KeyMarker!=null);
   if(!station.Progress.Has(puzzle)&&(!station.Knowledge.IsReady||!station.CanRecordPuzzle(puzzle)||conflict||!journalKnown&&station.Journal.Store.Entries.Count>=JournalStore.MaximumEntries||!memoryKnown&&station.Memories.Store.Clues.Count>=MemoryLimits.MaximumClues)){status.text="记录冲突、空间不足或终端数据不可用，尚未提交。";return PuzzleResult.Invalid;}
   var ledger=station.Echo.GetComponent<EchoScanController>().Scanner.Ledger;var credentials=ledger.HasDiscovered("scan_mark_01")&&ledger.HasDiscovered("scan_lab_calibration_01");
   var result=TerminalPuzzle.Submit(station.Progress,device.GetComponent<WorldTarget>().Id,CodeInput.text,credentials);
   if(result==PuzzleResult.Completed){source.gameObject.SetActive(true);if(!source.TryRead(out var record))throw new System.InvalidOperationException("Validated unlocked terminal source unavailable.");var archived=station.Journal.Store.Add(JournalEntry.FromClue(record));if(archived!=JournalAddResult.Added&&archived!=JournalAddResult.AlreadyKnown)throw new System.InvalidOperationException("Validated terminal archive rejected: "+archived);CodeInput.text="";CodeInput.readOnly=true;SubmitButton.interactable=false;status.text="授权已恢复，数据已归档。关闭面板后，请由你前往通路门按E开启。";station.SayLine(cabin?"cabin_unlocked":"vent_unlocked");}
   else if(result==PuzzleResult.AlreadyComplete){status.text="该终端已经授权，数据保持归档。";CodeInput.text="";}
   else{status.text=result==PuzzleResult.MissingCredential?"校准凭证不完整，需要两处真实扫描。":result==PuzzleResult.PrerequisiteMissing?"请先恢复通风，再继续维护访问。":"维护码未通过，请核对后重试。";CodeInput.text="";CodeInput.ActivateInputField();}
   return result;
  }
  public void Close(){if(!IsOpen)return;IsOpen=false;if(dialog)dialog.SetActive(false);if(CodeInput)CodeInput.text="";if(input)input.SetUiBlocked(false);device=null;}
  private void Update(){if(IsOpen&&(!input.HasApplicationFocus||Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame&&CodeInput.CanHandleModalEscape))Close();}
  private void OnDestroy(){if(CodeInput){CodeInput.SubmitRequested-=OnSubmit;CodeInput.CancelRequested-=Close;}if(canvas)Destroy(canvas.gameObject);}
 }
}
