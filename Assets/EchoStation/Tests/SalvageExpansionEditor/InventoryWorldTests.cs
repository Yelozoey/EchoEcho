using System;
using System.Collections;
using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage;
using EchoStation.Tests.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageExpansion.Tests
{
    public sealed class InventoryWorldTests
    {
        static Scene old,scene;static GameObject root,player;static ExpansionWorld world;static PlayerInputReader input;static SalvageMotor motor;static SyntheticInputScope devices;static bool background;static float scale;
        public static ExpansionWorld FixtureWorld=>world;
        public static GameObject FixtureRoot=>root;
        public static IEnumerator CreateFixture()=>Boot();
        static IEnumerator Frame(){int before=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();while(Time.frameCount<=before){if(timer.Elapsed.TotalSeconds>5)Assert.Fail("Actual frame timeout");yield return null;}}
        static IEnumerator KeyPress(Key key){InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState());yield return Frame();InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState(key));yield return Frame();InputSystem.QueueStateEvent(devices.Keyboard,new KeyboardState());yield return Frame();}
        static IEnumerator Boot()
        {
            background=Application.runInBackground;Application.runInBackground=true;scale=Time.timeScale;old=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("Expansion_B04_"+Guid.NewGuid().ToString("N"));SceneManager.SetActiveScene(scene);devices=new SyntheticInputScope();root=new GameObject("B04_OwnedWorld");
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.transform.SetParent(root.transform);floor.transform.position=new Vector3(18,-.1f,12);floor.transform.localScale=new Vector3(36,.2f,24);floor.AddComponent<SalvageDropSurface>();
            var container=new GameObject("Items").transform;container.SetParent(root.transform);var actors=new ExpansionItemActor[7];var positions=new[]{new Vector3(5,.3f,14),new Vector3(6,.3f,14),new Vector3(7,.3f,14),new Vector3(30,.3f,12),new Vector3(31,.3f,18),new Vector3(18,.3f,5),new Vector3(16,.3f,18)};
            for(int i=0;i<actors.Length;i++){var actor=GameObject.CreatePrimitive(PrimitiveType.Cube);actor.name=ExpansionCatalog.Items[i].Name;actor.transform.SetParent(container);actor.transform.position=positions[i];actor.transform.localScale=Vector3.one*.55f;actors[i]=actor.AddComponent<ExpansionItemActor>();actors[i].Configure(ExpansionCatalog.Items[i].Id);}
            player=new GameObject("ExpansionPlayer");player.SetActive(false);player.transform.SetParent(root.transform);player.transform.position=new Vector3(5,.15f,12);var controller=player.AddComponent<CharacterController>();controller.height=1.75f;controller.center=new Vector3(0,.875f,0);controller.radius=.3f;controller.skinWidth=.03f;controller.stepOffset=.25f;
            input=player.AddComponent<PlayerInputReader>();var view=new GameObject("ViewCamera");view.transform.SetParent(player.transform,false);view.transform.localPosition=new Vector3(0,1.6f,0);view.AddComponent<Camera>();motor=player.AddComponent<SalvageMotor>();motor.Configure(view.transform);world=player.AddComponent<ExpansionWorld>();world.Configure(motor,container,actors);player.SetActive(true);yield return Frame();world.Initialize();motor.RestorePose(new Vector3(5,.15f,12),90,0);input.SetApplicationFocus(true);input.SetPointerCaptured(true);Physics.SyncTransforms();
        }
        static void Aim(string id)
        {
            var target=world.Actors[id];var position=new Vector3(target.transform.position.x-2,.15f,target.transform.position.z);var delta=target.Body.bounds.center-(position+Vector3.up*1.6f);float pitch=-Mathf.Asin(delta.normalized.y)*Mathf.Rad2Deg;motor.RestorePose(position,90,pitch);Physics.SyncTransforms();
        }
        [Test]public void SevenDefinitionsAndInitialLoadoutHaveNoHiddenCapacityExemption()
        {
            var s=ExpansionCatalog.NewInventory();Assert.That(s.Definitions.Count,Is.EqualTo(7));Assert.That(s.Board().OccupiedCells.Count,Is.EqualTo(8));Assert.That(s.Get(ExpansionCatalog.Core).Location,Is.EqualTo(InventoryItemLocation.World));Assert.That(s.TryStow(ExpansionCatalog.Core,3,0,1,out _),Is.True);Assert.That(s.Board().OccupiedCells.Count,Is.EqualTo(14));Assert.That(s.Definitions.Count(d=>d.Kind==InventoryItemKind.Tool),Is.EqualTo(2));
        }
        [UnityTest]public IEnumerator ActualEAndRaycastCollectOnlyOwnedVisibleWorldItem()
        {
            yield return new EnterPlayMode();yield return Boot();Aim(ExpansionCatalog.Samples);yield return KeyPress(Key.E);
            Assert.That(world.Inventory.Get(ExpansionCatalog.Samples).Location,Is.EqualTo(InventoryItemLocation.Backpack));Assert.That(world.Actors[ExpansionCatalog.Samples].gameObject.activeSelf,Is.False);Assert.That(world.Actors[ExpansionCatalog.Samples].Body.enabled,Is.False);long revision=world.Inventory.Revision;Assert.That(world.TryPickupFromView(),Is.False);Assert.That(world.Inventory.Revision,Is.EqualTo(revision));
            Aim(ExpansionCatalog.Core);var foreign=GameObject.CreatePrimitive(PrimitiveType.Cube);foreign.transform.SetParent(root.transform);foreign.transform.position=motor.View.position+motor.View.forward;foreign.transform.localScale=Vector3.one*.4f;foreign.AddComponent<ExpansionItemActor>().Configure(ExpansionCatalog.Core);Physics.SyncTransforms();
            Assert.That(world.TryPickupFromView(),Is.False);Assert.That(world.Message,Does.Contain("不是当前"));Assert.That(world.Inventory.Revision,Is.EqualTo(revision));Object.Destroy(foreign);
        }
        [UnityTest]public IEnumerator CoreGuardAndNoSpaceFailWithoutHidingSource()
        {
            yield return new EnterPlayMode();yield return Boot();Aim(ExpansionCatalog.Core);long revision=world.Inventory.Revision;
            Assert.That(world.TryPickupFromView(),Is.False);Assert.That(world.Actors[ExpansionCatalog.Core].gameObject.activeSelf,Is.True);Assert.That(world.Inventory.Revision,Is.EqualTo(revision));
            world.PickupGuard=id=>"";Assert.That(world.TryPickupFromView(),Is.True);Assert.That(world.Inventory.Get(ExpansionCatalog.Core).Location,Is.EqualTo(InventoryItemLocation.Backpack));
            Aim(ExpansionCatalog.Instrument);revision=world.Inventory.Revision;Assert.That(world.TryPickupFromView(),Is.False);Assert.That(world.Inventory.Revision,Is.EqualTo(revision));Assert.That(world.Actors[ExpansionCatalog.Instrument].gameObject.activeSelf,Is.True);
        }
        [UnityTest]public IEnumerator EquipViewAndPausedUiDropUseActualSafeSurfaceAndCanBeRecovered()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(world.Backpack.Open(),Is.True);world.Backpack.Select(0,0);world.Backpack.Canvas.transform.Find("Panel/Equip").GetComponent<Button>().onClick.Invoke();yield return Frame();
            var actor=world.Actors[ExpansionCatalog.Wrench];Assert.That(world.Inventory.HeldId,Is.EqualTo(ExpansionCatalog.Wrench));Assert.That(actor.gameObject.activeSelf,Is.True);Assert.That(actor.Body.enabled,Is.False);Assert.That(actor.transform.IsChildOf(motor.View),Is.True);Assert.That(motor.Speed,Is.EqualTo(3.3f).Within(.001f));
            world.Backpack.Canvas.transform.Find("Panel/Drop").GetComponent<Button>().onClick.Invoke();yield return Frame();Assert.That(world.Inventory.Get(ExpansionCatalog.Wrench).Location,Is.EqualTo(InventoryItemLocation.World));Assert.That(actor.Body.enabled,Is.True);Assert.That(actor.transform.IsChildOf(motor.View),Is.False);Assert.That(motor.Speed,Is.EqualTo(3.5f));Assert.That(Time.timeScale,Is.Zero);
            world.Backpack.Close();Aim(ExpansionCatalog.Wrench);Assert.That(world.TryPickupFromView(),Is.True);Assert.That(world.Inventory.Get(ExpansionCatalog.Wrench).Location,Is.EqualTo(InventoryItemLocation.Backpack));
        }
        [UnityTest]public IEnumerator BlockedDropLeavesItemAndActorUnchanged()
        {
            yield return new EnterPlayMode();yield return Boot();world.Inventory.TryEquip(ExpansionCatalog.Battery,out _);world.SyncViews();var actor=world.Actors[ExpansionCatalog.Battery];var block=GameObject.CreatePrimitive(PrimitiveType.Cube);block.transform.SetParent(root.transform);block.transform.position=new Vector3(6.2f,.5f,12);Physics.SyncTransforms();long revision=world.Inventory.Revision;Vector3 position=actor.transform.position;
            Assert.That(world.TryDrop(ExpansionCatalog.Battery),Is.False);Assert.That(world.Inventory.Revision,Is.EqualTo(revision));Assert.That(actor.transform.position,Is.EqualTo(position));Assert.That(actor.Body.enabled,Is.False);Object.Destroy(block);yield return Frame();Physics.SyncTransforms();Assert.That(world.TryDrop(ExpansionCatalog.Battery),Is.True);
        }
        [UnityTest]public IEnumerator BatchPreviewCancelAndConfirmationKeepEquipmentAndRejectStaleReplays()
        {
            yield return new EnterPlayMode();yield return Boot();Assert.That(world.Inventory.TryStow(ExpansionCatalog.Core,3,0,1,out _),Is.True);Assert.That(world.Inventory.TryStow(ExpansionCatalog.Samples,2,2,0,out _),Is.True);world.SyncViews();
            long revision=world.Inventory.Revision;Assert.That(world.BeginDelivery(),Is.True);Assert.That(world.Inventory.Revision,Is.EqualTo(revision));Assert.That(Time.timeScale,Is.Zero);world.CancelDelivery();Assert.That(world.Inventory.Get(ExpansionCatalog.Core).Location,Is.EqualTo(InventoryItemLocation.Backpack));
            Assert.That(world.BeginDelivery(),Is.True);world.Inventory.TryEquip(ExpansionCatalog.Wrench,out _);Assert.That(world.ConfirmDelivery(),Is.False);Assert.That(world.Inventory.Get(ExpansionCatalog.Core).Location,Is.EqualTo(InventoryItemLocation.Backpack));
            Assert.That(world.BeginDelivery(),Is.True);yield return KeyPress(Key.Enter);Assert.That(world.Inventory.Get(ExpansionCatalog.Core).Location,Is.EqualTo(InventoryItemLocation.Delivered));Assert.That(world.Inventory.Get(ExpansionCatalog.Samples).Location,Is.EqualTo(InventoryItemLocation.Delivered));Assert.That(world.Inventory.Get(ExpansionCatalog.Battery).Location,Is.EqualTo(InventoryItemLocation.Backpack));Assert.That(world.Inventory.HeldId,Is.EqualTo(ExpansionCatalog.Wrench));Assert.That(world.ConfirmDelivery(),Is.False);Assert.That(Time.timeScale,Is.EqualTo(scale));
        }
        [UnityTest]public IEnumerator DeliveryCannotConfirmAwayFromDockOrAfterFocusLoss()
        {
            yield return new EnterPlayMode();yield return Boot();world.Inventory.TryStow(ExpansionCatalog.Core,3,0,1,out _);world.SyncViews();Assert.That(world.BeginDelivery(),Is.True);motor.RestorePose(new Vector3(10,.15f,12),90,0);Assert.That(world.ConfirmDelivery(),Is.False);Assert.That(world.Inventory.Get(ExpansionCatalog.Core).Location,Is.EqualTo(InventoryItemLocation.Backpack));
            motor.RestorePose(new Vector3(5,.15f,12),90,0);Assert.That(world.BeginDelivery(),Is.True);input.SetApplicationFocus(false);yield return Frame();Assert.That(world.DeliveryPending,Is.False);Assert.That(world.ConfirmDelivery(),Is.False);Assert.That(Time.timeScale,Is.EqualTo(scale));
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {if(!Application.isPlaying)yield break;yield return DestroyFixture();yield return new ExitPlayMode();}
        public static IEnumerator DestroyFixture()
        {
            if(!Application.isPlaying)yield break;if(world){world.CancelDelivery();if(world.Backpack)world.Backpack.Close();if(world.Pause)world.Pause.enabled=false;}if(old.IsValid())SceneManager.SetActiveScene(old);if(scene.IsValid()&&scene.isLoaded){var op=SceneManager.UnloadSceneAsync(scene);while(op!=null&&!op.isDone)yield return null;}if(devices!=null){devices.Dispose();Assert.That(devices.IsRestored,Is.True);devices=null;}Time.timeScale=scale;Application.runInBackground=background;
        }
    }
}
