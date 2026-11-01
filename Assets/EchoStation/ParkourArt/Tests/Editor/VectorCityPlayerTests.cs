using System;
using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.ParkourArt.Editor;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Player;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.ParkourArt.Tests
{
    public sealed class VectorCityPlayerTests
    {
        const string Evidence="Docs/Tests/ParkourArt/Evidence/PK06";
        Scene scene,previous;SyntheticInputScope inputs;VectorCityFirstPerson player;PlayerInputReader reader;CharacterController cc;
        bool background,capturedBackground;
        Behaviour[] muted=Array.Empty<Behaviour>();
        [Test] public void PK06_SceneStartsWithOneFirstPersonCameraAndSafeSpawn()
        {
            EditorSceneManager.OpenScene(VectorCityBuilder.ScenePath,OpenSceneMode.Single);
            try{
                var actor=Object.FindAnyObjectByType<VectorCityFirstPerson>();Assert.That(actor,Is.Not.Null);
                Assert.That(actor.GetComponent<PlayerInputReader>(),Is.Not.Null);Assert.That(actor.GetComponent<CharacterController>(),Is.Not.Null);
                Assert.That(actor.View.enabled&&actor.View.CompareTag("MainCamera"),Is.True);Assert.That(actor.View.transform.localPosition.y,Is.EqualTo(1.6f));
                Assert.That(Object.FindObjectsByType<Camera>().Count(c=>c.enabled&&c.cameraType==CameraType.Game),Is.EqualTo(1));
                Assert.That(actor.View.GetUniversalAdditionalCameraData().volumeLayerMask.value,Is.EqualTo(1));Assert.That(actor.View.farClipPlane,Is.EqualTo(280));
                Assert.That(actor.View.GetUniversalAdditionalCameraData().allowHDROutput,Is.False);
                Physics.SyncTransforms();Assert.That(Physics.Raycast(actor.SpawnPosition+Vector3.up*.05f,Vector3.down,out var hit,.4f),Is.True);Assert.That(actor.SpawnPosition.y-hit.point.y,Is.EqualTo(.1f).Within(.02f));
                VectorCityPlayerSetup.SetFirstPersonView(false);Assert.That(Camera.main.name,Is.EqualTo("Camera_Hero"));
                VectorCityPlayerSetup.SetFirstPersonView(true);Assert.That(Camera.main,Is.EqualTo(actor.View));
            }finally{EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);}
        }
        IEnumerator Boot()
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;capturedBackground=true;Application.runInBackground=true;
            previous=SceneManager.GetActiveScene();inputs=new SyntheticInputScope();
            muted=Object.FindObjectsByType<Behaviour>().Where(b=>b.enabled&&(b is AudioListener||b is Light||b is Camera)).ToArray();foreach(var item in muted)item.enabled=false;
            scene=EditorSceneManager.LoadSceneInPlayMode(VectorCityBuilder.ScenePath,new LoadSceneParameters(LoadSceneMode.Additive,LocalPhysicsMode.Physics3D));
            var loading=System.Diagnostics.Stopwatch.StartNew();while(!scene.isLoaded){Assert.That(loading.Elapsed.TotalSeconds,Is.LessThan(8),"Player scene load timeout");yield return R04ShadowProbe.Frame();}SceneManager.SetActiveScene(scene);
            player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<VectorCityFirstPerson>()).Single();reader=player.GetComponent<PlayerInputReader>();cc=player.GetComponent<CharacterController>();
            foreach(var renderer in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>()))renderer.gameObject.layer=30;
            player.View.cullingMask=1<<30;foreach(var light in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>()))light.cullingMask=1<<30;
            reader.SetApplicationFocus(true);reader.SetPointerCaptured(true);scene.GetPhysicsScene().Simulate(.02f);
            yield return Settle();
        }
        IEnumerator Frame(){scene.GetPhysicsScene().Simulate(.02f);yield return R04ShadowProbe.Frame();}
        IEnumerator Settle(){var timer=System.Diagnostics.Stopwatch.StartNew();while(!player.Grounded){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(4),"Spawn did not settle on roof");yield return Frame();}}
        void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(inputs.Keyboard,new KeyboardState(keys));
        IEnumerator Hold(float duration,params Key[] keys)
        {
            var start=Time.time;var timer=System.Diagnostics.Stopwatch.StartNew();
            while(Time.time-start<duration){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8),"Player frames stalled");Keys(keys);yield return Frame();}
            Keys();yield return Frame();
        }
        void Position(Vector3 point,float yaw=0){cc.enabled=false;player.transform.position=point;cc.enabled=true;player.SetLookAngles(yaw,0);scene.GetPhysicsScene().Simulate(.02f);}
        [UnityTest] public IEnumerator PK06_RealInputMovesLooksJumpsCollidesAndResets()
        {
            yield return new EnterPlayMode();yield return Boot();Directory.CreateDirectory(Evidence);
            var start=player.transform.position;yield return Hold(.3f,Key.W);float walked=player.transform.position.z-start.z;
            Assert.That(walked,Is.GreaterThan(.5f));Assert.That(Mathf.Abs(player.transform.position.x-start.x),Is.LessThan(.03f));
            var sprintStart=player.transform.position;yield return Hold(.3f,Key.W,Key.LeftShift);float sprinted=player.transform.position.z-sprintStart.z;Assert.That(sprinted,Is.GreaterThan(walked*1.4f));
            InputSystem.QueueStateEvent(inputs.Mouse,new MouseState{delta=new Vector2(70,-30)});yield return Frame();
            Assert.That(player.Yaw,Is.GreaterThan(2));Assert.That(player.Pitch,Is.GreaterThan(1));
            player.ResetToSpawn();yield return Settle();
            float baseY=player.transform.position.y,peak=baseY;Keys(Key.Space);yield return Frame();Keys();
            var timer=System.Diagnostics.Stopwatch.StartNew();bool airborne=false;
            do{yield return Frame();peak=Mathf.Max(peak,player.transform.position.y);airborne|=player.transform.position.y>baseY+.1f;Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8),"Jump did not land");}while(!airborne||!player.Grounded);
            Assert.That(peak-baseY,Is.GreaterThan(1.2f));Assert.That(player.transform.position.y,Is.EqualTo(baseY).Within(.1f));
            Position(new Vector3(-32,12.1f,-22),270);yield return Hold(.5f,Key.W);float wallX=player.transform.position.x;
            Assert.That(wallX,Is.InRange(-32.75f,-32.35f),"Actual left guardrail must block the player");
            Position(new Vector3(-25,12.1f,-23));Keys(Key.R);yield return Frame();Keys();yield return Frame();Assert.That(Vector2.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(player.SpawnPosition.x,player.SpawnPosition.z)),Is.LessThan(.02f));
            Position(new Vector3(-22,4,-25));yield return Frame();Assert.That(player.transform.position.y,Is.GreaterThan(11.9f));
            Keys(Key.Escape);yield return Frame();Keys();Assert.That(reader.PointerCaptured,Is.False);var stopped=player.transform.position;yield return Hold(.15f,Key.W);Assert.That(Vector2.Distance(new Vector2(stopped.x,stopped.z),new Vector2(player.transform.position.x,player.transform.position.z)),Is.LessThan(.02f));
            InputSystem.QueueStateEvent(inputs.Mouse,new MouseState{buttons=1});yield return Frame();Assert.That(reader.PointerCaptured,Is.True);
            player.ResetToSpawn();yield return Frame();float mean;
            using(var frames=new ParkourFrameCapture(player.View)){mean=frames.Capture(Evidence+"/FirstPerson_1080.png").Average(c=>(c.r+c.g+c.b)/3);}
            Assert.That(mean,Is.GreaterThan(.05f));
            File.WriteAllText(Evidence+"/ActualPlayer.json","{\"walkDistance\":"+walked.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"sprintDistance\":"+sprinted.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"jumpHeight\":"+(peak-baseY).ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"guardrailStopX\":"+wallX.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"imageMean\":"+mean.ToString(System.Globalization.CultureInfo.InvariantCulture)+",\"inputAndResetPassed\":true}");
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(inputs!=null){inputs.Dispose();Assert.That(inputs.IsRestored,Is.True);inputs=null;}
            if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var op=scene.IsValid()&&scene.isLoaded?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(op!=null&&!op.isDone){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(8));yield return null;}foreach(var item in muted)if(item)item.enabled=true;muted=Array.Empty<Behaviour>();if(capturedBackground){Application.runInBackground=background;capturedBackground=false;}yield return new ExitPlayMode();}
            else if(capturedBackground){Application.runInBackground=background;capturedBackground=false;}
        }
    }
}
