using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.SalvageNext.Editor;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextArtV2StreamingTests
    {
        Scene previous,main;bool background;int bundles;NextRoomStreaming stream;NextSession session;SalvageMotor motor;SalvageEcho echo;SalvageEchoCoordinator brain;PlayerInputReader input;RoomSceneOwner surviving;
        static IEnumerator Wait(Func<bool> condition,string stage){var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(15),stage);yield return null;}}
        IEnumerator Boot()
        {
            background=Application.runInBackground;Application.runInBackground=true;bundles=AssetBundle.GetAllLoadedAssetBundles().Count();previous=SceneManager.GetActiveScene();main=EditorSceneManager.LoadSceneInPlayMode(NextArtV2Stream.Main,new LoadSceneParameters(LoadSceneMode.Additive));while(!main.isLoaded)yield return null;SceneManager.SetActiveScene(main);
            var root=main.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");stream=root.GetComponentInChildren<NextRoomStreaming>();session=stream.GetComponent<NextSession>();motor=stream.GetComponent<SalvageMotor>();input=stream.GetComponent<PlayerInputReader>();echo=root.GetComponentInChildren<SalvageEcho>();brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;brain.AutomaticWarnings=false;Assert.That(brain.Initialize(),Is.True);brain.ResetBase(IntentKind.Wait);motor.enabled=false;
            Assert.That(stream.Owner,Is.Not.Null);Assert.That(stream.Blocking,Is.True,"Bootstrap before async content completion");Assert.That(input.enabled,Is.False);
            yield return Wait(()=>stream.Owner.IsReady(0)&&stream.Owner.IsReady(1)&&!stream.Blocking,"Dock and storage bootstrap");Assert.That(stream.Owner.Phase(2),Is.EqualTo(RoomScenePhase.Empty));input.SetApplicationFocus(true);input.SetPointerCaptured(true);Assert.That(input.CanPlay,Is.True);
        }
        void Place(float player,float robot)
        {motor.RestorePose(new Vector3(player,.1f,12),90,0);Assert.That(echo.Navigation.Agent.Warp(new Vector3(robot,0,12)),Is.True);stream.EvaluateNow();}
        [UnityTest,Timeout(180000)]public IEnumerator ActualDistanceLoadingPreservesMenuBlocksAndUnloadsFarRoomWithNativeCleanup()
        {
            yield return new EnterPlayMode();yield return Boot();var map=stream.Room(0).ColorMap(0);var mesh=stream.Room(0).Renderer(0).GetComponent<MeshFilter>().sharedMesh;var timer=System.Diagnostics.Stopwatch.StartNew();Place(34,33);Assert.That(stream.Policy.Wanted(2),Is.True);Assert.That(stream.Policy.Wanted(0),Is.False);Assert.That(stream.Blocking,Is.True);Assert.That(input.enabled,Is.False);input.SetUiBlocked(true);
            yield return Wait(()=>stream.Owner.IsReady(2)&&!stream.Blocking,"Maintenance ready");double load=timer.Elapsed.TotalSeconds;Assert.That(input.enabled,Is.True);Assert.That(input.UiBlocked,Is.True);Assert.That(input.PointerCaptured,Is.False);yield return Wait(()=>stream.Owner.Phase(0)==RoomScenePhase.Empty&&map==null&&mesh==null,"Far room native resources gone without fixture cleanup");Assert.That(stream.Room(2).gameObject.activeSelf,Is.True);input.SetUiBlocked(false);input.SetPointerCaptured(true);
            Place(5,3);Assert.That(stream.Blocking,Is.True);yield return Wait(()=>stream.Owner.IsReady(0)&&!stream.Blocking,"Return dock");Assert.That(input.CanPlay,Is.True);Assert.That(stream.Owner.Failures,Is.Zero,stream.Owner.LastError);Assert.That(load,Is.LessThan(10d/3.5d),"Measured preload buffer 10m must exceed actual local room load");Directory.CreateDirectory(NextArtV2Stream.Evidence);File.WriteAllText(NextArtV2Stream.Evidence+"/Distance_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"loadSeconds",load},{"preloadBufferSeconds",10d/3.5d},{"menuBlockPreserved",true},{"nativeMapMeshReleased",true},{"source","Real BundledSceneProvider"},{"failures",stream.Owner.Failures},{"worldEpoch",session.Epoch}}.ToString());
        }
        [UnityTest,Timeout(180000)]public IEnumerator RestoreImmediatelyRevokesCurrentWorldAndSceneExitReleasesLateLoads()
        {
            yield return new EnterPlayMode();yield return Boot();var snapshot=session.Capture();long epoch=session.Epoch;int commits=stream.Owner.Commits;Assert.That(session.TryRestore(snapshot,out var error),Is.True,error);Assert.That(session.Epoch,Is.GreaterThan(epoch));Assert.That(stream.Owner.IsReady(0),Is.False,"Readiness revoked within restore callback");Assert.That(stream.Blocking,Is.True);Assert.That(input.enabled,Is.False);yield return Wait(()=>stream.Owner.IsReady(0)&&stream.Owner.IsReady(1)&&!stream.Blocking,"Restored world ready");Assert.That(stream.Owner.Commits,Is.GreaterThan(commits));
            Place(34,33);surviving=stream.Owner;surviving.Poll();Assert.That(surviving.Phase(2),Is.EqualTo(RoomScenePhase.Loading));SceneManager.SetActiveScene(previous);var unload=SceneManager.UnloadSceneAsync(main);while(!unload.isDone)yield return null;main=default;yield return Wait(()=>surviving.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0,"Exit late native cleanup");Assert.That(surviving.LiveHandles,Is.Zero);Assert.That(surviving.Failures,Is.Zero,surviving.LastError);Assert.That(AssetBundle.GetAllLoadedAssetBundles().Count(),Is.EqualTo(bundles));Directory.CreateDirectory(NextArtV2Stream.Evidence);File.WriteAllText(NextArtV2Stream.Evidence+"/RestoreExit_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"worldRevokedWithinRestore",true},{"lateLoadsReleased",surviving.LateReleases},{"liveHandles",surviving.LiveHandles},{"bundlesAfter",bundles},{"pumpDestroyed",true},{"nativeSweeps",RoomScenePump.CompletedSweeps}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {if(!Application.isPlaying)yield break;if(previous.IsValid())SceneManager.SetActiveScene(previous);if(main.IsValid()&&main.isLoaded){surviving=stream?stream.Owner:null;var unload=SceneManager.UnloadSceneAsync(main);while(unload!=null&&!unload.isDone)yield return null;}if(surviving!=null)yield return Wait(()=>surviving.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0,"Cleanup survivor");Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
