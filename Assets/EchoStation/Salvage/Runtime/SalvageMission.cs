using System;
using System.Linq;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EchoStation.Salvage
{
    [RequireComponent(typeof(SalvageDelivery))]
    public sealed class SalvageMission:MonoBehaviour
    {
        SalvageCarry carry;SalvageDelivery delivery;SalvageMotor motor;PlayerInputReader input;SalvageEcho echo;
        Text board,hud;float hudTimer;bool cached;uint hudRevision=uint.MaxValue;
        public double Elapsed{get;private set;}
        public bool Completed{get;private set;}
        public bool BoardOpen{get;private set;}
        public bool EndPending{get;private set;}
        public bool RestartPending{get;private set;}
        public bool CanComplete{get{Cache();return delivery.Count>=3&&delivery.Points>=12;}}
        public string Notice{get;set;}="";
        public event Action<RectTransform> BoardOpened;
        public event Action Restarting;
        void Cache()
        {if(cached)return;carry=GetComponent<SalvageCarry>();delivery=GetComponent<SalvageDelivery>();motor=GetComponent<SalvageMotor>();input=GetComponent<PlayerInputReader>();echo=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();cached=true;}
        void Start()
        {
            Cache();hud=SalvageUi.Panel(transform,"SalvageHUD",640,310,10);var rect=(RectTransform)hud.transform.parent;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(20,-20);hud.fontSize=21;
            var cross=SalvageUi.Panel(transform,"Crosshair",42,42,9);cross.transform.parent.GetComponent<Image>().color=Color.clear;cross.text="+";cross.fontSize=24;cross.alignment=TextAnchor.MiddleCenter;cross.rectTransform.offsetMin=Vector2.zero;cross.rectTransform.offsetMax=Vector2.zero;
        }
        void Update()
        {
            Cache();if(!Completed&&!input.UiBlocked&&Application.isFocused)Elapsed+=Time.deltaTime;
            var keyboard=Keyboard.current;var commands=GetComponent<SalvageCommandPanel>();if(keyboard!=null&&(!commands||!commands.IsOpen))
            {
                if(keyboard.tabKey.wasPressedThisFrame&&!delivery.Pending){if(BoardOpen&&!Completed)CloseBoard();else OpenBoard();}
                if(BoardOpen)
                {
                    if(keyboard.nKey.wasPressedThisFrame)RequestRestart();
                    else if(keyboard.escapeKey.wasPressedThisFrame){if(EndPending||RestartPending){EndPending=RestartPending=false;RefreshBoard();}else if(!Completed)CloseBoard();}
                    else if(keyboard.enterKey.wasPressedThisFrame){if(RestartPending)ConfirmRestart();else if(EndPending)ConfirmCompletion();else RequestCompletion();}
                }
            }
            if(BoardOpen)RefreshBoard();hudTimer-=Time.unscaledDeltaTime;if(hud&&(hudTimer<=0||hudRevision!=carry.Revision)){hudTimer=.2f;hudRevision=carry.Revision;RefreshHud();}
        }
        public bool OpenBoard()
        {Cache();if(delivery.Pending)return false;var commands=GetComponent<SalvageCommandPanel>();if(commands&&commands.IsOpen)commands.Close();BoardOpen=true;input.SetUiBlocked(true);if(!board)board=SalvageUi.Panel(transform,"TaskBoard",850,720,40);SalvageUi.CanvasOf(board).SetActive(true);RefreshBoard();BoardOpened?.Invoke((RectTransform)board.transform.parent);return true;}
        public void CloseBoard(){Cache();if(Completed)return;BoardOpen=EndPending=RestartPending=false;if(board)SalvageUi.CanvasOf(board).SetActive(false);input.SetUiBlocked(false);input.SetPointerCaptured(true);}
        public bool RequestCompletion(){Cache();if(!BoardOpen||Completed||!CanComplete)return false;EndPending=true;RestartPending=false;RefreshBoard();return true;}
        public bool ConfirmCompletion(){if(!EndPending||!CanComplete)return false;Completed=true;EndPending=false;RefreshBoard();return true;}
        public bool RequestRestart(){Cache();if(!BoardOpen)return false;RestartPending=true;EndPending=false;RefreshBoard();return true;}
        public bool ConfirmRestart()
        {
            Cache();if(!RestartPending)return false;Restarting?.Invoke();delivery.Cancel();carry.ResetToInitial();echo.Battery.Restore(100,false);if(Application.isPlaying&&echo.Initialized)echo.Navigation.TryBind(new Vector3(4,0,10));motor.RestorePose(new Vector3(5,.15f,12),90,0);Elapsed=0;Completed=false;EndPending=RestartPending=false;Notice="";CloseBoard();return true;
        }
        public void RestoreProgress(double elapsed,bool completed)
        {Cache();Elapsed=elapsed;Completed=false;EndPending=RestartPending=false;CloseBoard();Completed=completed;if(completed)OpenBoard();}
        void RefreshBoard()
        {
            if(!board)return;var rows=string.Join("\n",carry.Registry.Records.OrderBy(r=>r.Definition.Id).Select(r=>r.Definition.Name+"  "+r.Definition.Kilograms+"kg / "+r.Definition.Points+"分  "+(r.State==CargoState.Delivered?"已交付":r.State==CargoState.Held?"手持":"待回收")));
            board.text="回收任务板  ·  脚本模式\n目标：交付至少3件，合计至少12分\n当前："+delivery.Count+"件 / "+delivery.Points+"分\n\n"+rows+"\n\n"+(Completed?"任务完成！用时 "+Elapsed.ToString("F1")+" 秒":RestartPending?"重新开始将重置本局进度，Enter确认 / Esc取消":EndPending?"确认结束本局？Enter确认 / Esc取消":CanComplete?"已达目标。Enter申请结束；Esc继续回收":"尚未达目标，Esc返回探索")+"\nN 重新开始    F5保存 / F9读档\n"+Notice;
        }
        void RefreshHud()
        {
            var focus=carry.HeldId==null?carry.Focus():null;var held=carry.HeldId==null?"空手":SalvageCatalog.Get(carry.HeldId).Name;
            hud.text=(GetComponent<SalvageCommandPanel>()?"回收探索 · Echo协作（脚本）":"回收切片 R1 · 脚本模式")+"\n交付 "+delivery.Count+"/3 件，"+delivery.Points+"/12 分    Echo "+echo.Battery.Charge.ToString("F0")+"%"+(echo.Docked?" 充电中":"")+"\n手持："+held+"  速度 "+motor.Speed.ToString("F1")+"m/s\nWASD移动 / 鼠标视角 / Space跳跃\nE拾取或泊位交付 / G放下 / F手电 / Tab任务板\n"+(GetComponent<SalvageCommandPanel>()?"R Echo指令 / T文字通信\n":"")+(focus?"E 拿起 "+SalvageCatalog.Get(focus.Id).Name:delivery.AtDock&&carry.HeldId!=null?"E 申请交付":"")+"\n"+carry.LastMessage+(echo.Battery.Charge<25?"\nEcho：电量偏低，建议返回泊位。":"");
        }
    }
}
