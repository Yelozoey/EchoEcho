using System;
using System.Collections.Generic;
using EchoStation.Echo;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;

namespace EchoStation.Tests.Echo
{
    public class FollowPolicyTests
    {
        private sealed class Port:IMovementPort
        {
            public Vector3 Position{get;set;}public NavigationState State{get;private set;}public bool Accept=true;public int Stops;public float Now;public Vector3 Goal;
            public readonly List<float> Requests=new List<float>();
            public bool TryMove(Vector3 destination){Requests.Add(Now);Goal=destination;State=Accept?NavigationState.Moving:NavigationState.Unreachable;return Accept;}
            public void Cancel(){Stops++;State=NavigationState.Idle;}
        }
        private static IntentBehaviorTree Start(Port port,FollowNode node)
        {
            var tree=new IntentBehaviorTree(port,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Follow,()=>node}});var dispatcher=new IntentDispatcher(new TargetRegistry(),tree);Assert.That(dispatcher.TryDispatch(ScriptCommands.Follow(),out _),Is.True);return tree;
        }
        [Test]public void E06_A01_StartStopHysteresisHoldsNearBoundaryAndLeavesPersonalSpace()
        {
            var port=new Port();Vector3? target=new Vector3(2.2f,0,0);using var tree=Start(port,new FollowNode(()=>target));tree.Tick(0);Assert.That(port.Requests.Count,Is.Zero);
            target=new Vector3(2.21f,0,0);tree.Tick(.01f);Assert.That(port.Requests.Count,Is.EqualTo(1));Assert.That(port.Goal.x,Is.EqualTo(.51f).Within(.0001f));
            port.Position=new Vector3(.6f,0,0);tree.Tick(.01f);Assert.That(port.State,Is.EqualTo(NavigationState.Idle));
            target=new Vector3(2.79f,0,0);tree.Tick(.3f);Assert.That(port.Requests.Count,Is.EqualTo(1));target=new Vector3(2.81f,0,0);tree.Tick(.01f);Assert.That(port.Requests.Count,Is.EqualTo(2));
        }
        [Test]public void E06_A02_StationaryTargetDoesNotRepathAndMovingTargetObeysMinimumInterval()
        {
            var port=new Port();Vector3? target=new Vector3(10,0,0);using var tree=Start(port,new FollowNode(()=>target));tree.Tick(0);
            for(var i=0;i<100;i++){port.Now+=.02f;tree.Tick(.02f);}Assert.That(port.Requests.Count,Is.EqualTo(1));
            for(var i=0;i<100;i++){target=target.Value+Vector3.right*.04f;port.Now+=.02f;tree.Tick(.02f);}
            Assert.That(port.Requests.Count,Is.InRange(5,8));for(var i=1;i<port.Requests.Count;i++)Assert.That(port.Requests[i]-port.Requests[i-1],Is.GreaterThanOrEqualTo(.299f));
        }
        [Test]public void E06_A03_UnreachableRetriesAreThrottledAndMissingTargetSafelyExits()
        {
            var port=new Port{Accept=false};Vector3? target=new Vector3(10,0,0);using var tree=Start(port,new FollowNode(()=>target));tree.Tick(0);
            for(var i=0;i<9;i++)tree.Tick(.05f);Assert.That(port.Requests.Count,Is.EqualTo(1));tree.Tick(.31f);Assert.That(port.Requests.Count,Is.EqualTo(2));
            target=null;tree.Tick(.01f);Assert.That(tree.Status,Is.EqualTo(BehaviorTreeStatus.Failure));Assert.That(port.State,Is.EqualTo(NavigationState.Idle));
        }
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)]
        public void E06_A04_NonfiniteTargetCannotIssueMovement(float invalid)
        {
            var port=new Port();using var tree=Start(port,new FollowNode(()=>new Vector3(invalid,0,0)));tree.Tick(.1f);Assert.That(tree.Status,Is.EqualTo(BehaviorTreeStatus.Failure));Assert.That(port.Requests.Count,Is.Zero);
        }
        [Test]public void E06_A05_InvalidBudgetsAreRejected()
        {
            Assert.Throws<ArgumentException>(()=>new FollowTuning(1.7f,1.7f,.3f,.25f,.75f));Assert.Throws<ArgumentException>(()=>new FollowTuning(2.2f,.5f,.3f,.25f,.75f));
            Assert.Throws<ArgumentException>(()=>new FollowTuning(2.2f,1.7f,.001f,.25f,.75f));Assert.Throws<ArgumentException>(()=>new FollowTuning(2.2f,1.7f,.3f,.25f,.1f));
            Assert.Throws<ArgumentException>(()=>new FollowNode(()=>Vector3.zero,default(FollowTuning)));
        }
    }
}
