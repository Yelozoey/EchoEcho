using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.Salvage.Editor;
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
    public sealed class NextHazardTests
    {
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        [Test]public void SavedVisualsKeepOriginalCollisionNavAndEightReadonlyTargets()
        {
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextRoomPartition.Main,OpenSceneMode.Additive);
            try
            {
                var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");Assert.That(root.GetComponentsInChildren<Collider>(true).Length,Is.EqualTo(118));Assert.That(root.GetComponentsInChildren<SalvageTargetView>().Length,Is.EqualTo(8));var models=root.GetComponentsInChildren<HazardVisualModel>();Assert.That(models.Length,Is.EqualTo(2));var expected=JObject.Parse(File.ReadAllText(NextHazardAssets.Evidence+"/Prepared_A.json"));
                foreach(var visual in models){Assert.That(visual.GetComponentsInChildren<Collider>(),Is.Empty);var target=visual.GetComponentInParent<SalvageTargetView>();Assert.That(visual.Id,Is.EqualTo(target.Id));Assert.That(target.Definition.Kind,Is.EqualTo(SalvageTargetKind.Danger));var collision=target.GetComponentsInChildren<BoxCollider>().Single();Assert.That(collision.GetComponent<MeshRenderer>().enabled,Is.False);var row=expected["targets"].Single(r=>r["id"].Value<string>()==target.Id);Assert.That(target.transform.position,Is.EqualTo(new Vector3(row["position"][0].Value<float>(),row["position"][1].Value<float>(),row["position"][2].Value<float>())));Assert.That(collision.size,Is.EqualTo(new Vector3(row["colliderSize"][0].Value<float>(),row["colliderSize"][1].Value<float>(),row["colliderSize"][2].Value<float>())));Assert.That(collision.transform.lossyScale,Is.EqualTo(new Vector3(row["colliderLossyScale"][0].Value<float>(),row["colliderLossyScale"][1].Value<float>(),row["colliderLossyScale"][2].Value<float>())));var bounds=NextLayout.BoundsOf(visual.gameObject);var allowed=collision.bounds;allowed.Expand(.03f);Assert.That(allowed.Contains(bounds.min)&&allowed.Contains(bounds.max),Is.True,"New visual must fit existing logical collision");Assert.That(visual.GetComponentsInChildren<MeshRenderer>().Length,Is.GreaterThan(0));Assert.That(visual.GetComponentsInChildren<Animator>(),Is.Empty);}
                Assert.That(Hash(NextRoomPartition.Nav),Is.EqualTo(JObject.Parse(File.ReadAllText(NextHazardAssets.Evidence+"/NavBefore.json"))["sha256"].Value<string>()));var screen=AssetDatabase.LoadAssetAtPath<Material>(FacilityAssets.Generated+"/ES_Next_Hazard_OrangeScreen.mat");Assert.That(screen.IsKeywordEnabled("_EMISSION"),Is.True);Assert.That((screen.globalIlluminationFlags&MaterialGlobalIlluminationFlags.EmissiveIsBlack)==0,Is.True);Assert.That(screen.GetColor("_EmissionColor").maxColorComponent,Is.GreaterThan(1));
                var indicator=AssetDatabase.LoadAssetAtPath<Material>(FacilityAssets.Generated+"/ES_Next_Hazard_Indicator.mat");Assert.That(indicator.IsKeywordEnabled("_EMISSION"),Is.True);Assert.That((indicator.globalIlluminationFlags&MaterialGlobalIlluminationFlags.EmissiveIsBlack)==0,Is.True);Assert.That(indicator.GetColor("_EmissionColor").maxColorComponent,Is.GreaterThan(1));
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        Scene previousScene,main;bool background;RoomSceneOwner owner;
        static IEnumerator Wait(Func<bool> condition,string stage){var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(15),stage);yield return null;}}
        [UnityTest]public IEnumerator RealModelsRemainPointableAndOnlyProvideWarningsWithCurrentNav()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;previousScene=SceneManager.GetActiveScene();main=EditorSceneManager.LoadSceneInPlayMode(NextRoomPartition.Main,new LoadSceneParameters(LoadSceneMode.Additive));while(!main.isLoaded)yield return null;SceneManager.SetActiveScene(main);var root=main.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var stream=root.GetComponentInChildren<NextRoomStreaming>();owner=stream.Owner;var motor=stream.GetComponent<SalvageMotor>();motor.enabled=false;var hub=stream.GetComponent<SalvageCommandHub>();var echo=root.GetComponentInChildren<SalvageEcho>();var brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;brain.AutomaticWarnings=false;Assert.That(brain.Initialize(),Is.True);brain.ResetBase(IntentKind.Wait);var rows=new JArray();
            foreach(string id in new[]{"danger_power_box","danger_leak_pipe"})
            {
                var target=hub.Targets.Single(t=>t.Id==id);var position=target.transform.position+new Vector3(-2,-.7f,0);motor.RestorePose(position,90,24);Assert.That(echo.Navigation.Agent.Warp(new Vector3(position.x,0,position.z)),Is.True);echo.transform.rotation=Quaternion.Euler(0,90,0);stream.EvaluateNow();yield return Wait(()=>owner.IsReady(stream.PlayerRoom)&&!stream.Blocking,"Hazard region ready");var input=stream.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);Physics.SyncTransforms();Assert.That(hub.PointedId(),Is.EqualTo(id));Assert.That(target.Allows(IntentKind.Investigate),Is.True);Assert.That(target.Allows(IntentKind.Warn),Is.True);Assert.That(brain.Command(ScriptCommands.Warn(id)),Is.True,"Existing warning perception must still accept visible danger");brain.Advance(.1f,false);Assert.That(target.GetComponentsInChildren<Collider>().All(c=>c.enabled),Is.True);Assert.That(stream.GetComponent<SalvageDelivery>().Count,Is.Zero);Assert.That(GameRuntimeOptions.UseLLM,Is.False);using(var image=new SalvageFrameCapture(motor.View.GetComponent<Camera>()))image.Capture(NextHazardAssets.Evidence+"/Actual_"+id+"_"+Guid.NewGuid().ToString("N")+".png");rows.Add(new JObject{{"id",id},{"pointed",true},{"investigate",true},{"warnAccepted",true},{"newPhysicsColliders",0}});brain.CancelCurrent();
            }
            File.WriteAllText(NextHazardAssets.Evidence+"/Runtime_"+Guid.NewGuid().ToString("N")+".json",rows.ToString());Assert.That(owner.Failures,Is.Zero,owner.LastError);
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(previousScene.IsValid())SceneManager.SetActiveScene(previousScene);if(main.IsValid()&&main.isLoaded){var unload=SceneManager.UnloadSceneAsync(main);while(unload!=null&&!unload.isDone)yield return null;}if(owner!=null)yield return Wait(()=>owner.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0,"Hazard cleanup");Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
