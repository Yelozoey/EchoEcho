using System;
using System.Collections;
using System.Collections.Generic;
using EchoStation.Player;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public class InteractionRaycasterTests
    {
        private Scene scene,previous;private bool background;private TargetRegistry registry;private Transform player;private InteractionRaycaster raycaster;private readonly List<ClueDefinition> definitions=new List<ClueDefinition>();
        private readonly Ray forward=new Ray(Vector3.zero,Vector3.forward);
        private void Setup(){background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("InteractionRayFixture",new CreateSceneParameters(LocalPhysicsMode.Physics3D));SceneManager.SetActiveScene(scene);registry=new TargetRegistry();player=new GameObject("PlayerRootFixture").transform;raycaster=new InteractionRaycaster(scene.GetPhysicsScene(),registry,player);}
        private WorldTarget Target(string id,Vector3 position,ClueKind? kind=ClueKind.Note,bool register=true)
        {
            var go=new GameObject(id);go.SetActive(false);go.transform.position=position;var box=go.AddComponent<BoxCollider>();box.size=Vector3.one*.2f;box.isTrigger=true;var target=go.AddComponent<WorldTarget>();target.Configure(id,"测试线索");if(kind.HasValue){var definition=ScriptableObject.CreateInstance<ClueDefinition>();definition.Configure(id,"测试标题","这是一段完整原文，仅供日志显示。","测试摘要。",kind.Value);definitions.Add(definition);go.AddComponent<ClueSource>().Configure(definition,null);}go.SetActive(true);if(register)Assert.That(registry.TryRegister(target,out _),Is.True);Sync();return target;
        }
        private BoxCollider Box(Vector3 position,bool trigger=false){var go=new GameObject("RayObstacle");go.transform.position=position;var box=go.AddComponent<BoxCollider>();box.size=Vector3.one*.1f;box.isTrigger=trigger;return box;}
        private void Sync()=>scene.GetPhysicsScene().Simulate(.02f);
        [UnityTest]public IEnumerator P04_A01_RegisteredVisibleClueHasExactIdentityPointAndDistance()
        {
            yield return new EnterPlayMode();Setup();var target=Target("interaction_note",new Vector3(0,0,2));Assert.That(raycaster.TrySelect(forward,out var selection),Is.True);Assert.That(selection.Target,Is.SameAs(target));Assert.That(selection.Clue,Is.SameAs(target.GetComponent<ClueSource>()));Assert.That(selection.Distance,Is.EqualTo(1.9f).Within(.001f));Assert.That(selection.Point.z,Is.EqualTo(1.9f).Within(.001f));Assert.That(raycaster.QuerySaturated,Is.False);
        }
        [UnityTest]public IEnumerator P04_A02_SurfaceAtRangeIsAllowedButBeyondRangeAndOffAxisAreRejected()
        {
            yield return new EnterPlayMode();Setup();var target=Target("interaction_range",new Vector3(0,0,2.6f));Assert.That(raycaster.TrySelect(forward,out _),Is.True);target.transform.position=new Vector3(0,0,2.61f);Sync();Assert.That(raycaster.TrySelect(forward,out _),Is.False);target.transform.position=new Vector3(1,0,2);Sync();Assert.That(raycaster.TrySelect(forward,out _),Is.False);
        }
        [UnityTest]public IEnumerator P04_A03_NearestSolidWallBlocksClueButWallBehindDoesNot()
        {
            yield return new EnterPlayMode();Setup();Target("interaction_wall",new Vector3(0,0,2));var wall=Box(new Vector3(0,0,1));Sync();Assert.That(raycaster.TrySelect(forward,out _),Is.False);wall.transform.position=new Vector3(0,0,2.35f);Sync();Assert.That(raycaster.TrySelect(forward,out _),Is.True);
        }
        [UnityTest]public IEnumerator P04_A04_OwnColliderAndUnrelatedTriggerDoNotBlockRegisteredTarget()
        {
            yield return new EnterPlayMode();Setup();var target=Target("interaction_own",new Vector3(0,0,2));Box(new Vector3(0,0,.5f),true);var own=Box(new Vector3(0,0,.8f));own.transform.SetParent(player);Sync();Assert.That(raycaster.TrySelect(forward,out var selection),Is.True);Assert.That(selection.Target,Is.SameAs(target));
        }
        [UnityTest]public IEnumerator P04_A05_HiddenUnregisteredAndDisabledTargetsCannotBeSelected()
        {
            yield return new EnterPlayMode();Setup();var hidden=Target("interaction_hidden",new Vector3(0,0,2),ClueKind.Scan);Assert.That(raycaster.TrySelect(forward,out _),Is.False);Assert.That(hidden.GetComponent<ClueSource>().IsRevealed,Is.False);hidden.gameObject.SetActive(false);var unregistered=Target("interaction_unknown",new Vector3(0,0,2),ClueKind.Note,false);Assert.That(raycaster.TrySelect(forward,out _),Is.False);Assert.That(registry.TryRegister(unregistered,out _),Is.True);Assert.That(raycaster.TrySelect(forward,out _),Is.True);unregistered.enabled=false;Assert.That(raycaster.TrySelect(forward,out _),Is.False);
        }
        [UnityTest]public IEnumerator P04_A06_SaturatedRayAndInvalidDirectionsFailClosed()
        {
            yield return new EnterPlayMode();Setup();Target("interaction_dense",new Vector3(0,0,2));for(var i=0;i<16;i++)Box(new Vector3(0,0,.2f+i*.1f),true);Sync();Assert.That(raycaster.TrySelect(forward,out _),Is.False);Assert.That(raycaster.QuerySaturated,Is.True);Assert.That(raycaster.TrySelect(new Ray(Vector3.zero,Vector3.zero),out _),Is.False);Assert.That(raycaster.TrySelect(new Ray(new Vector3(float.NaN,0,0),Vector3.forward),out _),Is.False);
        }
        [UnityTearDown]public IEnumerator After()
        {
            foreach(var definition in definitions)if(definition)Object.Destroy(definition);definitions.Clear();if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Interaction ray fixture unload timeout.");yield return null;}Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
