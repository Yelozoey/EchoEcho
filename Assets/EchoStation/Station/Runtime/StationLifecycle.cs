using System.Collections;
using EchoStation.Memory;
using EchoStation.Persistence;
using EchoStation.Player;
using EchoStation.Puzzles;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace EchoStation.Station
{
 [DisallowMultipleComponent]
 public sealed class StationLifecycle:MonoBehaviour
 {
  private StationSession station;private StationPersistence persistence;private StationControls controls;private StationEndingPanel ending;private PlayerInputReader input;private Canvas canvas;private GameObject dialog;private Text detail;private bool confirming;
  public bool IsOpen{get;private set;}public bool IsChangingSession{get;private set;}
  public Canvas Canvas=>canvas;
  public Button ContinueButton{get;private set;}public Button NewButton{get;private set;}public Button ConfirmNewButton{get;private set;}public Button CancelNewButton{get;private set;}public Button LoadButton{get;private set;}
  private RectTransform Rect(Transform p,string name,float x,float y)=>RuntimeUi.Rect(name,p,new Vector2(520,65),new Vector2(x,y),new Vector2(0,1),new Vector2(0,1));
  private void Awake()
  {
   station=GetComponent<StationSession>();persistence=GetComponent<StationPersistence>();controls=GetComponent<StationControls>();ending=GetComponent<StationEndingPanel>();input=GetComponent<PlayerInputReader>();canvas=RuntimeUi.CreateCanvas("ES_StationTitle",gameObject.scene,155);var shade=RuntimeUi.Fill("Shade",canvas.transform,Vector2.zero);RuntimeUi.Image(shade,new Color(0,0,0,.92f));dialog=shade.gameObject;var p=RuntimeUi.Rect("Panel",shade,new Vector2(1200,710),Vector2.zero,new Vector2(.5f,.5f),new Vector2(.5f,.5f));RuntimeUi.Image(p,new Color(.025f,.045f,.07f));RuntimeUi.Label(RuntimeUi.Rect("Title",p,new Vector2(1100,70),new Vector2(50,-25),new Vector2(0,1),new Vector2(0,1)),34).text="EchoStation · 废弃生态观测站";
   detail=RuntimeUi.Label(RuntimeUi.Rect("Detail",p,new Vector2(1100,135),new Vector2(50,-120),new Vector2(0,1),new Vector2(0,1)),22);
   ContinueButton=RuntimeUi.Button(Rect(p,"Continue",50,-300),"返回当前探索",Continue);NewButton=RuntimeUi.Button(Rect(p,"New",630,-300),"开始新局",AskNew);LoadButton=RuntimeUi.Button(Rect(p,"Load",50,-395),"读取手动存档",BeginLoad);RuntimeUi.Label(Rect(p,"SaveNotice",630,-395),19).text="S2独立单槽 · F5保存 / F9读取\n新局和结局均不自动覆盖手动档。";
   ConfirmNewButton=RuntimeUi.Button(Rect(p,"ConfirmNew",50,-510),"确认清空当前进度并开始新局",BeginNew,20);CancelNewButton=RuntimeUi.Button(Rect(p,"CancelNew",630,-510),"取消新局",()=>{confirming=false;Refresh();});RuntimeUi.Label(RuntimeUi.Rect("Footer",p,new Vector2(1100,55),new Vector2(50,-625),new Vector2(0,1),new Vector2(0,1)),18).text="无战斗 · 探索与环境线索 · Echo为本地脚本助手\nWASD / 鼠标 · E交互 · J日志 · T文字 · Tab指令";dialog.SetActive(false);
  }
  private void OnEnable(){controls.SaveRequested+=Save;controls.LoadRequested+=BeginLoad;controls.NewRequested+=OpenTitle;ending.TitleRequested+=OpenTitle;}
  private void OnDisable(){if(controls){controls.SaveRequested-=Save;controls.LoadRequested-=BeginLoad;controls.NewRequested-=OpenTitle;}if(ending)ending.TitleRequested-=OpenTitle;if(IsOpen&&!ending.HasEnded){IsOpen=false;if(dialog)dialog.SetActive(false);if(input)input.SetUiBlocked(false);}}
  private void Save(){if(!IsChangingSession)persistence.TrySave();}
  public void OpenTitle()
  {
   if(!station.Ready||persistence.Busy||IsChangingSession)return;station.Dialogue.SetOpen(false);GetComponent<JournalPanel>().SetOpen(false);controls.SetOpen(false);GetComponent<StationTerminalPanel>().Close();GetComponent<StationPowerPanel>().Close();ending.HideForTitle();IsOpen=true;confirming=false;dialog.SetActive(true);input.SetUiBlocked(true);Refresh();
  }
  private void Refresh(){ContinueButton.interactable=!ending.HasEnded;NewButton.interactable=!confirming;ConfirmNewButton.gameObject.SetActive(confirming);CancelNewButton.gameObject.SetActive(confirming);detail.text=confirming?"开始新局会清空当前未保存进度、日志与对话。现有手动档会保留。\n请确认，或取消后继续当前会话。":ending.HasEnded?"本次复查已结束。可以开始新局，或读取结局选择前的手动档。":"可以返回探索、读取手动档，或确认开始新局。\n游戏不自动读取或保存。结束处置前可用F5手动保存。";}
  private void Continue(){if(!IsOpen||ending.HasEnded||IsChangingSession||!input.HasApplicationFocus)return;HideTitle();station.SetStatus("已返回探索，单击游戏画面恢复鼠标。");}
  private void AskNew(){if(!IsOpen||IsChangingSession||!input.HasApplicationFocus)return;confirming=true;Refresh();}
  private void BeginNew(){if(!IsOpen||!confirming||IsChangingSession||persistence.Busy||!input.HasApplicationFocus)return;StartCoroutine(NewGame());}
  private void BeginLoad(){if(IsChangingSession||persistence.Busy||!station.Ready||!input.HasApplicationFocus)return;StartCoroutine(LoadGame());}
  private IEnumerator LoadGame(){IsChangingSession=true;yield return persistence.Load();IsChangingSession=false;if(persistence.LastSucceeded)HideTitle();else if(IsOpen){input.SetUiBlocked(true);detail.text=persistence.LastMessage;}}
  private IEnumerator NewGame(){IsChangingSession=true;yield return persistence.RestoreData(FreshData());IsChangingSession=false;if(persistence.LastSucceeded){HideTitle();station.SayLine("enter_valley");station.SetStatus("新局已开始，尚未保存。原手动档保持不变。单击游戏画面恢复鼠标。");}else{input.SetUiBlocked(true);detail.text=persistence.LastMessage;}}
  public static StationSaveData FreshData()
  {
   var player=StationLayout.PlayerSpawn;var echo=StationLayout.EchoSpawn;var memory=new MemoryStore();memory.AddEvent("visited_valley",0,"进入入口山谷。",MemoryPriority.Low);memory.AddCharacter(new CharacterMemory("player_current_zone",CharacterMemoryKind.PlayerKnowledge,"玩家位于入口山谷。","valley"));return new StationSaveData{Player=new SavedPose(player.x,player.y,player.z),Echo=new SavedPose(echo.x,echo.y,echo.z),EchoMode="follow",Progress=new ProgressState{Events=new[]{"visited_valley"}},MemoryJson=MemoryJson.Encode(memory.CaptureSnapshot())};
  }
  private void HideTitle(){IsOpen=false;confirming=false;dialog.SetActive(false);input.SetUiBlocked(false);input.SetPointerCaptured(false);}
  private void Update(){if(IsOpen&&!IsChangingSession&&input.HasApplicationFocus&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){if(confirming){confirming=false;Refresh();}else Continue();}}
  private void OnDestroy(){if(canvas)Destroy(canvas.gameObject);}
 }
}
