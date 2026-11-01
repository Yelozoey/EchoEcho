using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.SalvageExpansion;
using EchoStation.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EchoStation.L01
{
    [DefaultExecutionOrder(70),RequireComponent(typeof(ExpansionWorld),typeof(ExpansionEcho))]
    public sealed class ExpansionDialoguePanel:MonoBehaviour
    {
        sealed class Line{public string Speaker,Text;}
        sealed class Row{public RectTransform Root;public Text Body;}
        ExpansionWorld world;ExpansionEcho echo;ExpansionPersistence persistence;IDisposable lease;
        GameObject canvasRoot,hint;RectTransform targetContent,messageContent;Text status,context,notice;ScrollRect scroll;
        readonly List<Line> history=new List<Line>();readonly List<Row> rows=new List<Row>();readonly List<GameObject> targetRows=new List<GameObject>();
        readonly RaycastHit[] hits=new RaycastHit[64];EchoTextResult pending;string pointed;bool initialized,recapture,subscribed;
        public bool IsOpen{get;private set;}public GameTextInput InputField{get;private set;}public Button SendButton{get;private set;}public Canvas Canvas=>canvasRoot?canvasRoot.GetComponent<Canvas>():null;
        public int MessageCount=>history.Count;public int AllocatedRows=>rows.Count;public EchoTextResult LastResult{get;private set;}public string LastReply{get;private set;}="";
        public bool AwaitingTarget=>pending!=null;public string CaptionOverride{get;set;}public string ModeCaption=>CaptionOverride??"脚本模式 · LLM未接入";
        public Func<string,EchoTextContext,string,bool> QuestionInterceptor{get;set;}
        public Func<string,bool> TargetClickInterceptor{get;set;}
        public string TargetActionLabel{get;set;}="调查";
        public event Action Closed;
        public EchoTextContext KnownContext=>Snapshot();public string PointedId=>pointed;
        public void SetNotice(string text){if(notice)notice.text=text;}
        public void RefreshExtensionView(){if(initialized)RenderTargets();}
        public void AddExternalReply(string speaker,string text){LastReply=EchoTextRouter.LimitReply(text);Append(speaker,LastReply);}
        public IReadOnlyList<string> MessageTexts=>history.Select(l=>l.Text).ToArray();
        void Start()=>Initialize();
        public void Initialize()
        {
            if(initialized)return;world=GetComponent<ExpansionWorld>();echo=GetComponent<ExpansionEcho>();persistence=GetComponent<ExpansionPersistence>();
            world.Initialize();echo.Initialize();Build();echo.TextCommunicationRequested+=OpenFromMenu;persistence.Restored+=AfterRestore;subscribed=true;initialized=true;
        }
        void OpenFromMenu(){Open();}
        void EnsureEventSystem()
        {if(EventSystem.current)return;var es=new GameObject("L01_EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));es.transform.SetParent(world.transform,false);es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
        void Build()
        {
            EnsureEventSystem();var title=SalvageUi.Panel(transform,"L01_Communication",1320,860,125);canvasRoot=SalvageUi.CanvasOf(title);canvasRoot.AddComponent<GraphicRaycaster>();var panel=(RectTransform)title.transform.parent;
            title.rectTransform.anchorMin=title.rectTransform.anchorMax=title.rectTransform.pivot=new Vector2(0,1);title.rectTransform.anchoredPosition=new Vector2(28,-20);title.rectTransform.sizeDelta=new Vector2(940,44);title.alignment=TextAnchor.UpperLeft;title.text="Echo 通信";
            status=SalvageWidgets.Label(SalvageWidgets.Rect("Mode",panel,28,68,1230,38),ModeCaption,22);
            SalvageWidgets.Button(panel,"Close","返回探索 [Esc]",1060,20,230,Close);
            context=SalvageWidgets.Label(SalvageWidgets.Rect("Context",panel,28,110,1250,44),"",20);
            SalvageWidgets.Label(SalvageWidgets.Rect("TargetsTitle",panel,28,166,340,36),"已发现目标",22);
            SalvageWidgets.Scroll(panel,"Targets",28,208,365,408,out targetContent);
            scroll=SalvageWidgets.Scroll(panel,"Messages",415,166,875,450,out messageContent);
            notice=SalvageWidgets.Label(SalvageWidgets.Rect("Notice",panel,28,632,1260,60),"",20);
            var inputRect=SalvageWidgets.Rect("Input",panel,28,706,1090,68);inputRect.gameObject.AddComponent<Image>().color=new Color(.065f,.12f,.17f);inputRect.gameObject.SetActive(false);
            var text=SalvageWidgets.Label(SalvageWidgets.Rect("Text",inputRect,16,12,1058,50),"",23);var placeholder=SalvageWidgets.Label(SalvageWidgets.Rect("Placeholder",inputRect,16,12,1058,50),"跟随我 / 原地待命 / 调查这个 / 电量 / 报告＋目标",20);placeholder.color=new Color(.43f,.59f,.66f);
            InputField=inputRect.gameObject.AddComponent<GameTextInput>();InputField.textComponent=text;InputField.placeholder=placeholder;InputField.targetGraphic=inputRect.GetComponent<Image>();InputField.characterLimit=200;InputField.lineType=UnityEngine.UI.InputField.LineType.SingleLine;InputField.SubmitRequested+=SubmitInput;InputField.CancelRequested+=Close;inputRect.gameObject.SetActive(true);
            SendButton=SalvageWidgets.Button(panel,"Send","发送 [Enter]",1134,712,156,SubmitInput);
            SalvageWidgets.Label(SalvageWidgets.Rect("Footer",panel,28,798,1260,40),"文字输入时暂停。跟随、待命、调查和取消会返回探索；信息查询保留面板。",18);
            canvasRoot.SetActive(false);
            var label=SalvageUi.Panel(transform,"L01_CommunicationHint",480,48,11);hint=SalvageUi.CanvasOf(label);var rect=(RectTransform)label.transform.parent;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-20,20);label.rectTransform.offsetMin=new Vector2(10,3);label.rectTransform.offsetMax=new Vector2(-10,-3);label.fontSize=18;label.alignment=TextAnchor.MiddleCenter;label.text="T Echo通信 · 脚本模式";
        }
        public bool Open()
        {
            Initialize();if(IsOpen)return true;if(!isActiveAndEnabled||!world.ActionsAllowed||world.Pause.Blocked||!world.Input.HasApplicationFocus||echo.MenuOpen)return false;
            pointed=PointedKnownTarget();pending=null;lease=world.Pause.Acquire();world.Input.SetPointerCaptured(false);IsOpen=true;canvasRoot.SetActive(true);InputField.text="";
            notice.text="信息来自已发现目标与已有扫描报告。";RenderTargets();RenderMessages();FocusInput();return true;
        }
        string PointedKnownTarget()
        {
            if(!world.Motor.View)return null;int count=Physics.RaycastNonAlloc(world.Motor.View.position,world.Motor.View.forward,hits,12,~0,QueryTriggerInteraction.Ignore);if(count==hits.Length)return null;
            Collider nearest=null;float distance=float.PositiveInfinity;for(int i=0;i<count;i++){var c=hits[i].collider;if(c.transform.IsChildOf(world.transform)||c.transform.IsChildOf(echo.Robot))continue;if(hits[i].distance<distance){nearest=c;distance=hits[i].distance;}}
            var target=nearest?nearest.GetComponentInParent<WorldTarget>():null;if(!target)return null;echo.Discover(target);return echo.KnownTargetsSnapshot().Any(t=>t.Id==target.Id)?target.Id:null;
        }
        EchoTextContext Snapshot()
        {
            var targets=echo.KnownTargetsSnapshot().Select(t=>{bool found=echo.TryReadKnownReport(t.Id,out var report,out var stale);return new EchoTextTarget(t.Id,t.DisplayName,t.isActiveAndEnabled,found?report:null,stale);});
            return new EchoTextContext(targets,pointed,Mathf.Clamp(Mathf.RoundToInt(echo.Battery.Charge),0,100));
        }
        void FocusInput(){if(IsOpen){InputField.Select();InputField.ActivateInputField();}}
        void SubmitInput(){SubmitText(InputField.text);}
        public bool SubmitText(string value)
        {
            if(!IsOpen||!world.Input.HasApplicationFocus||InputField.IsComposing)return false;
            if(string.IsNullOrWhiteSpace(value)||value.Length>200){notice.text="请输入1–200个字符。";FocusInput();return false;}
            pending=null;Append("你",value);InputField.text="";var parsed=EchoTextRouter.Parse(value,Snapshot());
            if(parsed.Kind==EchoTextKind.Reject&&QuestionInterceptor!=null&&QuestionInterceptor(value,Snapshot(),pointed)){FocusInput();return true;}
            Apply(parsed);return true;
        }
        public bool ChooseTarget(string id)
        {
            if(!IsOpen||!world.Input.HasApplicationFocus||pending==null||!pending.Choices.Contains(id))return false;
            var request=pending;pending=null;var fresh=Snapshot();var target=fresh.Targets.FirstOrDefault(t=>t.Id==id);Append("你","选择："+(target?.Name??"目标已失效"));Apply(EchoTextRouter.Select(request.ChoiceKind,id,fresh));return true;
        }
        void Apply(EchoTextResult result)
        {
            LastResult=result;bool behavior=result.Kind==EchoTextKind.Follow||result.Kind==EchoTextKind.Wait||result.Kind==EchoTextKind.Cancel||result.Kind==EchoTextKind.Investigate;
            if(behavior)
            {
                Close();bool accepted=false;if(world.ActionsAllowed&&!world.Pause.Blocked&&world.Input.HasApplicationFocus)
                {if(result.Kind==EchoTextKind.Cancel){echo.CancelTask();accepted=true;}else accepted=echo.Command(result.Kind==EchoTextKind.Follow?ScriptCommands.Follow():result.Kind==EchoTextKind.Wait?ScriptCommands.Wait():ScriptCommands.Investigate(result.TargetId));}
                LastReply=accepted?result.Text:"当前状态不能执行该指令，请返回探索后重试。";Append("Echo（脚本）",LastReply);world.Say("Echo："+LastReply);return;
            }
            pending=result.Kind==EchoTextKind.Clarify?result:null;LastReply=result.Text;Append("Echo（脚本）",LastReply);notice.text=result.Kind==EchoTextKind.Clarify?"请从左侧明确选择目标，或输入一条新指令。":"信息查询不会替你操作设备。";RenderTargets();FocusInput();
        }
        void Append(string speaker,string text)
        {history.Add(new Line{Speaker=speaker,Text=text});if(history.Count>100)history.RemoveAt(0);RenderMessages();}
        void RenderMessages()
        {
            if(!messageContent)return;float y=8;
            for(int i=0;i<history.Count;i++)
            {if(i==rows.Count){var rect=SalvageWidgets.Rect("Message_"+i,messageContent,12,0,840,60);var body=SalvageWidgets.Label(rect,"",21);body.verticalOverflow=VerticalWrapMode.Truncate;rows.Add(new Row{Root=rect,Body=body});}var row=rows[i];row.Root.gameObject.SetActive(true);row.Body.text=history[i].Speaker+"：\n"+history[i].Text;float height=Mathf.Max(72,row.Body.preferredHeight+14);row.Root.sizeDelta=new Vector2(840,height);row.Root.anchoredPosition=new Vector2(12,-y);y+=height+12;}
            for(int i=history.Count;i<rows.Count;i++)rows[i].Root.gameObject.SetActive(false);messageContent.sizeDelta=new Vector2(0,y+8);UnityEngine.Canvas.ForceUpdateCanvases();if(scroll)scroll.verticalNormalizedPosition=0;
        }
        void RenderTargets()
        {
            foreach(var row in targetRows)if(row){row.SetActive(false);Destroy(row);}targetRows.Clear();var snapshot=Snapshot();var pointedTarget=snapshot.Targets.FirstOrDefault(t=>t.Id==pointed);
            context.text="电量 "+snapshot.Battery+"%   准星："+(pointedTarget?.Name??"无已发现目标");status.text=ModeCaption;
            var choices=pending==null?snapshot.Targets:snapshot.Targets.Where(t=>pending.Choices.Contains(t.Id)).ToArray();float y=8;
            foreach(var target in choices)
            {
                string id=target.Id;var name=target.Name+(snapshot.Targets.Count(t=>t.Name==target.Name)>1?" ["+target.Id+"]":"");
                var button=SalvageWidgets.Button(targetContent,"Target_"+id,(pending==null?TargetActionLabel+" ":"选择 ")+name,8,y,340,()=>TargetButton(id));targetRows.Add(button.gameObject);y+=54;
            }
            if(targetRows.Count==0){var label=SalvageWidgets.Label(SalvageWidgets.Rect("NoTargets",targetContent,12,8,330,120),"尚无可选目标。\n请返回探索，靠近设备并观察。",21);targetRows.Add(label.gameObject);y=140;}
            targetContent.sizeDelta=new Vector2(0,y);
        }
        void TargetButton(string id)
        {if(pending!=null){ChooseTarget(id);return;}if(!IsOpen||!world.Input.HasApplicationFocus)return;if(TargetClickInterceptor!=null&&TargetClickInterceptor(id))return;var fresh=Snapshot();var target=fresh.Targets.FirstOrDefault(t=>t.Id==id);if(target==null)return;Append("你","调查 "+target.Name);Apply(EchoTextRouter.Select(EchoTextKind.Investigate,id,fresh));}
        public void Close()
        {if(!IsOpen)return;IsOpen=false;pending=null;if(InputField)InputField.text="";if(canvasRoot)canvasRoot.SetActive(false);lease?.Dispose();lease=null;recapture=true;Closed?.Invoke();}
        void AfterRestore(){Close();pointed=null;pending=null;}
        void Update()
        {
            if(!initialized)return;if(hint)hint.SetActive(world.ActionsAllowed&&!world.Pause.Blocked&&world.Input.HasApplicationFocus);
            if(IsOpen){status.text=ModeCaption;if(!world.Input.HasApplicationFocus||!world.ActionsAllowed)Close();return;}
            if(Keyboard.current!=null&&Keyboard.current.tKey.wasPressedThisFrame&&world.Input.CanPlay)Open();
        }
        void LateUpdate(){if(recapture){recapture=false;if(world)world.Pause.RestorePointer();}}
        void OnDisable(){Close();}
        void OnDestroy(){Close();if(subscribed){if(echo)echo.TextCommunicationRequested-=OpenFromMenu;if(persistence)persistence.Restored-=AfterRestore;}if(canvasRoot)Destroy(canvasRoot);if(hint)Destroy(hint);}
    }
}
