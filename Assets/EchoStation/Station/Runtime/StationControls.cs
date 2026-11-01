using System;
using EchoStation.Echo;
using EchoStation.Dialogue.Integration;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EchoStation.Station
{
    [DefaultExecutionOrder(-65)]
    public sealed class StationControls:MonoBehaviour
    {
        private StationSession station;private PlayerInputReader input;private Canvas canvas;private GameObject dialog;private Text status,selection;private string selected="scan_mark_01";
        public bool IsOpen {get;private set;}
        public Canvas Canvas=>canvas;
        public event Action SaveRequested,LoadRequested,NewRequested;
        public void RequestSave()=>SaveRequested?.Invoke();public void RequestLoad()=>LoadRequested?.Invoke();
        private RectTransform Row(Transform p,string name,float x,float y,float w=340,float h=48)=>RuntimeUi.Rect(name,p,new Vector2(w,h),new Vector2(x,y),new Vector2(0,1),new Vector2(0,1));
        private void Awake()
        {
            station=GetComponent<StationSession>();input=GetComponent<PlayerInputReader>();canvas=RuntimeUi.CreateCanvas("ES_S2Controls",gameObject.scene,120);
            RuntimeUi.Label(Row(canvas.transform,"Help",28,-18,1750,65),18).text="WASD 移动 · 鼠标视角 · 空格跳跃 · E 交互 · F 手电 · J 日志 · T 文字对话\nTab 指令/显示 · 1 跟随 · 2 待命 · 3 调查眼前目标 · F5 保存 · F9 读取 · Esc 释放鼠标";
            status=RuntimeUi.Label(RuntimeUi.Rect("Status",canvas.transform,new Vector2(1600,70),new Vector2(0,100),new Vector2(.5f,0),new Vector2(.5f,0)),20,TextAnchor.MiddleCenter);
            var shade=RuntimeUi.Fill("Shade",canvas.transform,Vector2.zero);RuntimeUi.Image(shade,new Color(0,0,0,.7f));dialog=shade.gameObject;
            var p=RuntimeUi.Rect("Panel",shade,new Vector2(1200,820),Vector2.zero,new Vector2(.5f,.5f),new Vector2(.5f,.5f));RuntimeUi.Image(p,new Color(.025f,.045f,.07f));
            RuntimeUi.Label(Row(p,"Title",30,-20,1100,50),27).text="Echo 指令与显示";
            selection=RuntimeUi.Label(Row(p,"Selected",30,-80,1100,45),19);
            var scroll=RuntimeUi.ScrollViewport("Targets",p,new Vector2(30,-135),new Vector2(1140,300),out var content);
            // Rows are refreshed when opening, as terminal data can become available later.
            targetRoot=content;scroll.horizontal=false;
            RuntimeUi.Button(Row(p,"Follow",30,-455),"跟随",()=>Submit(ScriptCommands.Follow()));RuntimeUi.Button(Row(p,"Wait",430,-455),"待命",()=>Submit(ScriptCommands.Wait()));RuntimeUi.Button(Row(p,"Investigate",830,-455),"调查所选位置",()=>Submit(ScriptCommands.Investigate(selected)));
            RuntimeUi.Button(Row(p,"Save",30,-520),"手动保存  [F5]",RequestSave);RuntimeUi.Button(Row(p,"Load",430,-520),"读取存档  [F9]",RequestLoad);RuntimeUi.Button(Row(p,"New",830,-520),"新局 / 标题",()=>NewRequested?.Invoke());
            foreach(var pair in new[]{(1280,720),(1600,900),(1920,1080)}) {var size=pair;var i=size.Item1==1280?0:size.Item1==1600?1:2;RuntimeUi.Button(Row(p,"Res"+i,30+400*i,-585),size.Item1+" × "+size.Item2,()=>Screen.SetResolution(size.Item1,size.Item2,FullScreenMode.Windowed));}
            RuntimeUi.Button(Row(p,"Fullscreen",30,-650),"全屏 / 窗口",()=>Screen.SetResolution(Screen.width,Screen.height,Screen.fullScreenMode==FullScreenMode.Windowed?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed));RuntimeUi.Button(Row(p,"Close",830,-650),"返回探索",()=>SetOpen(false));
            RuntimeUi.Label(Row(p,"Limits",30,-715,1120,65),18).text="Echo只能提供信息、移动调查和警告。门、终端和回路由你操作。\n文字示例：调查储藏室可疑点 / 调查校准架 / 给我提示 / 通道终端的密码是多少";
            dialog.SetActive(false);station.StatusChanged+=OnStatus;
        }
        private RectTransform targetRoot;
        private void RefreshTargets(){foreach(Transform c in targetRoot)Destroy(c.gameObject);var all=station.AvailableTargets();for(var i=0;i<all.Length;i++){var target=all[i];RuntimeUi.Button(Row(targetRoot,"Target_"+target.Id,10,-10-i*52,1100,46),target.Name,()=>{selected=target.Id;selection.text="调查目标："+target.Name;});}targetRoot.sizeDelta=new Vector2(0,Mathf.Max(300,all.Length*52+20));selection.text="调查目标："+selected;}
        public bool SetOpen(bool value){if(value&&(input.UiBlocked&&!IsOpen||!input.HasApplicationFocus))return false;IsOpen=value;dialog.SetActive(value);input.SetUiBlocked(value);if(value)RefreshTargets();return true;}
        private void OnStatus(string text){if(status)status.text=text;}
        private void Submit(IntentRequest request){if(station.Ready){var controller=station.GetComponent<ScriptDialogueController>();if(!controller.SubmitPlayerIntent(request))station.SetStatus("当前无法执行，请核对目标、距离和通路。");else if(controller.OrdinaryEventRecordingPaused)station.SetStatus(controller.LastMemoryError);SetOpen(false);}}
        private void Update(){if(!station.Ready||!input.HasApplicationFocus)return;var k=Keyboard.current;if(k==null)return;if(k.tabKey.wasPressedThisFrame){SetOpen(!IsOpen);return;}if(IsOpen){if(k.escapeKey.wasPressedThisFrame)SetOpen(false);return;}if(!input.CanPlay)return;if(k.digit1Key.wasPressedThisFrame)Submit(ScriptCommands.Follow());if(k.digit2Key.wasPressedThisFrame)Submit(ScriptCommands.Wait());if(k.digit3Key.wasPressedThisFrame){var t=GetComponent<PlayerInteraction>().Selection.Target;if(t)Submit(ScriptCommands.Investigate(t.Id));}if(k.f5Key.wasPressedThisFrame)RequestSave();if(k.f9Key.wasPressedThisFrame)RequestLoad();}
        private void OnApplicationFocus(bool value){if(!value&&IsOpen)SetOpen(false);}
        private void OnDisable(){if(IsOpen)SetOpen(false);if(canvas)canvas.enabled=false;}
        private void OnEnable(){if(canvas)canvas.enabled=true;}
        private void OnDestroy(){if(station)station.StatusChanged-=OnStatus;if(canvas)Destroy(canvas.gameObject);}
    }
}
