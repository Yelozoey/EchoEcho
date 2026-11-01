using System.Collections.Generic;
using EchoStation.Echo;
using EchoStation.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EchoStation.Player
{
    [DefaultExecutionOrder(-70),RequireComponent(typeof(PlayerInputReader)),DisallowMultipleComponent]
    public sealed class CabinControls:MonoBehaviour
    {
        private Canvas canvas;private GameObject dialog;private CabinDemo demo;private PlayerInputReader input;private Text speech,selection;private readonly Dictionary<string,Button> targets=new Dictionary<string,Button>();private readonly Dictionary<IntentKind,Button> commands=new Dictionary<IntentKind,Button>();
        public bool IsOpen{get;private set;}public string SelectedTarget{get;private set;}="note_maintenance_01";
        public Button SaveButton{get;private set;}public Button LoadButton{get;private set;}
        public Button TargetButton(string id)=>targets.TryGetValue(id,out var button)?button:null;
        public Button CommandButton(IntentKind kind)=>commands.TryGetValue(kind,out var button)?button:null;
        private void Awake(){if(Application.isPlaying)Build();}
        private void Build()
        {
            if(canvas)return;demo=GetComponent<CabinDemo>();input=GetComponent<PlayerInputReader>();canvas=RuntimeUi.CreateCanvas("ES_CabinControls",gameObject.scene,120);var instructions=RuntimeUi.Label(RuntimeUi.Rect("Instructions",canvas.transform,new Vector2(1200,65),new Vector2(28,-20),new Vector2(0,1),new Vector2(0,1)),17);instructions.text="WASD 移动 · 鼠标视角 · 空格跳跃 · E 读取 · F 手电 · J 日志\nTab Echo 指令 / 显示设置 · F5 保存 · F9 读取 · Esc 释放鼠标 · 点击继续";
            var speechRect=RuntimeUi.Rect("EchoStatus",canvas.transform,new Vector2(1200,65),new Vector2(0,148),new Vector2(.5f,0),new Vector2(.5f,.5f));RuntimeUi.Image(speechRect,new Color(.015f,.03f,.05f,.85f),false);speech=RuntimeUi.Label(RuntimeUi.Fill("Text",speechRect,new Vector2(20,6)),19,TextAnchor.MiddleCenter);
            var shade=RuntimeUi.Fill("Commands",canvas.transform,Vector2.zero);RuntimeUi.Image(shade,new Color(0,0,0,.7f));dialog=shade.gameObject;var panel=RuntimeUi.Rect("Panel",shade,new Vector2(1080,700),Vector2.zero,new Vector2(.5f,.5f),new Vector2(.5f,.5f));RuntimeUi.Image(panel,new Color(.025f,.045f,.07f,.98f));RuntimeUi.Label(Rect(panel,"Heading",990,45,35,-24),27).text="Echo 控制与显示";selection=RuntimeUi.Label(Rect(panel,"Selection",990,40,35,-86),19);
            var ids=new[]{"note_maintenance_01","terminal_data_01","note_roster_01","scan_mark_01","power_panel_01"};var names=new[]{"主室维护记录","主室终端","储物间值班记录","储物间可疑位置","供电面板"};for(var i=0;i<ids.Length;i++){var id=ids[i];targets[id]=RuntimeUi.Button(Rect(panel,"Target_"+i,190,62,35+i*205,-142),names[i],()=>SelectTarget(id),17);}
            var kinds=new[]{IntentKind.Follow,IntentKind.Wait,IntentKind.Investigate,IntentKind.Warn};var labels=new[]{"跟随  [1]","待命  [2]","调查选中位置  [3]","警告供电面板  [4]"};for(var i=0;i<kinds.Length;i++){var kind=kinds[i];commands[kind]=RuntimeUi.Button(Rect(panel,"Command_"+i,235,60,35+i*255,-232),labels[i],()=>Submit(kind));}
            SaveButton=RuntimeUi.Button(Rect(panel,"Save",235,52,35,-325),"保存进度  [F5]",()=>demo.Save());LoadButton=RuntimeUi.Button(Rect(panel,"Load",235,52,290,-325),"读取进度  [F9]",()=>{if(demo.Load())SetOpen(false);});RuntimeUi.Button(Rect(panel,"Close",235,52,545,-325),"返回探索  [Tab / Esc]",()=>SetOpen(false));RuntimeUi.Button(Rect(panel,"Quit",235,52,800,-325),"退出",()=>Application.Quit());RuntimeUi.Label(Rect(panel,"DisplayHeading",990,36,35,-410),20).text="窗口分辨率与无边全屏";for(var i=0;i<3;i++){var width=new[]{1280,1600,1920}[i];var height=new[]{720,900,1080}[i];RuntimeUi.Button(Rect(panel,"Resolution_"+width,235,52,35+i*255,-460),width+" × "+height,()=>SetResolution(width,height));}RuntimeUi.Button(Rect(panel,"Fullscreen",235,52,800,-460),"切换全屏 / 窗口",ToggleFullscreen);RuntimeUi.Label(Rect(panel,"Help",990,85,35,-550),18).text="Echo 只提供调查、扫描和警告，不会操作门或设备。\n调查需要选择位置；警告指令需要 Echo 实际感知供电面板。关闭面板后点击画面继续探索。";dialog.SetActive(false);demo.StatusChanged+=OnStatus;
        }
        private static RectTransform Rect(Transform parent,string name,float width,float height,float x,float y)=>RuntimeUi.Rect(name,parent,new Vector2(width,height),new Vector2(x,y),new Vector2(0,1),new Vector2(0,1));
        private void Start(){demo.Echo.Speech+=OnSpeech;OnStatus(demo.Status);SelectTarget(SelectedTarget);}
        public bool SelectTarget(string id){if(!WorldTarget.Active.TryGet(id,out var target)||target.gameObject.scene!=gameObject.scene)return false;SelectedTarget=id;if(selection)selection.text="调查位置："+target.DisplayName;foreach(var pair in targets)pair.Value.GetComponent<Image>().color=pair.Key==id?RuntimeUi.SelectedColor:RuntimeUi.ButtonColor;return true;}
        public bool Submit(IntentKind kind)
        {
            if(!demo.Ready)return false;IntentRequest request;switch(kind){case IntentKind.Follow:request=ScriptCommands.Follow();break;case IntentKind.Wait:request=ScriptCommands.Wait();break;case IntentKind.Investigate:request=ScriptCommands.Investigate(SelectedTarget);break;case IntentKind.Warn:request=ScriptCommands.Warn("power_panel_01");break;default:return false;}var ok=demo.Echo.TrySubmit(request,out _);if(ok){demo.SetStatus(kind==IntentKind.Follow?"Echo：开始跟随。":kind==IntentKind.Wait?"Echo：原地待命。":kind==IntentKind.Investigate?"Echo：正在前往所选位置调查。":"Echo：已确认危险设备。请保持距离。");SetOpen(false);}else demo.SetStatus("Echo：当前无法执行该指令，请检查位置和感知范围。");return ok;
        }
        public bool SetOpen(bool value)
        {Build();if(value&&(GetComponent<JournalPanel>().IsOpen||input.UiBlocked&&!IsOpen||!input.HasApplicationFocus))return false;IsOpen=value;dialog.SetActive(value);input.SetUiBlocked(value);return true;}
        public void SetResolution(int width,int height){if((width!=1280||height!=720)&&(width!=1600||height!=900)&&(width!=1920||height!=1080))return;Screen.SetResolution(width,height,FullScreenMode.Windowed);}
        public void ToggleFullscreen()=>Screen.SetResolution(Screen.width,Screen.height,Screen.fullScreenMode==FullScreenMode.Windowed?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
        private void Update()
        {
            if(!demo||!demo.Ready||!input.HasApplicationFocus)return;var keys=Keyboard.current;if(keys==null)return;if(keys.tabKey.wasPressedThisFrame){SetOpen(!IsOpen);return;}if(IsOpen){if(keys.escapeKey.wasPressedThisFrame)SetOpen(false);return;}if(!input.CanPlay)return;if(keys.digit1Key.wasPressedThisFrame)Submit(IntentKind.Follow);else if(keys.digit2Key.wasPressedThisFrame)Submit(IntentKind.Wait);else if(keys.digit3Key.wasPressedThisFrame){var target=GetComponent<PlayerInteraction>().Selection.Target;if(target)SelectTarget(target.Id);Submit(IntentKind.Investigate);}else if(keys.digit4Key.wasPressedThisFrame)Submit(IntentKind.Warn);else if(keys.f5Key.wasPressedThisFrame)demo.Save();else if(keys.f9Key.wasPressedThisFrame)demo.Load();
        }
        private void OnStatus(string value){if(speech)speech.text=value;}
        private void OnSpeech(string value,bool refusal)=>OnStatus("Echo："+value);
        private void OnDisable(){if(IsOpen)SetOpen(false);if(canvas)canvas.enabled=false;}
        private void OnEnable(){if(canvas)canvas.enabled=true;}
        private void OnDestroy(){if(demo){demo.StatusChanged-=OnStatus;if(demo.Echo)demo.Echo.Speech-=OnSpeech;}if(canvas)Destroy(canvas.gameObject);}
    }
}
