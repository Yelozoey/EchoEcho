using System;
using System.Collections.Generic;
using EchoStation.Echo;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Echo
{
    public class WarnNodeTests
    {
        private sealed class Port:IMovementPort
        {public Vector3 Position=>Vector3.zero;public NavigationState State{get;set;}=NavigationState.Moving;public int Moves,Stops;public bool TryMove(Vector3 point){Moves++;return true;}public void Cancel(){Stops++;State=NavigationState.Idle;}}
        private GameObject owner;private WorldTarget target;private IntentBehaviorTree root;private IntentDispatcher dispatcher;private bool visible=true;
        private Port Start()
        {
            owner=new GameObject("WarningFixture");owner.SetActive(false);target=owner.AddComponent<WorldTarget>();target.Configure("warning_fixture","危险夹具");owner.AddComponent<EchoTargetPermissions>().Configure(false,true);owner.SetActive(true);var registry=new TargetRegistry();registry.TryRegister(target,out _);
            var port=new Port();root=new IntentBehaviorTree(port,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Warn,()=>new WarnNode(t=>visible)}});dispatcher=new IntentDispatcher(registry,root);Assert.That(dispatcher.TryDispatch(ScriptCommands.Warn(target.Id),out _),Is.True);return port;
        }
        [Test]public void E10_B01_WarningImmediatelyStopsAndHoldsForOnePointFiveSecondsWithoutMove()
        {
            var port=Start();Assert.That(port.State,Is.EqualTo(NavigationState.Idle));root.Tick(1.49f);Assert.That(root.CurrentMode,Is.EqualTo(IntentKind.Warn));Assert.That(root.Status,Is.EqualTo(BehaviorTreeStatus.Running));root.Tick(.02f);Assert.That(root.Status,Is.EqualTo(BehaviorTreeStatus.Success));Assert.That(port.Moves,Is.Zero);
        }
        [TestCase(false)][TestCase(true)]public void E10_B02_TargetDisableOrDangerPermissionRemovalFailsSafely(bool disable)
        {
            var port=Start();if(disable)owner.SetActive(false);else owner.GetComponent<EchoTargetPermissions>().Configure(false,false);root.Tick(.1f);Assert.That(root.Status,Is.EqualTo(BehaviorTreeStatus.Failure));Assert.That(port.Moves,Is.Zero);Assert.That(port.State,Is.EqualTo(NavigationState.Idle));
        }
        [Test]public void E10_B03_NoLongerPerceivedEndsWarningAndCancelIsSafe()
        {
            var port=Start();root.Tick(.3f);visible=false;root.Tick(.1f);Assert.That(root.Status,Is.EqualTo(BehaviorTreeStatus.Success));root.Cancel();Assert.That(port.Moves,Is.Zero);
        }
        [TearDown]public void After(){root?.Dispose();root=null;if(owner)Object.DestroyImmediate(owner);visible=true;}
    }
}
