using System.Collections.Generic;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EchoStation.Salvage
{
    [DefaultExecutionOrder(-70),RequireComponent(typeof(SalvageCommandHub))]
    public sealed class SalvageCommandPanel:MonoBehaviour
    {
        SalvageCommandHub hub;PlayerInputReader input;SalvageMission mission;SalvageDelivery delivery;
        Text heading,status;GameObject canvas;RectTransform panel,targetContent,messageContent;ScrollRect messages;
        readonly List<Button> targets=new List<Button>();readonly List<Text> summaries=new List<Text>();readonly List<Text> rows=new List<Text>();bool dirty=true;
        string[] listed=System.Array.Empty<string>();IntentKind choiceKind=IntentKind.Investigate;string pointedAtOpen;bool recapture;
        public bool IsOpen{get;private set;}public bool TextMode{get;private set;}
        public GameTextInput InputField{get;private set;}public Button FollowButton{get;private set;}public Button WaitButton{get;private set;}public Button InvestigateButton{get;private set;}public Button CancelButton{get;private set;}
        public IReadOnlyList<Button> TargetButtons=>targets;public int DisplayedMessages{get;private set;}
        void Awake(){Build();}
        void Build()
        {
            if(canvas||!Application.isPlaying)return;hub=GetComponent<SalvageCommandHub>();input=GetComponent<PlayerInputReader>();mission=GetComponent<SalvageMission>();delivery=GetComponent<SalvageDelivery>();
            if(!gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<EventSystem>(true)).Any())
            {var system=new GameObject("SalvageEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(system,gameObject.scene);system.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            heading=SalvageUi.Panel(transform,"EchoCommands",1320,850,70);canvas=SalvageUi.CanvasOf(heading);canvas.AddComponent<GraphicRaycaster>();panel=(RectTransform)heading.transform.parent;heading.rectTransform.anchorMin=heading.rectTransform.anchorMax=heading.rectTransform.pivot=new Vector2(0,1);heading.rectTransform.anchoredPosition=new Vector2(28,-22);heading.rectTransform.sizeDelta=new Vector2(1050,44);heading.alignment=TextAnchor.UpperLeft;
            SalvageWidgets.Button(panel,"Close","返回 [Esc]",1100,20,190,()=>Close());
            FollowButton=SalvageWidgets.Button(panel,"Follow","跟随",28,78,180,()=>hub.Select(IntentKind.Follow));
            InvestigateButton=SalvageWidgets.Button(panel,"Investigate","调查准星目标",224,78,220,()=>hub.Select(IntentKind.Investigate,pointedAtOpen));
            WaitButton=SalvageWidgets.Button(panel,"Wait","原地待命",460,78,180,()=>hub.Select(IntentKind.Wait));
            CancelButton=SalvageWidgets.Button(panel,"Cancel","取消当前任务",656,78,220,()=>hub.RequestCancel());
            SalvageWidgets.Button(panel,"TextToggle","文字输入 [T]",892,78,260,()=>Open(true));
            status=SalvageWidgets.Label(SalvageWidgets.Rect("Status",panel,28,138,1260,60),"",21);
            SalvageWidgets.Label(SalvageWidgets.Rect("TargetsHeading",panel,28,205,400,35),"准星与已发现目标",22);
            SalvageWidgets.Scroll(panel,"Targets",28,250,420,490,out targetContent);
            messages=SalvageWidgets.Scroll(panel,"Messages",470,205,820,500,out messageContent);
            var rect=SalvageWidgets.Rect("Input",panel,470,725,650,56);rect.gameObject.AddComponent<Image>().color=new Color(.06f,.12f,.17f);rect.gameObject.SetActive(false);
            var label=SalvageWidgets.Label(SalvageWidgets.Rect("Text",rect,12,8,625,42),"",22);var placeholder=SalvageWidgets.Label(SalvageWidgets.Rect("Placeholder",rect,12,8,625,42),"跟随我 / 调查这个 / 电量",20);placeholder.color=new Color(.4f,.55f,.6f);
            InputField=rect.gameObject.AddComponent<GameTextInput>();InputField.textComponent=label;InputField.placeholder=placeholder;InputField.targetGraphic=rect.GetComponent<Image>();InputField.characterLimit=200;InputField.lineType=UnityEngine.UI.InputField.LineType.SingleLine;InputField.SubmitRequested+=Submit;InputField.CancelRequested+=Close;rect.gameObject.SetActive(true);
            SalvageWidgets.Button(panel,"Send","发送",1138,729,150,Submit);
            SalvageWidgets.Label(SalvageWidgets.Rect("Footer",panel,28,800,1260,35),"Echo只提供调查与信息。物品由你搬运、确认交付；新指令会覆盖旧任务。",19);
            hub.Changed+=Changed;mission.BoardOpened+=AddBoardButton;canvas.SetActive(false);
        }
        void Changed(){dirty=true;}
        void AddBoardButton(RectTransform board){var owner=board.GetComponentInParent<Canvas>(true);if(!owner.GetComponent<GraphicRaycaster>())owner.gameObject.AddComponent<GraphicRaycaster>();if(board.Find("EchoTargets"))return;SalvageWidgets.Button(board,"EchoTargets","选择 Echo 调查目标 [R]",24,660,390,()=>Open(false));}
        void Update()
        {
            if(!input)return;if(!input.HasApplicationFocus){if(IsOpen)Close();return;}
            var keyboard=Keyboard.current;
            if(keyboard!=null&&!TextMode)
            {if(keyboard.rKey.wasPressedThisFrame){if(IsOpen)Close();else Open(false);}else if(keyboard.tKey.wasPressedThisFrame)Open(true);else if(IsOpen&&keyboard.escapeKey.wasPressedThisFrame)Close();}
            if(IsOpen&&dirty)Refresh();
        }
        public bool Open(bool text)
        {
            Build();if(!canvas||!input.HasApplicationFocus||delivery.Pending||mission.Completed)return false;
            if(!IsOpen){pointedAtOpen=hub.PointedId();if(mission.BoardOpen)mission.CloseBoard();else if(input.UiBlocked)return false;}
            TextMode=text;IsOpen=true;input.SetUiBlocked(true);canvas.SetActive(true);InputField.gameObject.SetActive(text);dirty=true;Refresh();
            if(text){InputField.Select();InputField.ActivateInputField();}else EventSystem.current.SetSelectedGameObject(FollowButton.gameObject);return true;
        }
        public void Close(){if(!IsOpen)return;IsOpen=TextMode=false;if(InputField)InputField.text="";if(canvas)canvas.SetActive(false);input.SetUiBlocked(false);input.SetPointerCaptured(input.HasApplicationFocus);recapture=true;}
        void LateUpdate(){if(!recapture)return;recapture=false;if(!IsOpen&&input&&!input.UiBlocked&&input.HasApplicationFocus)input.SetPointerCaptured(true);}
        public void Submit(){if(!IsOpen||!TextMode||!input.HasApplicationFocus||InputField.IsComposing)return;if(hub.Submit(InputField.text)){InputField.text="";InputField.ActivateInputField();}dirty=true;}
        void Refresh()
        {
            heading.text=TextMode?"Echo 通信 · 脚本指令":"Echo 协作";var reply=hub.LastReply;status.text=reply?.Speech??"指向可见目标后调查；已发现目标可直接选择。";
            var context=hub.Context(pointedAtOpen);listed=reply!=null&&reply.Kind==ScriptReplyKind.Clarify?reply.Choices.Where(context.CanSelect).ToArray():context.Choices.Union(hub.Known.Snapshot()).ToArray();choiceKind=reply?.ChoiceIntent??IntentKind.Investigate;
            for(int i=0;i<listed.Length;i++){int index=i;if(i==targets.Count){targets.Add(SalvageWidgets.Button(targetContent,"Target_"+i,"",8,i*112+8,400,()=>Choose(index)));summaries.Add(SalvageWidgets.Label(SalvageWidgets.Rect("Summary_"+i,targetContent,16,i*112+58,384,52),"",17));}var button=targets[i];button.gameObject.SetActive(true);button.interactable=context.CanSelect(listed[i]);var d=SalvageTargetCatalog.Items.Single(t=>t.Id==listed[i]);button.GetComponentInChildren<Text>().text=d.Name+(!button.interactable?" · 不可调查":hub.Known.Contains(d.Id)?" · 已发现":" · 准星");summaries[i].gameObject.SetActive(true);summaries[i].text=hub.Known.Summary(d.Id)??"尚无扫描记录";}
            for(int i=listed.Length;i<targets.Count;i++){targets[i].gameObject.SetActive(false);summaries[i].gameObject.SetActive(false);}targetContent.sizeDelta=new Vector2(0,Mathf.Max(490,listed.Length*112+16));
            float offset=12;for(int i=0;i<hub.History.Messages.Count;i++){if(i==rows.Count)rows.Add(SalvageWidgets.Label(SalvageWidgets.Rect("Message_"+i,messageContent,14,offset,788,70),"",21));var row=rows[i];row.gameObject.SetActive(true);var m=hub.History.Messages[i];row.text=(m.Speaker==EchoStation.Dialogue.DialogueSpeaker.Player?"你：":"Echo：")+m.Text;row.rectTransform.anchoredPosition=new Vector2(14,-offset);float height=Mathf.Max(36,row.preferredHeight);row.rectTransform.sizeDelta=new Vector2(788,height);offset+=height+18;}
            for(int i=hub.History.Messages.Count;i<rows.Count;i++)rows[i].gameObject.SetActive(false);messageContent.sizeDelta=new Vector2(0,Mathf.Max(500,offset));DisplayedMessages=hub.History.Messages.Count;Canvas.ForceUpdateCanvases();messages.verticalNormalizedPosition=0;dirty=false;
        }
        void Choose(int index){if(index<listed.Length)hub.Select(choiceKind,listed[index]);}
        void OnDisable(){Close();}
        void OnDestroy(){if(hub)hub.Changed-=Changed;if(mission)mission.BoardOpened-=AddBoardButton;}
    }
}
