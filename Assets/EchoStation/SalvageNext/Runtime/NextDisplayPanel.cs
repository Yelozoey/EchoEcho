using System;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace EchoStation.SalvageNext
{
    [DefaultExecutionOrder(-65),RequireComponent(typeof(NextSession),typeof(NextCommandPanel))]
    public sealed class NextDisplayPanel:MonoBehaviour
    {
        NextSession session;PlayerInputReader input;SalvageMission mission;SalvageDelivery delivery;
        IDisplayBackend backend;DisplaySettingsStore store;DisplayTransaction transaction;DisplaySelection draft;
        Text heading,status,selection;GameObject canvas,hudCanvas;Canvas[] hidden;bool[] hiddenFlags;bool missionEnabled,recapture,closing;string startupMessage="";
        public bool IsOpen{get;private set;}public DisplayTransaction Transaction=>transaction;public DisplaySelection Draft=>draft;public DisplaySettingsStore Store=>store;
        public Button ApplyButton{get;private set;}public Button ConfirmButton{get;private set;}public Button RevertButton{get;private set;}public Button HudButton{get;private set;}
        public Button[] QualityButtons{get;private set;}public Button[] ResolutionButtons{get;private set;}public Button[] ModeButtons{get;private set;}
        void Awake(){Build();}
        void Build()
        {
            if(canvas||!Application.isPlaying)return;session=GetComponent<NextSession>();input=GetComponent<PlayerInputReader>();mission=GetComponent<SalvageMission>();delivery=GetComponent<SalvageDelivery>();backend=new UnityDisplayBackend(session);store=new DisplaySettingsStore(Path.Combine(Application.persistentDataPath,"SalvageNext"));transaction=new DisplayTransaction(backend,store);
            heading=SalvageUi.Panel(transform,"NextDisplay",1020,710,90);canvas=SalvageUi.CanvasOf(heading);canvas.AddComponent<GraphicRaycaster>();var root=(RectTransform)heading.transform.parent;
            heading.text="画质与显示";heading.rectTransform.anchorMin=heading.rectTransform.anchorMax=heading.rectTransform.pivot=new Vector2(0,1);heading.rectTransform.anchoredPosition=new Vector2(28,-20);heading.rectTransform.sizeDelta=new Vector2(650,48);heading.alignment=TextAnchor.UpperLeft;
            SalvageWidgets.Button(root,"Close","返回 [F10 / Esc]",746,20,245,RequestClose);
            SalvageWidgets.Label(SalvageWidgets.Rect("QualityTitle",root,28,90,900,32),"画质",22);QualityButtons=new Button[3];string[] qualities={"低","中","高"};for(int i=0;i<3;i++){int n=i;QualityButtons[i]=SalvageWidgets.Button(root,"Quality"+i,qualities[i],28+i*318,130,300,()=>Choose((RecoveryQuality)n,null,null,null));}
            SalvageWidgets.Label(SalvageWidgets.Rect("ResolutionTitle",root,28,195,900,32),"分辨率",22);int[] widths={1280,1600,1920},heights={720,900,1080};ResolutionButtons=new Button[3];for(int i=0;i<3;i++){int n=i;ResolutionButtons[i]=SalvageWidgets.Button(root,"Resolution"+i,widths[i]+" × "+heights[i],28+i*318,235,300,()=>Choose(null,widths[n],heights[n],null));}
            SalvageWidgets.Label(SalvageWidgets.Rect("ModeTitle",root,28,300,900,32),"窗口模式",22);string[] modes={"窗口","无边框全屏","独占全屏"};ModeButtons=new Button[3];for(int i=0;i<3;i++){int n=i;ModeButtons[i]=SalvageWidgets.Button(root,"Mode"+i,modes[i],28+i*318,340,300,()=>Choose(null,null,null,(DisplayWindowMode)n));}
            selection=SalvageWidgets.Label(SalvageWidgets.Rect("Selection",root,28,410,960,42),"",22);status=SalvageWidgets.Label(SalvageWidgets.Rect("Status",root,28,458,960,85),"",20);
            ApplyButton=SalvageWidgets.Button(root,"Apply","应用",28,552,300,Apply);ConfirmButton=SalvageWidgets.Button(root,"Confirm","保留设置",346,552,300,Confirm);RevertButton=SalvageWidgets.Button(root,"Revert","还原",664,552,300,Revert);
            var ai=SalvageWidgets.Button(root,"UseLLM","UseLLM：关闭 · AI未通过质量门禁，本版脚本协作",28,628,936,()=>{});ai.interactable=false;
            var hud=SalvageUi.Panel(transform,"NextSettingsHUD",230,58,85);hud.text="";hudCanvas=SalvageUi.CanvasOf(hud);hudCanvas.AddComponent<GraphicRaycaster>();var hp=(RectTransform)hud.transform.parent;hp.anchorMin=hp.anchorMax=hp.pivot=Vector2.one;hp.anchoredPosition=new Vector2(-24,-24);HudButton=SalvageWidgets.Button(hp,"Settings","设置 [F10]",8,6,214,()=>Open());
            session.Restored+=AfterRestore;canvas.SetActive(false);
        }
        void Start()
        {if(Application.isEditor)return;if(store.TryRead(out var saved,out startupMessage)){if(!backend.TryApply(saved,out var error))startupMessage=error;}else if(!backend.TryApply(DisplaySelection.Default(),out var error))startupMessage=error;}
        public void ConfigureForTests(IDisplayBackend value,string folder)
        {if(IsOpen||transaction.IsPending)throw new InvalidOperationException("Display busy.");backend=value??throw new ArgumentNullException(nameof(value));store=new DisplaySettingsStore(folder);transaction=new DisplayTransaction(backend,store);}
        void Choose(RecoveryQuality? q,int? width,int? height,DisplayWindowMode? mode){if(!IsOpen||transaction.IsPending)return;draft=new DisplaySelection(q??draft.Quality,width??draft.Width,height??draft.Height,mode??draft.Mode);Refresh();}
        public bool Open()
        {
            Build();if(!canvas||!input.HasApplicationFocus||input.UiBlocked||delivery.Pending||mission.BoardOpen||mission.Completed)return false;
            try{draft=backend.Capture();}catch(Exception e){startupMessage=e.Message;return false;}if(transaction.Phase==DisplayPhase.Failed)transaction=new DisplayTransaction(backend,store);missionEnabled=mission.enabled;mission.enabled=false;hidden=GetComponentsInChildren<Canvas>(true).Where(c=>c.gameObject!=canvas&&c.gameObject!=hudCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay&&c.gameObject.activeInHierarchy).ToArray();hiddenFlags=hidden.Select(c=>c.enabled).ToArray();foreach(var c in hidden)c.enabled=false;
            IsOpen=true;closing=false;canvas.SetActive(true);hudCanvas.SetActive(false);input.SetUiBlocked(true);Refresh();EventSystem.current.SetSelectedGameObject(ApplyButton.gameObject);return true;
        }
        public void Apply(){if(IsOpen&&transaction.Begin(draft,Time.realtimeSinceStartupAsDouble))Refresh();}
        public void Confirm(){if(IsOpen)transaction.Confirm(Time.realtimeSinceStartupAsDouble,input.HasApplicationFocus);Refresh();}
        public void Revert(){transaction.Cancel(Time.realtimeSinceStartupAsDouble);Refresh();}
        public void RequestClose(){if(!IsOpen)return;closing=true;transaction.Cancel(Time.realtimeSinceStartupAsDouble);if(!transaction.IsPending)CloseNow();}
        void CloseNow()
        {if(!IsOpen)return;IsOpen=false;closing=false;canvas.SetActive(false);hudCanvas.SetActive(true);if(mission)mission.enabled=missionEnabled;if(hidden!=null)for(int i=0;i<hidden.Length;i++)if(hidden[i])hidden[i].enabled=hiddenFlags[i];hidden=null;if(input){input.SetUiBlocked(mission&&(mission.BoardOpen||mission.Completed)||delivery&&delivery.Pending);input.SetPointerCaptured(!input.UiBlocked&&input.HasApplicationFocus);recapture=true;}}
        void AfterRestore(){if(IsOpen)RequestClose();}
        void Update()
        {
            if(transaction==null)return;transaction.Tick(Time.realtimeSinceStartupAsDouble,input.HasApplicationFocus);if(IsOpen){if(!input.HasApplicationFocus)closing=true;if(closing&&!transaction.IsPending)CloseNow();else Refresh();}
            var k=Keyboard.current;if(k==null||!input.HasApplicationFocus)return;if(k.f10Key.wasPressedThisFrame){if(IsOpen)RequestClose();else Open();}else if(IsOpen&&k.escapeKey.wasPressedThisFrame)RequestClose();
        }
        void LateUpdate(){if(recapture){recapture=false;if(!IsOpen&&!input.UiBlocked&&input.HasApplicationFocus)input.SetPointerCaptured(true);}}
        void Refresh()
        {
            if(!canvas||draft==null)return;selection.text="当前选择："+new[]{"低","中","高"}[(int)draft.Quality]+" · "+draft.Width+" × "+draft.Height+" · "+new[]{"窗口","无边框全屏","独占全屏"}[(int)draft.Mode];bool idle=!transaction.IsPending&&transaction.Phase!=DisplayPhase.Failed;foreach(var button in QualityButtons.Concat(ResolutionButtons).Concat(ModeButtons))button.interactable=idle;ApplyButton.interactable=idle;ConfirmButton.interactable=transaction.Phase==DisplayPhase.Confirming;RevertButton.interactable=transaction.Phase==DisplayPhase.Applying||transaction.Phase==DisplayPhase.Confirming;status.text=(transaction.Message==""?startupMessage:transaction.Message)+(transaction.Phase==DisplayPhase.Confirming?"  "+Math.Ceiling(transaction.SecondsLeft(Time.realtimeSinceStartupAsDouble))+"秒后还原":"");
        }
        void OnDisable(){if(transaction!=null&&transaction.IsPending){transaction.Cancel(Time.realtimeSinceStartupAsDouble,"面板退出，还原未提交设置。");transaction.Tick(Time.realtimeSinceStartupAsDouble,input&&input.HasApplicationFocus);}CloseNow();}
        void OnDestroy(){if(session)session.Restored-=AfterRestore;}
    }
}
