using System.Collections;
using System.IO;
using System.Threading;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.AI.Presentation;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Player;
using EchoStation.Station;
using EchoStation.Story;
using UnityEngine;

namespace EchoStation.AI.GameIntegration
{
 [DefaultExecutionOrder(50),RequireComponent(typeof(StationSession)),DisallowMultipleComponent]
 public sealed class S3DialogueDriver:MonoBehaviour
 {
  private StationSession station;private StationLlmSession session;private LlmFeedbackView view;private StationControls controls;private StationEndingPanel ending;private PlayerInteraction interaction;private MemoryStore observedMemory;private Coroutine startup;
  private bool attached,observedOption,wasRestoring;private string pendingEnding,confirmedEnding;
  public bool Ready=>session!=null;public bool Enabled=>session!=null&&session.Enabled;public StationLlmSession Session=>session;public LlmFeedbackView FeedbackView=>view;
#if UNITY_EDITOR
  private ILocalModelFactory testFactory;private IMonotonicClock testClock;
  internal void ConfigureForTests(ILocalModelFactory factory,IMonotonicClock clock=null)
  {if(session!=null)throw new System.InvalidOperationException("Configure a controlled factory before initialization");testFactory=factory;testClock=clock;}
#endif
  private void OnEnable()
  {if(!Application.isPlaying)return;GameRuntimeOptions.UseLLM=false;observedOption=false;startup=StartCoroutine(AttachWhenReady());}
  private IEnumerator AttachWhenReady()
  {
   // Gives scene Start methods a full frame and permits explicit Editor-only test dependency setup.
   yield return null;station=GetComponent<StationSession>();while(!station.Ready){if(!isActiveAndEnabled)yield break;yield return null;}if(!isActiveAndEnabled)yield break;
   if(session==null)
   {
    ILocalModelFactory factory=new LocalQwen3Factory(Path.Combine(Application.streamingAssetsPath,"EchoLLM","Qwen3_06B_Q4"),new JsonIntentConstraintProvider(),StationScript.CreateSearch());IMonotonicClock clock=null;
#if UNITY_EDITOR
    if(testFactory!=null){factory=testFactory;clock=testClock;}
#endif
    session=new StationLlmSession(station,factory,Thread.CurrentThread.ManagedThreadId,clock);
   }
   view=GetComponent<LlmFeedbackView>();if(!view)view=gameObject.AddComponent<LlmFeedbackView>();view.enabled=true;view.Configure(station.Dialogue,station.Echo.GetComponent<EchoPresentation>());view.CancelRequested+=Cancel;
   controls=GetComponent<StationControls>();ending=GetComponent<StationEndingPanel>();interaction=GetComponent<PlayerInteraction>();var mode=GetComponent<LlmModeToggle>();if(!mode)mode=gameObject.AddComponent<LlmModeToggle>();mode.Configure(controls,this);AttachEvents();pendingEnding=ending.Choice.PendingId;confirmedEnding=ending.Choice.EndingId;wasRestoring=station.Restoring;RefreshFeedback();startup=null;
  }
  public bool SetEnabled(bool value)
  {
   if(session==null||value&&(!isActiveAndEnabled||ending&&ending.HasEnded))return false;session.SetEnabled(value);GameRuntimeOptions.UseLLM=session.Enabled;observedOption=session.Enabled;RefreshFeedback();return true;
  }
  public void Cancel(){if(session==null)return;session.Cancel();GameRuntimeOptions.UseLLM=session.Enabled;observedOption=session.Enabled;RefreshFeedback();}
  private void Update()
  {
   if(session==null)return;
   if(GameRuntimeOptions.UseLLM!=observedOption){SetEnabled(GameRuntimeOptions.UseLLM);GameRuntimeOptions.UseLLM=session.Enabled;observedOption=session.Enabled;}
   if(station.Restoring&&!wasRestoring)session.Invalidate(LifecycleChange.LoadGame);wasRestoring=station.Restoring;
   if(ending&&(pendingEnding!=ending.Choice.PendingId||confirmedEnding!=ending.Choice.EndingId))
   {pendingEnding=ending.Choice.PendingId;confirmedEnding=ending.Choice.EndingId;session.Invalidate(LifecycleChange.Ending);if(ending.HasEnded)SetEnabled(false);}
   session.Poll();
   // Mirror automatic fault downgrade before the next frame can interpret the old true as a reopen.
   GameRuntimeOptions.UseLLM=session.Enabled;observedOption=session.Enabled;RefreshFeedback();
  }
  private void RefreshFeedback(){if(view&&session!=null)view.SetPhase(session.Feedback);}
  private void AttachEvents()
  {
   if(attached)return;attached=true;station.Memories.Replaced+=OnMemoryReplaced;observedMemory=station.Memories.Store;observedMemory.Changed+=OnKnownChanged;station.ZoneChanged+=OnWorldChanged;station.Progress.Completed+=OnWorldChanged;controls.LoadRequested+=OnLoad;controls.NewRequested+=OnNew;ending.TitleRequested+=OnNew;interaction.InformationRead+=OnWorldChanged;
  }
  private void DetachEvents()
  {
   if(!attached)return;attached=false;station.Memories.Replaced-=OnMemoryReplaced;if(observedMemory!=null)observedMemory.Changed-=OnKnownChanged;observedMemory=null;station.ZoneChanged-=OnWorldChanged;station.Progress.Completed-=OnWorldChanged;
   if(controls){controls.LoadRequested-=OnLoad;controls.NewRequested-=OnNew;}if(ending)ending.TitleRequested-=OnNew;if(interaction)interaction.InformationRead-=OnWorldChanged;
  }
  private void OnKnownChanged(MemoryChange change){if(session!=null&&!session.ApplyingTrustedResult)session.Invalidate(LifecycleChange.WorldChanged);}
  private void OnMemoryReplaced()
  {if(observedMemory!=null)observedMemory.Changed-=OnKnownChanged;observedMemory=station.Memories.Store;observedMemory.Changed+=OnKnownChanged;session.Invalidate(LifecycleChange.LoadGame);}
  private void OnWorldChanged(string _) {session?.Invalidate(LifecycleChange.WorldChanged);}
  private void OnLoad(){session?.Invalidate(LifecycleChange.LoadGame);}
  private void OnNew(){session?.Invalidate(LifecycleChange.NewGame);SetEnabled(false);}
  private void OnDisable()
  {
   if(startup!=null){StopCoroutine(startup);startup=null;}DetachEvents();if(view){view.CancelRequested-=Cancel;view.enabled=false;}
   session?.SetEnabled(false);GameRuntimeOptions.UseLLM=false;observedOption=false;
  }
  private void OnDestroy(){session?.Dispose();session=null;}
 }
}
