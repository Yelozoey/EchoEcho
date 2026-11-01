using System.Collections;
using EchoStation.Echo;
using EchoStation.Echo.Editor;
using EchoStation.World;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Echo
{
    public class ScanSensorTests
    {
        private Scene scene,previous;private PhysicsScene physics;private EchoRig rig;private WorldTarget target;private ClueDefinition definition;private ClueSource clue;private bool background;
        private IEnumerator Setup()
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("ScanSensorFixture",new CreateSceneParameters(LocalPhysicsMode.Physics3D));SceneManager.SetActiveScene(scene);physics=scene.GetPhysicsScene();
            rig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath)).GetComponent<EchoRig>();
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.SetActive(false);go.name="HiddenScanFixture";go.transform.position=new Vector3(0,.74f,1.6f);go.transform.localScale=new Vector3(.3f,.3f,.015f);go.GetComponent<BoxCollider>().isTrigger=true;
            target=go.AddComponent<WorldTarget>();target.Configure("sensor_fixture","扫描夹具");go.AddComponent<EchoTargetPermissions>().Configure(true,false);
            definition=ScriptableObject.CreateInstance<ClueDefinition>();definition.Configure(target.Id,"扫描线索","这是一份扫描后才可供玩家读取的完整资料。","扫描发现的简短摘要。",ClueKind.Scan);clue=go.AddComponent<ClueSource>();clue.Configure(definition,go.GetComponent<MeshRenderer>());go.SetActive(true);physics.Simulate(.02f);yield return null;
        }
        [UnityTest]public IEnumerator E08_B01_NonEchoOutOfRangeAndOutOfPitchCannotStartOrReveal()
        {
            yield return new EnterPlayMode();yield return Setup();var noEcho=new EchoScanEngine(null,physics);Assert.That(noEcho.TryBegin(target),Is.False);Assert.That(noEcho.LastRequestFailure,Is.EqualTo(ScanFailure.NotEcho));
            var engine=new EchoScanEngine(rig,physics);target.transform.position=new Vector3(0,.74f,3);physics.Simulate(.02f);Assert.That(engine.TryBegin(target),Is.False);Assert.That(engine.LastRequestFailure,Is.EqualTo(ScanFailure.OutOfRange));
            target.transform.position=new Vector3(0,2.2f,.4f);physics.Simulate(.02f);Assert.That(engine.TryBegin(target),Is.False);Assert.That(engine.LastRequestFailure,Is.EqualTo(ScanFailure.OutOfView));Assert.That(clue.IsRevealed,Is.False);
        }
        [UnityTest]public IEnumerator E08_B02_ActualWallBlocksButTargetOwnTriggerIsAllowed()
        {
            yield return new EnterPlayMode();yield return Setup();var engine=new EchoScanEngine(rig,physics);Assert.That(engine.Eligibility(target),Is.EqualTo(ScanFailure.None));
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(0,.8f,.8f);wall.transform.localScale=new Vector3(1.5f,2,.1f);physics.Simulate(.02f);
            Assert.That(engine.TryBegin(target),Is.False);Assert.That(engine.LastRequestFailure,Is.EqualTo(ScanFailure.Obstructed));Assert.That(clue.IsRevealed,Is.False);Object.Destroy(wall);yield return null;physics.Simulate(.02f);Assert.That(engine.TryBegin(target),Is.True);
        }
        [UnityTest]public IEnumerator E08_B03_AimingDwellCooldownAndRepeatDiscoveryAreEnforced()
        {
            yield return new EnterPlayMode();yield return Setup();var engine=new EchoScanEngine(rig,physics);var discoveries=0;engine.Ledger.Discovered+=receipt=>discoveries++;
            Assert.That(engine.TryBegin(target),Is.True);engine.Tick(.01f);Assert.That(engine.Phase,Is.EqualTo(ScanPhase.Scanning));engine.Tick(.3f);Assert.That(clue.IsRevealed,Is.False);engine.Tick(.31f);Assert.That(engine.Phase,Is.EqualTo(ScanPhase.Completed));Assert.That(clue.IsRevealed&&engine.LastReceipt.IsNewDiscovery,Is.True);
            Assert.That(engine.TryBegin(target),Is.False);Assert.That(engine.LastRequestFailure,Is.EqualTo(ScanFailure.Cooldown));engine.Tick(1.01f);Assert.That(engine.TryBegin(target),Is.True);engine.Tick(.01f);engine.Tick(.61f);Assert.That(engine.LastReceipt.IsNewDiscovery,Is.False);Assert.That(discoveries,Is.EqualTo(1));
        }
        [UnityTest]public IEnumerator E08_B04_CancelOrTargetLeavingRangeBeforeCompletionNeverReveals()
        {
            yield return new EnterPlayMode();yield return Setup();var engine=new EchoScanEngine(rig,physics);engine.TryBegin(target);engine.Tick(.01f);engine.Tick(.3f);engine.Cancel();engine.Tick(1);Assert.That(clue.IsRevealed,Is.False);Assert.That(engine.Phase,Is.EqualTo(ScanPhase.Cancelled));
            Assert.That(engine.TryBegin(target),Is.True);engine.Tick(.01f);target.transform.position=new Vector3(0,.74f,5);physics.Simulate(.02f);engine.Tick(.7f);Assert.That(engine.Phase,Is.EqualTo(ScanPhase.Failed));Assert.That(engine.Failure,Is.EqualTo(ScanFailure.OutOfRange));Assert.That(clue.IsRevealed,Is.False);
        }
        [UnityTest]public IEnumerator E08_B05_ProbeActuallyAimsToSideTargetWithoutMovingBody()
        {
            yield return new EnterPlayMode();yield return Setup();target.transform.position=new Vector3(1.2f,.9f,.4f);physics.Simulate(.02f);var body=rig.Body.position;var rotation=rig.transform.rotation;var engine=new EchoScanEngine(rig,physics);Assert.That(engine.TryBegin(target),Is.True);
            for(var i=0;i<40&&engine.IsBusy;i++)engine.Tick(.05f);Assert.That(engine.Phase,Is.EqualTo(ScanPhase.Completed));Assert.That(Quaternion.Angle(Quaternion.identity,rig.ProbePitch.localRotation),Is.GreaterThan(30));Assert.That(Vector3.Angle(rig.ScanOrigin.forward,target.transform.position-rig.ScanOrigin.position),Is.LessThan(2));Assert.That(rig.Body.position,Is.EqualTo(body));Assert.That(rig.transform.rotation,Is.EqualTo(rotation));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(Application.isPlaying)
            {
                if(definition)Object.Destroy(definition);if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();
                while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new System.TimeoutException("Sensor fixture unload timed out.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();
            }
        }
    }
}
