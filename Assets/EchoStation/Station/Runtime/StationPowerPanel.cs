using System.Linq;
using EchoStation.Player;
using EchoStation.Puzzles;
using EchoStation.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace EchoStation.Station
{
 [DisallowMultipleComponent]
 public sealed class StationPowerPanel:MonoBehaviour
 {
  private StationSession station;private PlayerInputReader input;private PlayerInteraction interaction;private Canvas canvas;private GameObject dialog;private Text status;private LocalFogField fog;private StationGate barrier;
  private bool exhaust,recirculation=true,experiment=true;private readonly Button[] toggles=new Button[3];
  public VentilationCycle Cycle {get;}=new VentilationCycle();
  public bool IsOpen {get;private set;}public Button SubmitButton {get;private set;}
  public Button ToggleButton(int index)=>index>=0&&index<3?toggles[index]:null;
  private RectTransform Rect(Transform p,string name,float w,float h,float x,float y)=>RuntimeUi.Rect(name,p,new Vector2(w,h),new Vector2(x,y),new Vector2(0,1),new Vector2(0,1));
  private void Awake()
  {
   station=GetComponent<StationSession>();input=GetComponent<PlayerInputReader>();interaction=GetComponent<PlayerInteraction>();canvas=RuntimeUi.CreateCanvas("ES_PowerPanel",gameObject.scene,136);var shade=RuntimeUi.Fill("Shade",canvas.transform,Vector2.zero);RuntimeUi.Image(shade,new Color(0,0,0,.8f));dialog=shade.gameObject;var p=RuntimeUi.Rect("Panel",shade,new Vector2(1100,570),Vector2.zero,new Vector2(.5f,.5f),new Vector2(.5f,.5f));RuntimeUi.Image(p,new Color(.025f,.045f,.07f));RuntimeUi.Label(Rect(p,"Title",1000,50,40,-25),28).text="A区通风供电";RuntimeUi.Label(Rect(p,"SelectionHint",1000,55,40,-95),19).text="以下是待提交的回路选项，确认之前不执行送电。请核对已归档的回路说明。";
   for(var i=0;i<3;i++){var index=i;toggles[i]=RuntimeUi.Button(Rect(p,"Toggle_"+i,310,85,40+350*i,-180),"",()=>Toggle(index),23);}
   status=RuntimeUi.Label(Rect(p,"State",1000,75,40,-300),20);SubmitButton=RuntimeUi.Button(Rect(p,"Submit",460,58,40,-410),"确认送电",()=>Submit());RuntimeUi.Button(Rect(p,"Cancel",460,58,590,-410),"取消 / 返回  [Esc]",Close);dialog.SetActive(false);Refresh();
  }
  private void Start(){fog=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LocalFogField>()).Single();barrier=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StationGate>()).Single(g=>g.name=="Vent_Isolation");SyncVisuals();}
  private void OnEnable(){if(interaction)interaction.InformationRead+=OnDevice;}
  private void OnDisable(){if(interaction)interaction.InformationRead-=OnDevice;Close();}
  private void OnDevice(string _){if(!station.Ready||!input.CanPlay)return;var target=interaction.Selection.Target;var device=target?target.GetComponent<StationDevice>():null;if(!device||device.Kind!=StationDeviceKind.Power)return;IsOpen=true;dialog.SetActive(true);input.SetUiBlocked(true);Refresh();}
  public void Toggle(int index){if(!IsOpen||Cycle.Powered)return;if(index==0)exhaust=!exhaust;else if(index==1)recirculation=!recirculation;else if(index==2)experiment=!experiment;Refresh();}
  public PuzzleResult Submit()
  {
   if(!IsOpen||!input.HasApplicationFocus)return PuzzleResult.Invalid;if(!station.Progress.Has("pz03")&&!station.CanRecordPuzzle("pz03")){status.text="事件记忆容量不足，送电未提交。";return PuzzleResult.Invalid;}
   var result=PowerPuzzle.Submit(station.Progress,exhaust,recirculation,experiment);
   if(result==PuzzleResult.Completed){Cycle.Begin();station.SayLine("power_success");}
   else if(result==PuzzleResult.WrongCircuit)station.SayLine("wrong_power");else if(result==PuzzleResult.PrerequisiteMissing)station.SetStatus("请先完成小屋维护授权。");
   Refresh();return result;
  }
  public bool RestoreCycle(bool powered,float elapsed){if(!Cycle.TryRestore(powered,elapsed))return false;exhaust=powered;recirculation=experiment=!powered;SyncVisuals();if(IsOpen)Refresh();return true;}
  private void SyncVisuals(){if(fog&&fog.Recovery!=Cycle.Fraction)fog.SetRecovery(Cycle.Fraction);if(barrier&&barrier.IsOpen!=Cycle.PassageReady)barrier.Apply(Cycle.PassageReady);}
  private void Refresh(){var states=new[]{exhaust,recirculation,experiment};var labels=new[]{"排风","内循环","实验支路"};for(var i=0;i<3;i++){toggles[i].GetComponentInChildren<Text>().text=labels[i]+"："+(states[i]?"ON":"OFF");toggles[i].interactable=!Cycle.Powered;}SubmitButton.interactable=!Cycle.Powered;status.text=Cycle.PassageReady?"通风已恢复，通道可以通行。":Cycle.Powered?"正在排风，局部雾气将在3秒内消退。":"请核对三条支路，正确组合由你确认后执行。";}
  private void Update(){if(station.Restoring)return;if(IsOpen&&(!input.HasApplicationFocus||Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame))Close();if(!Cycle.Powered||Cycle.PassageReady)return;Cycle.Step(Time.deltaTime);SyncVisuals();if(Cycle.PassageReady){station.SetStatus("通风已恢复，局部雾气降低，通道已经开放。");if(IsOpen)Refresh();}}
  public void Close(){if(!IsOpen)return;IsOpen=false;if(dialog)dialog.SetActive(false);if(input)input.SetUiBlocked(false);}
  private void OnDestroy(){if(canvas)Destroy(canvas.gameObject);}
 }
}
