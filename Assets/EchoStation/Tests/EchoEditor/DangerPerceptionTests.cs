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
    public class DangerPerceptionTests
    {
        private Scene scene,previous;private PhysicsScene physics;private EchoRig rig;private WorldTarget target;private DangerPerception sensor;private bool background;
        private IEnumerator Setup()
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("DangerSenseFixture",new CreateSceneParameters(LocalPhysicsMode.Physics3D));SceneManager.SetActiveScene(scene);physics=scene.GetPhysicsScene();
            rig=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EchoModelSetup.PrefabPath)).GetComponent<EchoRig>();sensor=new DangerPerception(rig,physics);target=CreateTarget("danger_fixture_01",rig.ScanOrigin.position+Vector3.forward*3);physics.Simulate(.02f);yield return null;
        }
        private WorldTarget CreateTarget(string id,Vector3 position)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.SetActive(false);go.transform.position=position;go.transform.localScale=new Vector3(.3f,.8f,.15f);var identity=go.AddComponent<WorldTarget>();identity.Configure(id,"危险夹具");go.AddComponent<EchoTargetPermissions>().Configure(false,true);go.SetActive(true);return identity;
        }
        [UnityTest]public IEnumerator E10_A01_DistanceInsideBoundaryAndOutsideUseActualWorldPositions()
        {
            yield return new EnterPlayMode();yield return Setup();foreach(var distance in new[]{3.9f,4f,4.01f})
            {target.transform.position=rig.ScanOrigin.position+rig.transform.forward*distance;physics.Simulate(.02f);Assert.That(sensor.CanPerceive(target,out var failure),Is.EqualTo(distance<=4),distance+" / "+failure);}
        }
        [UnityTest]public IEnumerator E10_A02_ViewConeIncludesBoundaryAndRejectsOutside()
        {
            yield return new EnterPlayMode();yield return Setup();foreach(var angle in new[]{54.9f,55f,55.1f,120f})
            {target.transform.position=rig.ScanOrigin.position+Quaternion.Euler(0,angle,0)*Vector3.forward*3;physics.Simulate(.02f);Assert.That(sensor.CanPerceive(target,out var failure),Is.EqualTo(angle<=55),angle+" / "+failure);}
        }
        [UnityTest]public IEnumerator E10_A03_TargetOwnColliderAllowedAndActualWallOccludes()
        {
            yield return new EnterPlayMode();yield return Setup();Assert.That(sensor.CanPerceive(target,out _),Is.True);
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=rig.ScanOrigin.position+Vector3.forward;wall.transform.localScale=new Vector3(2,2,.1f);physics.Simulate(.02f);Assert.That(sensor.CanPerceive(target,out var failure),Is.False);Assert.That(failure,Is.EqualTo(DangerSenseFailure.Occluded));
        }
        [UnityTest]public IEnumerator E10_A04_ExplicitDangerCapabilityAndActiveActorAreRequired()
        {
            yield return new EnterPlayMode();yield return Setup();target.GetComponent<EchoTargetPermissions>().Configure(true,false);Assert.That(sensor.CanPerceive(target,out var failure),Is.False);Assert.That(failure,Is.EqualTo(DangerSenseFailure.NotDanger));
            target.GetComponent<EchoTargetPermissions>().Configure(false,true);Assert.That(sensor.CanPerceive(target,out _),Is.True);target.gameObject.SetActive(false);Assert.That(sensor.CanPerceive(target,out failure),Is.False);Assert.That(failure,Is.EqualTo(DangerSenseFailure.MissingTarget));Assert.That(new DangerPerception(null,physics).CanPerceive(target,out failure),Is.False);Assert.That(failure,Is.EqualTo(DangerSenseFailure.InvalidActor));
        }
        [UnityTest]public IEnumerator E10_A05_NearestSelectionSkipsOccludedDanger()
        {
            yield return new EnterPlayMode();yield return Setup();target.transform.position=rig.ScanOrigin.position+Vector3.forward*2.5f;var other=CreateTarget("danger_fixture_02",rig.ScanOrigin.position+new Vector3(1,0,3));physics.Simulate(.02f);Assert.That(sensor.TryFindNearest(out var found)&&found==target,Is.True);
            Assert.That(sensor.TryFindNearest(out found,candidate=>candidate!=target)&&found==other,Is.True,"A cooling-down near hazard must not hide another visible hazard.");
            var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=rig.ScanOrigin.position+new Vector3(0,0,1.3f);wall.transform.localScale=new Vector3(.4f,2,.1f);physics.Simulate(.02f);Assert.That(sensor.TryFindNearest(out found)&&found==other,Is.True);
        }
        [UnityTest]public IEnumerator E10_A06_ExceededRayBudgetRejectsConservatively()
        {
            yield return new EnterPlayMode();yield return Setup();for(var i=0;i<17;i++){var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=rig.ScanOrigin.position+Vector3.forward*(.4f+i*.1f);wall.transform.localScale=new Vector3(1,1,.025f);}physics.Simulate(.02f);
            Assert.That(sensor.CanPerceive(target,out var failure),Is.False);Assert.That(failure,Is.EqualTo(DangerSenseFailure.HitBudgetFull));
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(Application.isPlaying)
            {if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new System.TimeoutException("Danger perception fixture unload timed out.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
