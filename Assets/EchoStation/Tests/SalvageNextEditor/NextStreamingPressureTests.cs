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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextStreamingPressureTests
    {
        const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R03c-1";
        Scene previous,main;bool background;NextRoomStreaming stream;NextSession session;SalvageMotor motor;PlayerInputReader input;SalvageCarry carry;SalvageEcho echo;SalvageEchoCoordinator brain;RoomSceneOwner owner;
        static IEnumerator Wait(Func<bool> condition,string stage){var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(15),stage);yield return null;}}
        IEnumerator Boot()
        {
            background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();main=EditorSceneManager.LoadSceneInPlayMode(NextRoomPartition.Main,new LoadSceneParameters(LoadSceneMode.Additive));while(!main.isLoaded)yield return null;SceneManager.SetActiveScene(main);var root=main.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");stream=root.GetComponentInChildren<NextRoomStreaming>();owner=stream.Owner;session=stream.GetComponent<NextSession>();motor=stream.GetComponent<SalvageMotor>();input=stream.GetComponent<PlayerInputReader>();carry=stream.GetComponent<SalvageCarry>();echo=root.GetComponentInChildren<SalvageEcho>();brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;brain.AutomaticWarnings=false;Assert.That(brain.Initialize(),Is.True);brain.ResetBase(IntentKind.Wait);motor.enabled=false;session.ConfigureStoreForTests(Path.GetFullPath("Library/EchoStation/SalvageNext/Pressure_"+Guid.NewGuid().ToString("N")));yield return Wait(()=>owner.IsReady(0)&&owner.IsReady(1)&&!stream.Blocking,"Initial ready");input.SetApplicationFocus(true);input.SetPointerCaptured(true);
        }
        void Place(float player,float robot){motor.RestorePose(new Vector3(player,.1f,12),90,0);Assert.That(echo.Navigation.Agent.Warp(new Vector3(robot,0,12)),Is.True);stream.EvaluateNow();}
        static int BundledRoomMeshes(int index)=>Resources.FindObjectsOfTypeAll<Mesh>().Count(m=>m&&m.name.StartsWith("ES_Next_Room"+index+"_Merged_",StringComparison.Ordinal)&&AssetDatabase.GetAssetPath(m)=="");
        [UnityTest,Timeout(180000)]public IEnumerator TargetLeaseCancelRapidCrossingsAndHeldSaveLoadsKeepOnlyCurrentWorldAndBoundedResources()
        {
            yield return new EnterPlayMode();yield return Boot();Directory.CreateDirectory(Evidence);var seed=session.Capture();seed.World.Collaboration.Discovered=new[]{"cargo_f"};Assert.That(session.TryRestore(seed,out var error),Is.True,error);yield return Wait(()=>owner.IsReady(0)&&owner.IsReady(1)&&!stream.Blocking,"Validated discovered-target snapshot");Assert.That(brain.Command(ScriptCommands.Investigate("cargo_f")),Is.True);stream.EvaluateNow();Assert.That(stream.Policy.UsedTaskLease,Is.True);Assert.That(stream.Policy.Wanted(2),Is.True);yield return Wait(()=>owner.IsReady(2),"Far active target preloaded");Assert.That(brain.CancelCurrent(),Is.True);Assert.That(echo.Navigation.Agent.Warp(new Vector3(3,0,12)),Is.True);stream.EvaluateNow();Assert.That(stream.Policy.UsedTaskLease,Is.False);yield return Wait(()=>owner.Phase(2)==RoomScenePhase.Empty,"Cancel releases far target room");
            motor.RestorePose(new Vector3(7,.1f,19),90,35);stream.EvaluateNow();yield return Wait(()=>!stream.Blocking,"Pickup room ready");input.SetApplicationFocus(true);input.SetPointerCaptured(true);Assert.That(carry.TryPickFromView(),Is.True,carry.LastMessage);Assert.That(carry.HeldId,Is.EqualTo("cargo_a"));Assert.That(session.Save(),Is.True);long startingEpoch=session.Epoch;ulong startingGeneration=brain.Generation;int loads=0;
            for(int i=0;i<16;i++)
            {
                bool far=i%2==0;Place(far?34:5,far?33:3);Assert.That(brain.CancelCurrent(),Is.True);stream.EvaluateNow();owner.Poll();
                if(i==0||i%4==3){Assert.That(session.Load(),Is.True,stream.GetComponent<SalvageMission>().Notice);loads++;Assert.That(carry.HeldId,Is.EqualTo("cargo_a"));Assert.That(brain.CurrentTargetId,Is.Null);Assert.That(brain.CurrentMode,Is.EqualTo(IntentKind.Wait));}
                yield return null;Assert.That(owner.Failures,Is.Zero,owner.LastError);
            }
            Place(5,3);yield return Wait(()=>owner.IsReady(0)&&owner.IsReady(1)&&owner.Phase(2)==RoomScenePhase.Empty&&!stream.Blocking,"Final near state ready");yield return Wait(()=>BundledRoomMeshes(0)==13&&BundledRoomMeshes(1)==12&&BundledRoomMeshes(2)==0,"Native room mesh counts settle through product sweep");Assert.That(carry.HeldId,Is.EqualTo("cargo_a"));Assert.That(carry.Registry.Get("cargo_a").State,Is.EqualTo(CargoState.Held));Assert.That(session.Epoch,Is.EqualTo(startingEpoch+loads));Assert.That(brain.Generation,Is.GreaterThan(startingGeneration));Assert.That(owner.LateReleases,Is.GreaterThan(0));Assert.That(owner.LiveHandles,Is.EqualTo(2));Assert.That(GameRuntimeOptions.UseLLM,Is.False);
            File.WriteAllText(Evidence+"/Pressure_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"crossings",16},{"diskLoads",loads},{"epochBefore",startingEpoch},{"epochAfter",session.Epoch},{"generationBefore",startingGeneration},{"generationAfter",brain.Generation},{"lateReleases",owner.LateReleases},{"commits",owner.Commits},{"failures",owner.Failures},{"heldId",carry.HeldId},{"meshCounts",new JArray(BundledRoomMeshes(0),BundledRoomMeshes(1),BundledRoomMeshes(2))},{"testCallsUnusedAssets",false},{"targetDiscoveryFixture","Validated snapshot, not a scan claim"},{"savePath",session.Store.Primary},{"liveResidentHandles",owner.LiveHandles},{"nativeSweeps",RoomScenePump.CompletedSweeps}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(previous.IsValid())SceneManager.SetActiveScene(previous);if(main.IsValid()&&main.isLoaded){var unload=SceneManager.UnloadSceneAsync(main);while(unload!=null&&!unload.isDone)yield return null;}if(owner!=null)yield return Wait(()=>owner.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0,"Pressure cleanup");Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
