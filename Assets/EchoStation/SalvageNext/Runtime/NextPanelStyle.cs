using System.Linq;
using EchoStation.Salvage;
using UnityEngine;
using UnityEngine.UI;
namespace EchoStation.SalvageNext
{
    [DefaultExecutionOrder(220),RequireComponent(typeof(NextHud))]
    public sealed class NextPanelStyle:MonoBehaviour
    {
        NextUiTheme theme;SalvageMission mission;SalvageDelivery delivery;SalvageCarry carry;Text boardProgress,boardStatus,boardNotice;Text[] cargoNames,cargoValues,cargoStates;bool deliveryStyled;float next;
        public static NextUiTheme ThemeFor(Component owner)=>owner.GetComponent<NextHud>()?.Theme;
        void Start()
        {
            theme=GetComponent<NextHud>().Theme;mission=GetComponent<SalvageMission>();delivery=GetComponent<SalvageDelivery>();carry=GetComponent<SalvageCarry>();mission.BoardOpened+=Board;
            foreach(var name in new[]{"EchoCommands","NextDisplay","NextSettingsHUD","NextRoomLoading"}){var node=transform.Find(name);if(node)theme.StyleCanvas(node,name!="NextSettingsHUD");}if(mission.BoardOpen){var board=transform.Find("TaskBoard/Panel") as RectTransform;if(board)Board(board);}
        }
        void Board(RectTransform root)
        {
            if(!root.Find("BoardTitle"))
            {
                root.Find("Text").GetComponent<Text>().enabled=false;
                theme.Label(root,"BoardTitle",28,20,770,44,"回收清单",28,NextUiTheme.Cyan);boardProgress=theme.Label(root,"BoardProgress",28,78,770,40,"",26);
                theme.Label(root,"BoardGoal",28,126,770,34,"交付至少 3 件且累计 12 分后，可主动结算。",22,NextUiTheme.Muted);
                theme.Label(root,"CargoHeader",28,184,380,34,"物品",22,NextUiTheme.Muted);theme.Label(root,"ValueHeader",405,184,220,34,"重量 / 价值",22,NextUiTheme.Muted);theme.Label(root,"StateHeader",650,184,170,34,"状态",22,NextUiTheme.Muted);
                var definitions=SalvageCatalog.Items;cargoNames=new Text[definitions.Count];cargoValues=new Text[definitions.Count];cargoStates=new Text[definitions.Count];for(int i=0;i<definitions.Count;i++){var d=definitions[i];float y=228+i*44;cargoNames[i]=theme.Label(root,"CargoName"+i,28,y,375,34,d.Name,22);cargoValues[i]=theme.Label(root,"CargoValue"+i,405,y,220,34,d.Kilograms+" kg / "+d.Points+" 分",22);cargoStates[i]=theme.Label(root,"CargoState"+i,650,y,170,34,"",22);}
                boardStatus=theme.Label(root,"BoardStatus",28,554,770,64,"",22);boardStatus.horizontalOverflow=HorizontalWrapMode.Wrap;boardNotice=theme.Label(root,"BoardNotice",28,618,770,34,"",20,NextUiTheme.Muted);theme.Label(root,"SaveHints",440,664,380,34,"N 重开   F5 保存 / F9 读档",20,NextUiTheme.Muted);
                theme.StyleCanvas(root.parent);boardProgress.color=NextUiTheme.Cyan;
            }
            RefreshBoard();
        }
        public void RefreshBoard()
        {
            if(!boardProgress)return;boardProgress.text="已交付  "+delivery.Count+" / 3 件     回收价值  "+delivery.Points+" / 12 分";
            for(int i=0;i<SalvageCatalog.Items.Count;i++){var record=carry.Registry.Get(SalvageCatalog.Items[i].Id);cargoStates[i].text=record.State==CargoState.Delivered?"已交付":record.State==CargoState.Held?"手持中":record.Definition.Kilograms>8?"超出上限":"待回收";cargoStates[i].color=record.State==CargoState.Delivered?NextUiTheme.Cyan:record.Definition.Kilograms>8?NextUiTheme.Orange:NextUiTheme.Muted;}
            boardStatus.text=mission.RestartPending?"重新开始会重置本局进度。Enter 确认 / Esc 取消":mission.Completed?"本次回收已完成 · 用时 "+mission.Elapsed.ToString("F1")+" 秒":mission.EndPending?"确认结束本局？Enter 确认 / Esc 取消":mission.CanComplete?"目标已达成。Enter 申请结算 / Esc 继续探索":"尚未达到回收目标。Esc 返回探索。";boardStatus.color=mission.EndPending||mission.RestartPending?NextUiTheme.Orange:NextUiTheme.Ink;boardNotice.text=mission.Notice;
        }
        void LateUpdate()
        {
            if(!theme||Time.unscaledTime<next)return;next=Time.unscaledTime+.15f;if(mission.BoardOpen)RefreshBoard();if(!deliveryStyled&&delivery.Pending){var root=transform.Find("DeliveryConfirmation");if(root){theme.StyleCanvas(root);var text=root.Find("Panel/Text").GetComponent<Text>();text.color=NextUiTheme.Ink;deliveryStyled=true;}}
        }
        void OnDestroy(){if(mission)mission.BoardOpened-=Board;}
    }
}
