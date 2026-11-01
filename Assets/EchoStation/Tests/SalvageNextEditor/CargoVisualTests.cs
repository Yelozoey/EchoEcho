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
    public sealed class CargoVisualTests
    {
        Scene previous,scene;bool background;
        [Test] public void SevenVisualsKeepCanonicalIdsTexturesBoundsAndNoGameplayComponents()
        {
            foreach(var item in SalvageCatalog.Items)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(CargoAssets.PathFor(item.Id));Assert.That(prefab,Is.Not.Null,item.Id);
                var model=prefab.GetComponent<CargoVisualModel>();Assert.That(model.Id,Is.EqualTo(item.Id));var bounds=model.LocalBounds;
                Assert.That(Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z),Is.EqualTo(item.Id=="cargo_heavy"?.8f:.44f).Within(.001f));Assert.That(bounds.min.y,Is.EqualTo(0).Within(.001f));
                Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<CargoActor>(true),Is.Empty);
                foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>())foreach(var material in renderer.sharedMaterials)
                {Assert.That(material.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));Assert.That(AssetDatabase.GetAssetPath(material),Does.StartWith(FacilityAssets.Generated));Assert.That(material.GetTexture("_BaseMap"),Is.Not.Null);}
            }
        }
        void Aim(SalvageMotor motor,CargoActor actor)
        {
            motor.RestorePose(actor.transform.position+new Vector3(0,.1f,-1.8f),0,0);var delta=actor.Body.bounds.center-motor.View.position;
            motor.RestorePose(motor.transform.position,0,-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg);Physics.SyncTransforms();
        }
        [UnityTest,Timeout(180000)] public IEnumerator SixRealRayPicksDropsConfirmedDeliveriesAndHeavyRejection()
        {
            yield return new EnterPlayMode();Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();
            scene=EditorSceneManager.LoadSceneInPlayMode("Assets/EchoStation/Salvage/Scenes/ES_Salvage_Demo.unity",new LoadSceneParameters(LoadSceneMode.Additive));while(!scene.isLoaded)yield return null;SceneManager.SetActiveScene(scene);yield return null;
            var objects=scene.GetRootGameObjects();var motor=objects.SelectMany(r=>r.GetComponentsInChildren<SalvageMotor>()).Single();motor.enabled=false;
            var echo=objects.SelectMany(r=>r.GetComponentsInChildren<SalvageEcho>()).Single();var brain=echo.GetComponent<SalvageEchoCoordinator>();brain.enabled=false;echo.enabled=false;Assert.That(brain.Initialize(),Is.True);brain.AutomaticWarnings=false;brain.ResetBase(IntentKind.Wait);
            var input=motor.GetComponent<PlayerInputReader>();input.SetApplicationFocus(true);input.SetPointerCaptured(true);input.SetUiBlocked(false);
            var carry=motor.GetComponent<SalvageCarry>();var delivery=motor.GetComponent<SalvageDelivery>();var actors=objects.SelectMany(r=>r.GetComponentsInChildren<CargoActor>(true)).ToArray();Assert.That(actors.Length,Is.EqualTo(7));
            var evidence=new JArray();foreach(var actor in actors){var position=actor.transform.position;CargoAssets.Attach(actor);Assert.That(actor.transform.position,Is.EqualTo(position));Assert.That(actor.Body.size.x,Is.GreaterThan(0));}
            foreach(var item in SalvageCatalog.Items.Where(i=>i.Kilograms<=8))
            {
                var actor=actors.Single(a=>a.Id==item.Id);var model=actor.GetComponentInChildren<CargoVisualModel>();Aim(motor,actor);Assert.That(carry.Focus(),Is.SameAs(actor),item.Id);
                Assert.That(carry.TryPickFromView(),Is.True,carry.LastMessage);model.RefreshLabels();Assert.That(actor.Body.enabled,Is.False);Assert.That(actor.GetComponentsInChildren<TextMesh>().All(t=>!t.GetComponent<Renderer>().enabled),Is.True);Assert.That(model.gameObject.activeInHierarchy,Is.True);
                // Face the open central floor before dropping; this exercises the real surface and overlap checks.
                motor.RestorePose(new Vector3(5,.1f,8),0,25);Physics.SyncTransforms();Assert.That(carry.TryDrop(),Is.True,carry.LastMessage);model.RefreshLabels();Assert.That(actor.Body.enabled,Is.True);Assert.That(carry.Registry.Get(item.Id).State,Is.EqualTo(CargoState.World));Assert.That(actor.Body.bounds.min.y,Is.GreaterThanOrEqualTo(.015f));
                Assert.That(actor.GetComponentsInChildren<TextMesh>().All(t=>t.GetComponent<Renderer>().enabled),Is.True);Aim(motor,actor);Assert.That(carry.TryPickFromView(),Is.True,carry.LastMessage);
                motor.RestorePose(new Vector3(5,.1f,12),0,0);Physics.SyncTransforms();int before=delivery.Count;Assert.That(delivery.Request(),Is.True);Assert.That(delivery.Count,Is.EqualTo(before));Assert.That(delivery.Confirm(),Is.True);Assert.That(delivery.Confirm(),Is.False);
                Assert.That(actor.gameObject.activeSelf,Is.False);Assert.That(delivery.Count,Is.EqualTo(before+1));evidence.Add(new JObject{{"id",item.Id},{"kg",item.Kilograms},{"points",item.Points},{"rayPickDropRepickConfirmedDeliver",true}});
            }
            Assert.That(delivery.Count,Is.EqualTo(6));Assert.That(delivery.Points,Is.EqualTo(28));var heavy=actors.Single(a=>a.Id=="cargo_heavy");Aim(motor,heavy);Assert.That(carry.Focus(),Is.SameAs(heavy));Assert.That(carry.TryPickFromView(),Is.False);Assert.That(carry.LastMessage,Does.Contain("8kg"));
            carry.ResetToInitial();foreach(var actor in actors){actor.GetComponentInChildren<CargoVisualModel>().RefreshLabels();Assert.That(actor.gameObject.activeSelf,Is.True);Assert.That(actor.Body.enabled,Is.True);}Assert.That(delivery.Count,Is.Zero);
            Directory.CreateDirectory(CargoAssets.Evidence);File.WriteAllText(CargoAssets.Evidence+"/Carry_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"items",evidence},{"deliveredBeforeReset",6},{"pointsBeforeReset",28},{"heavyRejected",true},{"resetRestoredAll",true},{"UseLLM",false}}.ToString());
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {if(!Application.isPlaying)yield break;if(previous.IsValid())SceneManager.SetActiveScene(previous);if(scene.IsValid()){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
