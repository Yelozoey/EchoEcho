using System;
using System.Collections.Generic;
using EchoStation.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoStation.Player
{
    [DefaultExecutionOrder(-80),RequireComponent(typeof(PlayerJournal),typeof(PlayerInputReader)),DisallowMultipleComponent]
    public sealed class JournalPanel:MonoBehaviour
    {
        private sealed class Row{public Button button;public Text caption;public string id;}
        private Canvas canvas;private GameObject dialog,ownedEvents;private RectTransform listContent;private ScrollRect listScroll,bodyScroll;private Text title,body,count,toast;private PlayerJournal journal;private PlayerInputReader input;private bool subscribed,dirty=true;private float toastSeconds;private string selectedId;private ClueKind? filter;
        private readonly List<JournalEntry> visible=new List<JournalEntry>();private readonly List<Row> rows=new List<Row>();private readonly Dictionary<int,Button> filters=new Dictionary<int,Button>();
        public bool IsOpen{get;private set;}
        public Canvas Canvas=>canvas;
        public int VisibleCount=>visible.Count;
        public string SelectedId=>selectedId;
        public string DisplayedFullText=>body?body.text:"";
        public string DisplayedTitle=>title?title.text:"";
        public ClueKind? Filter=>filter;
        public ScrollRect BodyScroll=>bodyScroll;
        public int AllocatedRows=>rows.Count;
        private void Awake(){if(Application.isPlaying)Build();}
        private void Build()
        {
            if(canvas)return;journal=GetComponent<PlayerJournal>();input=GetComponent<PlayerInputReader>();canvas=RuntimeUi.CreateCanvas("ES_JournalOverlay",gameObject.scene,100);
            if(!EventSystem.current){ownedEvents=new GameObject("ES_UIEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));SceneManager.MoveGameObjectToScene(ownedEvents,gameObject.scene);ownedEvents.GetComponent<EventSystem>().sendNavigationEvents=false;}
            var shade=RuntimeUi.Fill("Dialog",canvas.transform,Vector2.zero);RuntimeUi.Image(shade,new Color(0,0,0,.75f));dialog=shade.gameObject;var panel=RuntimeUi.Rect("Panel",shade,new Vector2(1440,800),Vector2.zero,new Vector2(.5f,.5f),new Vector2(.5f,.5f));RuntimeUi.Image(panel,new Color(.025f,.045f,.07f,.98f));
            RuntimeUi.Label(RuntimeUi.Rect("Heading",panel,new Vector2(600,45),new Vector2(40,-28),new Vector2(0,1),new Vector2(0,1)),28).text="观测日志";
            count=RuntimeUi.Label(RuntimeUi.Rect("Count",panel,new Vector2(300,40),new Vector2(670,-30),new Vector2(0,1),new Vector2(0,1)),18);
            RuntimeUi.Button(RuntimeUi.Rect("Close",panel,new Vector2(200,42),new Vector2(-40,-30),new Vector2(1,1),new Vector2(1,1)),"关闭  [J / Esc]",()=>SetOpen(false));
            for(var i=-1;i<3;i++){var key=i;var caption=i<0?"全部":KindLabel((ClueKind)i);filters[key]=RuntimeUi.Button(RuntimeUi.Rect("Filter_"+i,panel,new Vector2(100,36),new Vector2(40+(i+1)*110,-92),new Vector2(0,1),new Vector2(0,1)),caption,()=>SetFilter(key<0?(ClueKind?)null:(ClueKind)key));}
            listScroll=RuntimeUi.ScrollViewport("EntryList",panel,new Vector2(40,-150),new Vector2(360,610),out listContent);
            title=RuntimeUi.Label(RuntimeUi.Rect("EntryTitle",panel,new Vector2(960,40),new Vector2(440,-145),new Vector2(0,1),new Vector2(0,1)),24);
            bodyScroll=RuntimeUi.ScrollViewport("ReadingArea",panel,new Vector2(440,-195),new Vector2(960,565),out var content);body=RuntimeUi.Label(content,20,TextAnchor.UpperLeft);
            var readingHint=RuntimeUi.Label(RuntimeUi.Rect("ReadingHint",panel,new Vector2(960,24),new Vector2(440,-766),new Vector2(0,1),new Vector2(0,1)),16);readingHint.text="滚轮阅读长记录";readingHint.color=new Color(.5f,.68f,.76f);
            toast=RuntimeUi.Label(RuntimeUi.Rect("ArchiveNotice",canvas.transform,new Vector2(1000,40),new Vector2(40,-45),new Vector2(0,1),new Vector2(0,1)),20);toast.enabled=false;dialog.SetActive(false);
        }
        private void OnEnable()
        {
            if(!Application.isPlaying)return;Build();canvas.gameObject.SetActive(true);if(!subscribed){journal.Store.Added+=OnAdded;journal.Store.Replaced+=OnReplaced;subscribed=true;}dirty=true;
        }
        private void OnAdded(JournalEntry entry){dirty=true;toast.text="已归档："+entry.Title+"    [J] 查看";toastSeconds=3;}
        private void OnReplaced(){dirty=true;selectedId=null;toastSeconds=0;}
        private void Update()
        {
            if(input.JournalPressed)SetOpen(!IsOpen);if(IsOpen&&input.CancelPressed)SetOpen(false);toastSeconds=Mathf.Max(0,toastSeconds-Time.unscaledDeltaTime);toast.enabled=toastSeconds>0&&!IsOpen;if(IsOpen&&dirty)RefreshEntries();
        }
        public bool SetOpen(bool value)
        {
            Build();if(value==IsOpen)return true;if(value&&(!isActiveAndEnabled||!input.HasApplicationFocus||input.UiBlocked))return false;IsOpen=value;dialog.SetActive(value);input.SetUiBlocked(value);if(value){dirty=true;RefreshEntries();if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);}return true;
        }
        public void SetFilter(ClueKind? value)
        {
            if(value.HasValue&&(value.Value<ClueKind.Note||value.Value>ClueKind.Scan))throw new ArgumentException("Invalid journal category.");filter=value;dirty=true;if(IsOpen)RefreshEntries();
        }
        public Button FilterButton(ClueKind? value)=>filters[value.HasValue?(int)value.Value:-1];
        public Button EntryButton(string id){foreach(var row in rows)if(row.button.gameObject.activeSelf&&row.id==id)return row.button;return null;}
        private void RefreshEntries()
        {
            visible.Clear();foreach(var entry in journal.Store.Entries)if(!filter.HasValue||entry.Kind==filter.Value)visible.Add(entry);var selectedBefore=selectedId;var selectedExists=false;foreach(var entry in visible)if(entry.Id==selectedId)selectedExists=true;if(!selectedExists)selectedId=visible.Count>0?visible[visible.Count-1].Id:null;
            for(var i=0;i<visible.Count;i++)
            {
                if(i>=rows.Count){var row=new Row();row.button=RuntimeUi.Button(RuntimeUi.Rect("Entry_"+i,listContent,new Vector2(340,44),new Vector2(10,-i*52),new Vector2(0,1),new Vector2(0,1)),"",()=>SelectEntry(row.id));row.caption=row.button.GetComponentInChildren<Text>();row.caption.alignment=TextAnchor.MiddleLeft;rows.Add(row);}var item=rows[i];item.id=visible[i].Id;item.caption.text=KindLabel(visible[i].Kind)+"  "+visible[i].Title;item.button.gameObject.SetActive(true);
            }
            for(var i=visible.Count;i<rows.Count;i++)rows[i].button.gameObject.SetActive(false);listContent.sizeDelta=new Vector2(0,Mathf.Max(listScroll.viewport.rect.height,visible.Count*52));foreach(var pair in filters)pair.Value.GetComponent<Image>().color=pair.Key==(filter.HasValue?(int)filter.Value:-1)?RuntimeUi.SelectedColor:RuntimeUi.ButtonColor;count.text=journal.Store.Entries.Count+" 条记录";dirty=false;RefreshBody(selectedBefore!=selectedId);
        }
        public bool SelectEntry(string id)
        {
            foreach(var entry in visible)if(entry.Id==id){selectedId=id;RefreshBody(true);return true;}return false;
        }
        private void RefreshBody(bool resetScroll)
        {
            if(selectedId!=null&&journal.Store.TryGet(selectedId,out var entry)){title.text=entry.Title;body.text=entry.FullText;}else{title.text="观测记录";body.text="尚无此分类的记录。";}UnityEngine.Canvas.ForceUpdateCanvases();body.rectTransform.sizeDelta=new Vector2(0,Mathf.Max(bodyScroll.viewport.rect.height,body.preferredHeight+20));if(resetScroll)bodyScroll.verticalNormalizedPosition=1;foreach(var row in rows)if(row.button.gameObject.activeSelf)row.button.GetComponent<Image>().color=row.id==selectedId?RuntimeUi.SelectedColor:RuntimeUi.ButtonColor;
        }
        private static string KindLabel(ClueKind kind)=>kind==ClueKind.Note?"笔记":kind==ClueKind.Data?"数据":"扫描";
        private void OnDisable()
        {
            if(subscribed&&journal){journal.Store.Added-=OnAdded;journal.Store.Replaced-=OnReplaced;subscribed=false;}if(IsOpen)SetOpen(false);if(canvas)canvas.gameObject.SetActive(false);
        }
        private void OnDestroy(){if(canvas)Destroy(canvas.gameObject);if(ownedEvents)Destroy(ownedEvents);}
    }
}
