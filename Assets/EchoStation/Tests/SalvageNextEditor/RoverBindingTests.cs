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
    public sealed class RoverBindingTests
    {
        const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-A01c-2";
        Scene old,scene;bool background;SalvageMotor motor;SalvageCommandHub hub;SalvageEcho echo;SalvageEchoCoordinator brain;RoverAppearance appearance;
        IEnumerator Boot()
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;Directory.CreateDirectory(Evidence);old=SceneManager.GetActiveScene();
            scene=EditorSceneManager.LoadSceneInPlayMode("Assets/EchoStation/Salvage/Scenes/ES_Salvage_Demo.unity",new LoadSceneParameters(LoadSceneMode.Additive));
            var timer=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8));yield return null;}SceneManager.SetActiveScene(scene);yield return null;
            motor=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;motor.GetComponent<PlayerInputReader>().SetApplicationFocus(true);motor.GetComponent<PlayerInputReader>().SetPointerCaptured(true);hub=motor.GetComponent<SalvageCommandHub>();
            echo=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;echo.enabled=false;Assert.That(brain.Initialize(),Is.True);brain.AutomaticWarnings=false;brain.ResetBase(IntentKind.Wait);
            appearance=echo.gameObject.AddComponent<RoverAppearance>();appearance.Configure(AssetDatabase.LoadAssetAtPath<GameObject>(RoverAssets.PrefabPath).GetComponent<RoverModelParts>());
            Assert.That(GameRuntimeOptions.UseLLM,Is.False);
        }
        [UnityTest,Timeout(180000)] public IEnumerator MountMapsThreeStatesMeasuredSixWheelsAndRestoresOldRigOnDisable()
        {
            yield return new EnterPlayMode();yield return Boot();var rig=echo.GetComponent<EchoRig>();var originalBody=rig.Body;var originalProbe=rig.ProbePitch;var originalOrigin=rig.ScanOrigin;var originalStatus=rig.Status;var originalWheels=rig.Wheels.ToArray();var oldRenderers=echo.GetComponentsInChildren<Renderer>();var flags=oldRenderers.Select(r=>r.enabled).ToArray();
            var presentation=echo.GetComponent<EchoPresentation>();var lightParent=presentation.ScanLight.transform.parent;var collider=echo.GetComponent<BoxCollider>();var size=collider.size;
            Assert.That(appearance.Mount(),Is.True);var model=appearance.Visual;Assert.That(rig.Body,Is.SameAs(model.Body));Assert.That(rig.ProbePitch,Is.SameAs(model.Head));Assert.That(rig.ScanOrigin,Is.SameAs(model.ScanOrigin));Assert.That(rig.Wheels,Is.EqualTo(originalWheels));Assert.That(model.WheelCount,Is.EqualTo(6));Assert.That(oldRenderers.All(r=>!r.enabled),Is.True);Assert.That(presentation.ScanLight.transform.parent,Is.SameAs(model.ScanOrigin));Assert.That(collider.size,Is.EqualTo(size));
            presentation.ApplyFrame(0,EchoMood.Calm,.1f,0,false);Assert.That(model.Indicator.sharedMaterial,Is.SameAs(model.Calm));
            var pose=model.Head.localRotation;presentation.ApplyFrame(0,EchoMood.Thinking,.1f,.15f,false);Assert.That(model.Indicator.sharedMaterial,Is.SameAs(model.Thinking));Assert.That(model.Indicator.enabled,Is.True);Assert.That(Quaternion.Angle(pose,model.Head.localRotation),Is.GreaterThan(1));
            presentation.ApplyFrame(0,EchoMood.Thinking,.1f,.4f,true);Assert.That(model.Indicator.enabled,Is.False);Assert.That(presentation.ScanLight.enabled,Is.True);
            presentation.ApplyFrame(0,EchoMood.Alert,.1f,.5f,false);Assert.That(model.Indicator.sharedMaterial,Is.SameAs(model.Alert));Assert.That(presentation.ScanLight.enabled,Is.False);
            var rotations=Enumerable.Range(0,6).Select(i=>model.Wheel(i).localRotation).ToArray();appearance.AdvanceWheels(1,.1f);for(int i=0;i<6;i++)Assert.That(Quaternion.Angle(rotations[i],model.Wheel(i).localRotation),Is.GreaterThan(5));
            appearance.enabled=false;Assert.That(rig.Body,Is.SameAs(originalBody));Assert.That(rig.ProbePitch,Is.SameAs(originalProbe));Assert.That(rig.ScanOrigin,Is.SameAs(originalOrigin));Assert.That(rig.Status,Is.SameAs(originalStatus));Assert.That(presentation.ScanLight.transform.parent,Is.SameAs(lightParent));Assert.That(oldRenderers.Select(r=>r.enabled),Is.EqualTo(flags));Assert.That(originalStatus.sharedMaterial,Is.SameAs(rig.Alert));
            yield return null;Assert.That(echo.GetComponentsInChildren<RoverModelParts>(true),Is.Empty);appearance.enabled=true;Assert.That(appearance.Mounted,Is.True);Assert.That(echo.GetComponentsInChildren<RoverModelParts>().Length,Is.EqualTo(1));
            File.WriteAllText(Evidence+"/States_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"newVisualWheels",6},{"legacyDriverWheels",rig.Wheels.Count},{"blueYellowRed",true},{"probeMoved",true},{"scanLightReparentedAndRestored",true},{"disableReenable",true},{"colliderChanged",false},{"UseLLM",false}}.ToString());
        }
        [UnityTest,Timeout(180000)] public IEnumerator RealFollowMovesSixVisibleWheelsAndWaitStopsWithoutChangingCollision()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(appearance.Mount(),Is.True);motor.RestorePose(new Vector3(9,.1f,12),0,0);Physics.SyncTransforms();var start=echo.transform.position;var model=appearance.Visual;
            var initial=Enumerable.Range(0,6).Select(i=>model.Wheel(i).localRotation).ToArray();var moved=new bool[6];brain.ResetBase(IntentKind.Follow);var timer=System.Diagnostics.Stopwatch.StartNew();
            while(Vector3.Distance(start,echo.transform.position)<.4f)
            {
                Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8),"Actual NavMesh follow did not move.");yield return null;brain.Advance(Mathf.Min(Time.deltaTime,.1f),false);
                for(int i=0;i<6;i++)moved[i]|=Quaternion.Angle(initial[i],model.Wheel(i).localRotation)>5;
            }
            yield return null;for(int i=0;i<6;i++)moved[i]|=Quaternion.Angle(initial[i],model.Wheel(i).localRotation)>5;Assert.That(moved.All(value=>value),Is.True);
            var body=echo.GetComponent<BoxCollider>();Assert.That(body.enabled,Is.True);var bounds=body.bounds;
            foreach(var hit in Physics.OverlapBox(bounds.center,bounds.extents*.95f,Quaternion.identity,~0,QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(echo.transform)&&!hit.GetComponentInParent<SalvageDropSurface>()&&!hit.GetComponentInParent<CargoActor>())Assert.Fail("Robot penetrated "+hit.name);
            brain.ResetBase(IntentKind.Wait);timer.Restart();while(echo.Navigation.Agent.velocity.sqrMagnitude>.0001f){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(3));yield return null;brain.Advance(Mathf.Min(Time.deltaTime,.1f),false);}yield return null;
            var stopped=Enumerable.Range(0,6).Select(i=>model.Wheel(i).localRotation).ToArray();for(int f=0;f<8;f++){yield return null;brain.Advance(Mathf.Min(Time.deltaTime,.1f),false);}for(int i=0;i<6;i++)Assert.That(Quaternion.Angle(stopped[i],model.Wheel(i).localRotation),Is.LessThan(.1f));
            File.WriteAllText(Evidence+"/Movement_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"distance",Vector3.Distance(start,echo.transform.position)},{"allSixAnimated",true},{"waitStopped",true},{"wallPenetration",false},{"UseLLM",false}}.ToString());
        }
        [UnityTest,Timeout(180000)] public IEnumerator ActualScanUsesNewOriginAndCommitsOnlyInformation()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(appearance.Mount(),Is.True);var scanner=echo.GetComponent<SalvageScanner>();scanner.Initialize();
            motor.RestorePose(new Vector3(7,.1f,19),90,35);Assert.That(echo.Navigation.TryBind(new Vector3(9,0,17.5f)),Is.True);echo.transform.rotation=Quaternion.identity;Physics.SyncTransforms();Assert.That(hub.PointedId(),Is.EqualTo("cargo_a"));
            float charge=echo.Battery.Charge;Assert.That(brain.Command(ScriptCommands.Investigate("cargo_a")),Is.True);var timer=System.Diagnostics.Stopwatch.StartNew();
            while(!hub.Known.Contains("cargo_a")){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8),"New head scan failed: "+scanner.LastError);yield return null;brain.Advance(Mathf.Min(Time.deltaTime,.1f),false);}
            Assert.That(scanner.PaidDiscoveries,Is.EqualTo(1));Assert.That(charge-echo.Battery.Charge,Is.InRange(8f,8.7f));Assert.That(hub.GetComponent<SalvageCarry>().Registry.Get("cargo_a").State,Is.EqualTo(CargoState.World));Assert.That(hub.GetComponent<SalvageDelivery>().Count,Is.Zero);Assert.That(echo.GetComponent<EchoRig>().ScanOrigin,Is.SameAs(appearance.Visual.ScanOrigin));
            File.WriteAllText(Evidence+"/Scan_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"known","cargo_a"},{"paidDiscoveries",scanner.PaidDiscoveries},{"charged",charge-echo.Battery.Charge},{"cargoStillWorld",true},{"delivered",0},{"newHeadOrigin",true},{"UseLLM",false}}.ToString());
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();
        }
    }
}
