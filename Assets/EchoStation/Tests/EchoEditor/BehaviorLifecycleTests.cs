using System;
using System.Collections.Generic;
using EchoStation.Echo;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Echo
{
    public class BehaviorLifecycleTests
    {
        private sealed class Port:IMovementPort
        {
            public int Moves,Stops;public Vector3 Position=>Vector3.zero;public NavigationState State=>NavigationState.Idle;
            public bool TryMove(Vector3 destination){Moves++;return true;}public void Cancel()=>Stops++;
        }
        private sealed class Node:IBehaviorNode
        {
            public int Enters,Ticks,Exits;public BehaviorContext Context;public NodeStatus Result=NodeStatus.Running;public string ThrowAt;public Action OnEnter;
            public void Enter(BehaviorContext context){Enters++;Context=context;OnEnter?.Invoke();if(ThrowAt=="enter")throw new InvalidOperationException("enter fault");}
            public NodeStatus Tick(float delta){Ticks++;if(ThrowAt=="tick")throw new InvalidOperationException("tick fault");return Result;}
            public void Exit(){Exits++;if(ThrowAt=="exit")throw new InvalidOperationException("exit fault");}
        }
        private readonly List<GameObject> created=new List<GameObject>();
        private IntentDispatcher Dispatcher(IntentBehaviorTree tree)
        {
            var registry=new TargetRegistry();var go=new GameObject("LifecycleTarget");go.SetActive(false);created.Add(go);var target=go.AddComponent<WorldTarget>();target.Configure("target_01","夹具");go.AddComponent<EchoTargetPermissions>().Configure(true,true);registry.TryRegister(target,out _);return new IntentDispatcher(registry,tree);
        }
        [Test]public void E05_01_FourModeSwitchesExitOnceAndOldLeaseCannotWriteNewMovement()
        {
            var port=new Port();var nodes=new[]{new Node(),new Node(),new Node(),new Node()};
            using var tree=new IntentBehaviorTree(port,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Follow,()=>nodes[0]},{IntentKind.Investigate,()=>nodes[1]},{IntentKind.Wait,()=>nodes[2]},{IntentKind.Warn,()=>nodes[3]}});var dispatcher=Dispatcher(tree);
            var requests=new[]{ScriptCommands.Follow(),ScriptCommands.Investigate("target_01"),ScriptCommands.Wait(),ScriptCommands.Warn("target_01")};
            for(var i=0;i<requests.Length;i++)
            {
                Assert.That(dispatcher.TryDispatch(requests[i],out _),Is.True);tree.Tick(.1f);Assert.That(nodes[i].Enters,Is.EqualTo(1));Assert.That(nodes[i].Context.Movement.TryMove(Vector3.one*i),Is.True);
                for(var j=0;j<i;j++){Assert.That(nodes[j].Exits,Is.EqualTo(1));Assert.That(nodes[j].Ticks,Is.EqualTo(1));Assert.That(nodes[j].Context.Movement.TryMove(Vector3.zero)||nodes[j].Context.Movement.Stop(),Is.False);}
            }
            Assert.That(port.Moves,Is.EqualTo(4));Assert.That(port.Stops,Is.EqualTo(3));Assert.That(tree.CurrentMode,Is.EqualTo(IntentKind.Warn));
        }
        [TestCase(NodeStatus.Success)][TestCase(NodeStatus.Failure)]
        public void E05_02_CompletionOrFailureReleasesMovementAndExitsExactlyOnce(NodeStatus outcome)
        {
            var port=new Port();var node=new Node{Result=outcome};using var tree=new IntentBehaviorTree(port,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Follow,()=>node}});var dispatcher=Dispatcher(tree);
            Assert.That(dispatcher.TryDispatch(ScriptCommands.Follow(),out _),Is.True);tree.Tick(.1f);tree.Tick(.1f);tree.Cancel();tree.Cancel();
            Assert.That(node.Ticks,Is.EqualTo(1));Assert.That(node.Exits,Is.EqualTo(1));Assert.That(port.Stops,Is.EqualTo(1));Assert.That(node.Context.Movement.IsValid,Is.False);Assert.That(tree.Status,Is.EqualTo(outcome==NodeStatus.Success?BehaviorTreeStatus.Success:BehaviorTreeStatus.Failure));
        }
        [Test]public void E05_03_CancelIsIdempotentAndLateMoveAndStopAreDenied()
        {
            var port=new Port();var node=new Node();using var tree=new IntentBehaviorTree(port,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Follow,()=>node}});var dispatcher=Dispatcher(tree);
            dispatcher.TryDispatch(ScriptCommands.Follow(),out _);var late=node.Context.Movement;tree.Cancel();tree.Cancel();Assert.That(node.Exits,Is.EqualTo(1));Assert.That(port.Stops,Is.EqualTo(1));Assert.That(late.TryMove(Vector3.one)||late.Stop(),Is.False);Assert.That(tree.Status,Is.EqualTo(BehaviorTreeStatus.Cancelled));
        }
        [Test]public void E05_04_MissingOrNullFactoryKeepsCurrentBehaviorAndSpeechDoesNotSwitchIt()
        {
            var port=new Port();var node=new Node();var speech=new List<string>();using var tree=new IntentBehaviorTree(port,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Follow,()=>node},{IntentKind.Wait,()=>null}},(text,refusal)=>speech.Add(text));var dispatcher=Dispatcher(tree);
            dispatcher.TryDispatch(ScriptCommands.Follow(),out _);Assert.That(dispatcher.TryDispatch(ScriptCommands.Wait(),out _),Is.False);Assert.That(dispatcher.TryDispatch(ScriptCommands.Investigate("target_01"),out _),Is.False);
            Assert.That(dispatcher.TryDispatch(ScriptCommands.Answer("记录已核对。"),out _),Is.True);Assert.That(dispatcher.TryDispatch(ScriptCommands.Refuse("我不能替你操作机关。"),out _),Is.True);tree.Tick(.1f);
            Assert.That(node.Exits,Is.Zero);Assert.That(node.Ticks,Is.EqualTo(1));Assert.That(tree.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(speech.Count,Is.EqualTo(2));
        }
        [TestCase("enter")][TestCase("tick")][TestCase("exit")]
        public void E05_05_NodeExceptionsRevokeAuthorityAndStopMovement(string stage)
        {
            var port=new Port();var node=new Node{ThrowAt=stage};using var tree=new IntentBehaviorTree(port,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Follow,()=>node}});var dispatcher=Dispatcher(tree);
            var accepted=dispatcher.TryDispatch(ScriptCommands.Follow(),out _);if(stage=="enter")Assert.That(accepted,Is.False);else if(stage=="tick")tree.Tick(.1f);else tree.Cancel();
            Assert.That(tree.Status,Is.EqualTo(BehaviorTreeStatus.Failure));Assert.That(tree.LastError,Does.Contain(stage+" fault"));Assert.That(node.Context.Movement.IsValid,Is.False);Assert.That(port.Stops,Is.EqualTo(1));Assert.That(node.Exits,Is.EqualTo(1));
        }
        [Test]public void E05_06_CancelRequestedInsideEnterCleansUpAfterCallback()
        {
            var port=new Port();var node=new Node();using var tree=new IntentBehaviorTree(port,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Follow,()=>node}});node.OnEnter=tree.Cancel;var dispatcher=Dispatcher(tree);
            Assert.That(dispatcher.TryDispatch(ScriptCommands.Follow(),out _),Is.False);Assert.That(tree.Status,Is.EqualTo(BehaviorTreeStatus.Cancelled));Assert.That(node.Context.Movement.IsValid,Is.False);Assert.That(node.Exits,Is.EqualTo(1));Assert.That(port.Stops,Is.EqualTo(1));
        }
        [TearDown]public void After(){foreach(var go in created)if(go)Object.DestroyImmediate(go);created.Clear();}
    }
}
