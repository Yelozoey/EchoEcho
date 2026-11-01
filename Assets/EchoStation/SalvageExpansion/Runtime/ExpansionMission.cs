using System;
using System.Linq;
using EchoStation.Salvage;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace EchoStation.SalvageExpansion
{
    [DefaultExecutionOrder(90)]
    public sealed class ExpansionMission:MonoBehaviour,IExpansionSaveSection
    {
        ExpansionWorld world;FacilityController facility;EnvironmentHazards hazards;ExpansionEcho echo;ExpansionPersistence persistence;ExpansionSnapshot initial;IDisposable lease;Text panel;GameObject canvas;bool initialized,ending,restarting,recapture;int steps;
        public string Key=>"mission";public float Elapsed{get;private set;}public bool Completed{get;private set;}public bool TutorialSkipped{get;private set;}public bool BoardOpen=>canvas&&canvas.activeSelf;public bool CanComplete=>world&&world.AtDock&&!hazards.Failed&&world.Inventory.Get(ExpansionCatalog.Core).Location==InventoryItemLocation.Delivered;public int ExtraValue=>world.Inventory.Records.Where(r=>r.Location==InventoryItemLocation.Delivered).Sum(r=>world.Inventory.Definition(r.Id).Value);
        public string Tutorial
        {get{if(TutorialSkipped)return "引导已跳过 · Tab 可回看";if((steps&1)==0)return "01 按I查看背包，选择便携电池或工具并装备。";if((steps&2)==0)return "02 看向附近设备，R打开Echo，选择调查目标。";if(!facility.ValveClosed&&!facility.FanPowered)return "03 上路蒸汽可等安全窗，或给通风供电；下路可用扳手关阀。";if(!facility.Isolated)return "04 装备绝缘工具，对准隔离器按E，消除核心区漏电。";if(facility.Clamps!=7)return "05 装备扳手，对准核心周围三个夹具逐个按E。";if(world.Inventory.Get(ExpansionCatalog.Core).Location==InventoryItemLocation.World)return "06 I预留2×3空间；E收纳核心。数据校验和额外物资可选。";if(!CanComplete)return "07 带核心回泊位，E预览交付，Enter确认。";return "08 核心已交付，Tab主动结算或继续探索。";}}
        void Start()=>Initialize();
        public void Initialize()
        {
            if(initialized)return;world=GetComponent<ExpansionWorld>();facility=GetComponent<FacilityController>();hazards=GetComponent<EnvironmentHazards>();echo=GetComponent<ExpansionEcho>();persistence=GetComponent<ExpansionPersistence>();if(!world||!facility||!hazards||!echo||!persistence)throw new InvalidOperationException("Mission services required.");echo.Initialize();persistence.Register(this);persistence.Restored+=Restored;hazards.RestartRequested=RestartNow;world.ExtraHud=()=>"防护压力 "+hazards.Pressure.ToString("F0")+"/100  ·  设施电池 "+facility.Charge.ToString("F0")+"秒  · Echo "+echo.Battery.Charge.ToString("F0")+"%\n"+Tutorial;initialized=true;initial=persistence.Capture();
        }
        public void Advance(float seconds,bool paused){if(!float.IsFinite(seconds)||seconds<0||seconds>1)throw new ArgumentOutOfRangeException();if(!initialized||paused||Completed||hazards.Failed)return;Elapsed=Mathf.Min(108000,Elapsed+seconds);if(world.Backpack.IsOpen)steps|=1;if(echo.DiscoveredCount>0&&echo.LastSpeech!="脚本协作在线。R 查看Echo。")steps|=2;}
        public bool OpenBoard()
        {
            if(!initialized||hazards.Failed||world.Pause.Blocked||!world.Input.HasApplicationFocus)return false;ShowBoard();return true;
        }
        void ShowBoard(){if(!panel){panel=SalvageUi.Panel(transform,"ExpansionTaskBoard",1050,820,122);canvas=SalvageUi.CanvasOf(panel);canvas.AddComponent<GraphicRaycaster>();var root=panel.transform.parent;SalvageWidgets.Button(root,"Skip","跳过引导",28,735,250,()=>{TutorialSkipped=true;Refresh();});SalvageWidgets.Button(root,"Review","回看引导",300,735,250,()=>{TutorialSkipped=false;steps=0;Refresh();});SalvageWidgets.Button(root,"Close","返回 [Tab / Esc]",740,735,280,CloseBoard);panel.rectTransform.offsetMin=new Vector2(28,100);}if(lease==null)lease=world.Pause.Acquire();world.Input.SetPointerCaptured(false);canvas.SetActive(true);Refresh();}
        public void CloseBoard(){if(Completed)return;ending=restarting=false;if(canvas)canvas.SetActive(false);lease?.Dispose();lease=null;recapture=true;}
        public bool RequestCompletion(){if(!BoardOpen||Completed||!CanComplete)return false;ending=true;restarting=false;Refresh();return true;}
        public bool ConfirmCompletion(){if(!ending||!BoardOpen||!CanComplete)return false;ending=false;Completed=true;world.ActionsAllowed=false;echo.CancelTask();Refresh();return true;}
        public void RequestRestart(){if(!BoardOpen)return;restarting=true;ending=false;Refresh();}
        public bool ConfirmRestart(){if(!restarting)return false;RestartNow();return !Completed&&!hazards.Failed;}
        public void RestartNow(){if(initial==null)return;if(!persistence.TryRestore(initial,out var error))world.Say("无法重开："+error);}
        void Restored(){ending=restarting=false;if(canvas)canvas.SetActive(false);lease?.Dispose();lease=null;recapture=true;if(Completed)ShowBoard();}
        void Refresh()
        {
            if(!panel)return;panel.text=(Completed?"任务完成":"环境回收任务")+"\n\n主目标：数据核心 "+(world.Inventory.Get(ExpansionCatalog.Core).Location==InventoryItemLocation.Delivered?"已交付":"待交付")+"\n额外回收价值："+ExtraValue+"   数据完整归档："+(facility.Archived?"是":"否")+"\n用时："+TimeSpan.FromSeconds(Elapsed).ToString(@"mm\:ss")+"   危险暴露次数："+hazards.Incidents+"\n\n"+Tutorial+"\n\nI 背包与装备 · E 交互 · G 安全放下\nR Echo调查/跟随/待命 · F手电 · F5保存/F9读档\nF10 画质/分辨率 · 整理时暂停，操作阀门时危险继续\n\n"+(restarting?"确认重新开始本局？Enter确认 / Esc取消":ending?"确认结束本局？Enter确认 / Esc取消":Completed?"N重开本局 · 窗口关闭结束试玩":CanComplete?"Enter申请结算 · N重开 · Esc继续探索":"核心交付后在泊位结算 · N重开 · Esc继续探索");
        }
        void Update()
        {
            if(!initialized)return;Advance(Mathf.Min(Time.deltaTime,.1f),world.Pause.Paused||!world.Input.HasApplicationFocus);if(world.Backpack.IsOpen)steps|=1;var k=Keyboard.current;if(k==null||!world.Input.HasApplicationFocus)return;
            if(BoardOpen){if(k.nKey.wasPressedThisFrame)RequestRestart();else if(k.enterKey.wasPressedThisFrame){if(restarting)ConfirmRestart();else if(ending)ConfirmCompletion();else RequestCompletion();}else if(k.escapeKey.wasPressedThisFrame){if(restarting||ending){ending=restarting=false;Refresh();}else CloseBoard();}else if(k.tabKey.wasPressedThisFrame)CloseBoard();}
            else if(k.tabKey.wasPressedThisFrame&&world.Input.CanPlay)OpenBoard();
        }
        void LateUpdate(){if(recapture){recapture=false;if(world)world.Pause.RestorePointer();}}
        public JToken Capture()=>new JObject{{"elapsed",Elapsed},{"completed",Completed},{"steps",steps},{"skip",TutorialSkipped}};
        public bool TryPrepare(ExpansionSnapshot snapshot,JToken value,out Action apply,out Action rollback,out string error)
        {
            apply=rollback=null;error="";try{var v=SaveSectionJson.Fields(value,"elapsed","completed","steps","skip");SaveSectionJson.Number(v["elapsed"],0,108000);bool complete=SaveSectionJson.Boolean(v["completed"]);SaveSectionJson.Integer(v["steps"],0,3);SaveSectionJson.Boolean(v["skip"]);if(complete&&(snapshot.Inventory.Single(r=>r.Id==ExpansionCatalog.Core).Location!=InventoryItemLocation.Delivered||(bool)snapshot.Sections["hazards"]["failed"]))throw new ArgumentException("Completion conditions inconsistent.");var before=(JObject)Capture();var frozen=(JObject)v.DeepClone();apply=()=>Apply(frozen);rollback=()=>Apply(before);return true;}catch(Exception e)when(e is ArgumentException||e is OverflowException){error=e.Message;return false;}
        }
        void Apply(JObject v){Elapsed=(float)v["elapsed"];Completed=(bool)v["completed"];steps=(int)v["steps"];TutorialSkipped=(bool)v["skip"];world.ActionsAllowed=!Completed&&!hazards.Failed;}
        void OnDisable(){if(canvas)canvas.SetActive(false);lease?.Dispose();lease=null;}
        void OnDestroy(){if(persistence)persistence.Restored-=Restored;}
    }
}
