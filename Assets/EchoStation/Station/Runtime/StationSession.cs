using System;
using System.Linq;
using EchoStation.Dialogue.Commands;
using EchoStation.Dialogue.Integration;
using EchoStation.Dialogue.Knowledge;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Memory.Integration;
using EchoStation.Player;
using EchoStation.Puzzles;
using EchoStation.Rendering;
using EchoStation.Story;
using EchoStation.World;
using UnityEngine;

namespace EchoStation.Station
{
    [DisallowMultipleComponent]
    public sealed class StationSession:MonoBehaviour
    {
        [SerializeField]private EchoBrain echo;
        public EchoBrain Echo=>echo;
        public FirstPersonMotor Player=>GetComponent<FirstPersonMotor>();
        public PlayerJournal Journal=>GetComponent<PlayerJournal>();
        public DialoguePanel Dialogue=>GetComponent<DialoguePanel>();
        public MemorySession Memories {get;}=new MemorySession();
        public PuzzleProgress Progress {get;}=new PuzzleProgress();
        public JournalMemoryBridge Knowledge {get;private set;}
        public bool Ready {get;private set;}
        public string CurrentZone {get;private set;}
        private double elapsedOffset;
        public double Elapsed=>Math.Max(0,Time.timeSinceLevelLoadAsDouble+elapsedOffset);
        public bool Restoring {get;internal set;}
        internal void RestoreClockAndZone(double elapsed,string zone){elapsedOffset=elapsed-Time.timeSinceLevelLoadAsDouble;CurrentZone=zone;}
        public string Status {get;private set;}="正在初始化观测站。";
        public event Action<string> StatusChanged;
        public event Action<string> ZoneChanged;
        private int frameRate,vsync;
        private bool pacing;
        public void Configure(EchoBrain robot)=>echo=robot;
        private void Start()
        {
            GameRuntimeOptions.UseLLM=false;frameRate=Application.targetFrameRate;vsync=QualitySettings.vSyncCount;pacing=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;
            if(!echo||!echo.GetComponent<EchoNavigation>().TryBind(echo.transform.position)||!echo.Initialize(transform,IntentKind.Follow))throw new InvalidOperationException("S2 Echo startup failed.");
            Journal.BindEcho(echo.GetComponent<EchoScanController>());
            Knowledge=new JournalMemoryBridge(Journal.Store,Memories.Store,summary=>{
                StationClues.TryGet(summary.id,out var c);
                return new ClueMemory(summary.id,summary.title,summary.summary,(MemoryClueKind)(int)summary.kind,c?.Password,c?.KeyMarker);
            });
            var responder=new ScriptKnowledgeResponder(Memories,StationScript.CreateSearch(),StationScript.Hints,new[]{"7316","0428"},()=>Elapsed);
            GetComponent<ScriptDialogueController>().Configure(Dialogue,echo,Memories,Knowledge,AvailableTargets,q=>responder.Answer(q,Dialogue.Session.Version),()=>Elapsed,ProgressAudit.ReservedEvents);
            var view=GetComponentInChildren<Camera>();view.GetComponent<ScanWaveView>().Source=echo.GetComponent<ScanWaveEmitter>();
            Progress.Completed+=OnPuzzleCompleted;echo.Speech+=OnSpeech;Ready=true;RefreshZone();
        }
        public ScriptTarget[] AvailableTargets()=>gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WorldTarget>()).Where(t=>t&&t.isActiveAndEnabled).Select(t=>{
            var p=t.GetComponent<EchoTargetPermissions>();
            var aliases=t.Id=="scan_mark_01"?new[]{"储藏室可疑点","储藏室可疑位置","储物间可疑位置"}:t.Id=="scan_lab_calibration_01"?new[]{"A区校准架可疑点","校准架","A区标记"}:new[]{t.DisplayName};
            return new ScriptTarget(t.Id,t.DisplayName,p&&p.CanInvestigate,p&&p.IsDanger,aliases);
        }).ToArray();
        private void Update(){if(Ready&&!Restoring)RefreshZone();}
        public void RefreshZone()
        {
            var zone=StationLayout.ZoneAt(transform.position);if(zone==null||zone==CurrentZone)return;CurrentZone=zone;
            // A capturing First predicate creates its closure even on the unchanged-zone early return.
            var zoneName=zone;for(var i=0;i<StationLayout.Zones.Count;i++)if(StationLayout.Zones[i].Id==zone){zoneName=StationLayout.Zones[i].Name;break;}
            var state=new CharacterMemory(ScriptKnowledgeResponder.CurrentZoneMemoryId,CharacterMemoryKind.PlayerKnowledge,"玩家位于"+zoneName+"。",zone);
            if(Memories.Store.TryGetCharacter(state.Id,out _))Memories.Store.UpdateCharacter(state);else Memories.Store.AddCharacter(state);
            var first=!Memories.Store.TryGetEvent("visited_"+zone,out _);if(first)Memories.Store.AddEvent("visited_"+zone,Elapsed,"进入"+zoneName+"。",MemoryPriority.Low);
            SayLine(first?"enter_"+zone:"revisit");ZoneChanged?.Invoke(zone);
        }
        public void SayLine(string id)=>echo.Speak(StationScript.KnownLine(id,Memories.Store.CaptureSnapshot()),false);
        internal bool CanRecordPuzzle(string id)=>Memories.Store.TryGetEvent("progress_"+id,out _)||Memories.Store.Events.Count<MemoryLimits.MaximumEvents;
        public ProgressState CaptureProgressState()
        {
            var gates=gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StationGate>()).ToArray();
            var cycle=GetComponent<StationPowerPanel>().Cycle;var z=transform.position.z;
            return new ProgressState{Completed=Progress.Capture(),Journal=Journal.Store.Entries.Select(e=>e.Id).ToArray(),Scans=echo.GetComponent<EchoScanController>().Scanner.Ledger.GetDiscoveredIds(),Events=Memories.Store.Events.Select(e=>e.Id).ToArray(),Zone=StationLayout.ZoneAt(transform.position),AccessLevel=z>58.3f?3:z>38.3f?2:z>5.5f?1:0,CabinOpen=gates.Single(g=>g.GetComponent<WorldTarget>()?.Id=="cabin_door_01").IsOpen,CoreOpen=gates.Single(g=>g.GetComponent<WorldTarget>()?.Id=="core_door_01").IsOpen,VentOpen=gates.Single(g=>g.name=="Vent_Isolation").IsOpen,Powered=cycle.Powered,VentElapsed=cycle.Elapsed};
        }
        private void OnPuzzleCompleted(string id)
        {
            var spec=StationScript.Puzzles.First(p=>p.Id==id);var result=Memories.Store.AddEvent(spec.CompletionId,Elapsed,"玩家完成"+spec.Title+"。",MemoryPriority.Critical);
            if(result!=MemoryWriteResult.Added&&result!=MemoryWriteResult.AlreadyKnown)SetStatus("进度事件写入失败："+result);
        }
        private void OnSpeech(string text,bool refusal)=>SetStatus("Echo："+text);
        public void SetStatus(string text){Status=text??"";StatusChanged?.Invoke(Status);}
        private void OnDestroy(){Ready=false;Progress.Completed-=OnPuzzleCompleted;if(echo){echo.Speech-=OnSpeech;echo.Shutdown();}Knowledge?.Dispose();if(pacing&&Application.isEditor){Application.targetFrameRate=frameRate;QualitySettings.vSyncCount=vsync;}}
    }
}
