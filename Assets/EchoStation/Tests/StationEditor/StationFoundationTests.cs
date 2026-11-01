using System;
using System.Collections;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Rendering;
using EchoStation.Station;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Player;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class StationFoundationTests
 {
  private Scene scene,previous;private bool background;private SyntheticInputScope inputs;private StationSession station;
  [Test]public void W01_B01_OwnedSceneHasFiveRegionsActorsPipelineAndIndependentNavigation()
  {
   var old=SceneManager.GetActiveScene();var s=EditorSceneManager.OpenScene(StationLayout.ScenePath,OpenSceneMode.Additive);
   try{var root=s.GetRootGameObjects().Single(r=>r.name=="ES_Station_Greybox");Assert.That(root.transform.Cast<Transform>().Count(t=>t.name.StartsWith("Zone_")),Is.EqualTo(5));
    var surface=root.GetComponent<NavMeshSurface>();Assert.That(AssetDatabase.GetAssetPath(surface.navMeshData),Is.EqualTo(StationLayout.DataFolder+"/StationNavMesh.asset"));
    var player=s.GetRootGameObjects().Single(r=>r.GetComponent<StationSession>());Assert.That(player.GetComponent<CabinDemo>(),Is.Null);Assert.That(player.GetComponent<SaveGameAdapter>(),Is.Null);Assert.That(player.GetComponent<DialoguePanel>(),Is.Not.Null);
    var c=player.GetComponentInChildren<Camera>();Assert.That(c.allowHDR,Is.True);Assert.That(c.GetUniversalAdditionalCameraData().allowHDROutput,Is.False);Assert.That(c.GetComponent<ScanWaveView>(),Is.Not.Null);
   }finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(s,true);}
  }
  private IEnumerator Setup(){background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();inputs=new SyntheticInputScope();scene=EditorSceneManager.LoadSceneInPlayMode(StationLayout.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));yield return Until(()=>scene.isLoaded,6,"Station scene load timeout");SceneManager.SetActiveScene(scene);station=scene.GetRootGameObjects().Single(r=>r.GetComponent<StationSession>()).GetComponent<StationSession>();yield return Until(()=>station.Ready,6,"Station not ready");station.Echo.AutomaticWarningsEnabled=false;}
  private static IEnumerator Until(Func<bool> condition,double seconds,string message){var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){if(timer.Elapsed.TotalSeconds>seconds)Assert.Fail(message);yield return R04ShadowProbe.Frame();}}
  [UnityTest]public IEnumerator W01_B02_ActualBootBindsEchoAndAllZonePathsAreContinuousBothWays()
  {
   yield return new EnterPlayMode();yield return Setup();Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(station.CurrentZone,Is.EqualTo("valley"));Assert.That(station.Knowledge.IsReady,Is.True);Assert.That(station.Memories.Store.Clues,Is.Empty);
   var filter=new NavMeshQueryFilter{agentTypeID=station.Echo.GetComponent<NavMeshAgent>().agentTypeID,areaMask=NavMesh.AllAreas};var start=station.Echo.transform.position;
   var gates=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StationGate>()).ToArray();var obstacles=gates.Select(g=>g.GetComponent<NavMeshObstacle>()).ToArray();var states=obstacles.Select(o=>o.enabled).ToArray();
   bool CoreReachable(){var p=new NavMeshPath();NavMesh.CalculatePath(start,new Vector3(0,-4,67),filter,p);return p.status==NavMeshPathStatus.PathComplete;}
   yield return Until(()=>!CoreReachable(),3,"Initial locked gates did not block core route");Assert.That(gates.All(g=>!g.IsOpen&&g.GetComponent<Collider>().enabled),Is.True);
   try{foreach(var obstacle in obstacles)obstacle.enabled=false;yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();yield return Until(CoreReachable,3,"Underlying topology disconnected with test carving disabled");
    foreach(var zone in StationLayout.Zones){var p=zone.Id=="lab_a"?new Vector3(0,-4,30):zone.Center;Assert.That(NavMesh.SamplePosition(p,out var hit,.6f,filter),Is.True,zone.Id);var path=new NavMeshPath();Assert.That(NavMesh.CalculatePath(start,hit.position,filter,path),Is.True,zone.Id);Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete),zone.Id);Assert.That(NavMesh.CalculatePath(hit.position,start,filter,path),Is.True);Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete));}
   }finally{for(var i=0;i<obstacles.Length;i++)obstacles[i].enabled=states[i];}
   yield return Until(()=>!CoreReachable(),3,"Restored locked gates no longer block route");Assert.That(gates.All(g=>!g.IsOpen&&g.GetComponent<Collider>().enabled),Is.True);
   var before=station.Echo.transform.position;var cc=station.GetComponent<CharacterController>();cc.enabled=false;station.transform.position=new Vector3(0,.05f,-30);cc.enabled=true;yield return Until(()=>Vector3.Distance(before,station.Echo.transform.position)>.2f,5,"Echo did not actually follow");
  }
  [UnityTest]public IEnumerator W01_B03_ZoneMemoryAndTextControlsDoNotCreateCluesOrDeviceChanges()
  {
   yield return new EnterPlayMode();yield return Setup();station.Echo.TrySubmit(ScriptCommands.Wait(),out _);var cc=station.GetComponent<CharacterController>();
   foreach(var z in StationLayout.Zones){cc.enabled=false;station.transform.position=z.Id=="lab_a"?new Vector3(0,-3.95f,30):z.Center+Vector3.up*.05f;cc.enabled=true;station.RefreshZone();Assert.That(station.CurrentZone,Is.EqualTo(z.Id));Assert.That(station.Memories.Store.TryGetCharacter("player_current_zone",out var known),Is.True);Assert.That(known.TargetId,Is.EqualTo(z.Id));}
   Assert.That(station.Memories.Store.Events.Count,Is.EqualTo(5));Assert.That(station.Memories.Store.Clues,Is.Empty);var input=station.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);Assert.That(station.Dialogue.SetOpen(true),Is.True);Assert.That(station.GetComponent<StationControls>().SetOpen(true),Is.False);station.Dialogue.SetOpen(false);Assert.That(station.GetComponent<StationControls>().SetOpen(true),Is.True);Assert.That(station.GetComponent<JournalPanel>().SetOpen(true),Is.False);station.GetComponent<StationControls>().SetOpen(false);
  }
  [UnityTearDown]public IEnumerator TearDown(){if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var op=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;while(op!=null&&!op.isDone)yield return null;Application.runInBackground=background;yield return new ExitPlayMode();}}
 }
}
