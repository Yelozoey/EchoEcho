using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage;
using UnityEngine;
using UnityEngine.UI;
namespace EchoStation.SalvageNext
{
    [DefaultExecutionOrder(200),DisallowMultipleComponent,RequireComponent(typeof(SalvageMission))]
    public sealed class NextHud:MonoBehaviour
    {
        [SerializeField]NextUiTheme theme;
        SalvageMission mission;SalvageDelivery delivery;SalvageCarry carry;SalvageEcho echo;SalvageEchoCoordinator brain;PlayerInputReader input;
        Text progress,zone,mode,charge,held,details,prompt,message;Image battery,promptKey,reticle;CanvasGroup visibility;RectTransform promptBox;float nextUpdate;string lastMessage;float messageUntil;
        public NextUiTheme Theme=>theme;public Canvas Canvas{get;private set;}public bool Visible=>visibility&&visibility.alpha>0;public string Progress=>progress?progress.text:"";public string HeldText=>held?held.text:"";public string BatteryText=>charge?charge.text:"";
        public void Configure(NextUiTheme value){if(Canvas)throw new System.InvalidOperationException("HUD already built");theme=value;}
        void Start()
        {
            if(!theme)throw new System.InvalidOperationException("ArtV2 UI theme required");mission=GetComponent<SalvageMission>();delivery=GetComponent<SalvageDelivery>();carry=GetComponent<SalvageCarry>();input=GetComponent<PlayerInputReader>();echo=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();brain=echo.GetComponent<SalvageEchoCoordinator>();
            foreach(var name in new[]{"SalvageHUD","Crosshair"}){var old=transform.Find(name);if(old){old.gameObject.SetActive(false);Destroy(old.gameObject);}}
            var root=new GameObject("NextHud",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(CanvasGroup));root.transform.SetParent(transform,false);Canvas=root.GetComponent<Canvas>();Canvas.renderMode=RenderMode.ScreenSpaceOverlay;Canvas.sortingOrder=10;var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;visibility=root.GetComponent<CanvasGroup>();visibility.blocksRaycasts=false;visibility.interactable=false;
            var goal=Card("Objective",new Vector2(0,1),new Vector2(28,-28),380,148);theme.Label(goal,"Title",20,12,340,36,"回收任务",24,NextUiTheme.Cyan);progress=theme.Label(goal,"Progress",20,54,340,42,"",28);zone=theme.Label(goal,"Zone",20,108,340,34,"",22,NextUiTheme.Muted);
            var robot=Card("Echo",new Vector2(0,0),new Vector2(28,84),336,148);theme.Label(robot,"Title",20,12,280,36,"ECHO  ·  扫描助手",24,NextUiTheme.Cyan);mode=theme.Label(robot,"Mode",20,54,154,40,"",26);charge=theme.Label(robot,"Charge",176,54,144,40,"",24);var track=SalvageWidgets.Rect("BatteryTrack",robot,20,122,296,7);track.gameObject.AddComponent<Image>().color=new Color(.16f,.26f,.32f);battery=SalvageWidgets.Rect("Battery",track,0,0,296,7).gameObject.AddComponent<Image>();battery.color=NextUiTheme.Cyan;battery.raycastTarget=false;
            var cargo=Card("Cargo",new Vector2(1,0),new Vector2(-28,84),380,148);theme.Label(cargo,"Title",20,12,340,36,"手持物品",24,NextUiTheme.Cyan);held=theme.Label(cargo,"Held",20,54,340,42,"",28);details=theme.Label(cargo,"Details",20,108,340,34,"",22,NextUiTheme.Muted);
            reticle=theme.Icon(root.transform,"Reticle",theme.Crosshair,0,0,64,64);var rr=reticle.rectTransform;rr.anchorMin=rr.anchorMax=rr.pivot=new Vector2(.5f,.5f);rr.anchoredPosition=Vector2.zero;
            promptBox=Card("Interaction",new Vector2(.5f,.5f),new Vector2(0,-66),460,66);promptKey=theme.Icon(promptBox,"Key",theme.E,10,13);prompt=theme.Label(promptBox,"Prompt",58,15,382,36,"",24);
            var hints=Card("Controls",new Vector2(.5f,0),new Vector2(0,22),830,58);Hint(hints,theme.R,"Echo",8);Hint(hints,theme.Tab,"任务板",163);Hint(hints,theme.F,"手电",359);Hint(hints,theme.G,"放下",509);Hint(hints,theme.T,"通信",659);
            var feedback=Card("Feedback",new Vector2(.5f,0),new Vector2(0,92),740,62);message=theme.Label(feedback,"Message",20,12,700,38,"",24);message.alignment=TextAnchor.MiddleCenter;RefreshNow();
        }
        RectTransform Card(string name,Vector2 anchor,Vector2 position,float w,float h)
        {var rect=SalvageWidgets.Rect(name,Canvas.transform,0,0,w,h);rect.anchorMin=rect.anchorMax=rect.pivot=anchor;rect.anchoredPosition=position;theme.Box(rect);return rect;}
        void Hint(RectTransform parent,Sprite sprite,string title,float x){theme.Icon(parent,"Key_"+title,sprite,x,8);theme.Label(parent,"Hint_"+title,x+46,12,130,36,title,24,NextUiTheme.Muted);}
        void LateUpdate(){if(!visibility)return;visibility.alpha=input.UiBlocked?0:1;if(Time.unscaledTime>=nextUpdate)RefreshNow();}
        static void Set(Text label,string value){if(label.text!=value)label.text=value;}
        public void RefreshNow()
        {
            if(!Canvas)return;nextUpdate=Time.unscaledTime+.15f;visibility.alpha=input.UiBlocked?0:1;
            Set(progress,delivery.Count+" / 3 件    "+delivery.Points+" / 12 分");Set(zone,mission.CanComplete?"目标已达成 · Tab 确认结算":new[]{"01  回收泊位","02  储藏区","03  检修间"}[Mathf.Clamp(Mathf.FloorToInt(transform.position.x/12),0,2)]);
            string state=brain.CurrentMode==IntentKind.Investigate?"调查中":brain.CurrentMode==IntentKind.Warn?"警告":brain.BaseMode==IntentKind.Wait?"原地待命":"跟随中";Set(mode,state);Set(charge,echo.Battery.Charge.ToString("F0")+"%"+(echo.Docked?" 充电":""));battery.rectTransform.sizeDelta=new Vector2(296*Mathf.Clamp01(echo.Battery.Charge/100),7);battery.color=echo.Battery.Charge<25?NextUiTheme.Orange:NextUiTheme.Cyan;
            var item=carry.HeldId==null?null:SalvageCatalog.Get(carry.HeldId);Set(held,item==null?"空手":item.Name);Set(details,item==null?"单件上限 8 kg":item.Kilograms+" kg    ·    "+item.Points+" 分    ·    G 放下");
            var focus=item==null?carry.Focus():null;string action="";
            if(item!=null&&delivery.AtDock)action="确认交付 "+item.Name;else if(focus){var target=SalvageCatalog.Get(focus.Id);action=target.Kilograms>8?"超出搬运上限（"+target.Kilograms+" kg）":"拿起 "+target.Name;}
            promptBox.gameObject.SetActive(action.Length>0);Set(prompt,action);reticle.color=action.Length>0?NextUiTheme.Cyan:NextUiTheme.Ink;
            if(lastMessage!=carry.LastMessage){lastMessage=carry.LastMessage;messageUntil=Time.unscaledTime+4;}
            string note=echo.Battery.Charge<25?"Echo 电量偏低 · 返回泊位可充电":Time.unscaledTime<messageUntil?lastMessage:"";Set(message,note??"");message.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(note));message.color=echo.Battery.Charge<25?NextUiTheme.Orange:NextUiTheme.Ink;
        }
        void OnDestroy(){if(Canvas)Destroy(Canvas.gameObject);}
    }
}
