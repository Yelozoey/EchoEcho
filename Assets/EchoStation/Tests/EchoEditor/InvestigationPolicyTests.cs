using System;
using System.Collections.Generic;
using EchoStation.Echo;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Echo
{
    public class InvestigationPolicyTests
    {
        private sealed class Port:IMovementPort
        {
            public Vector3 Position{get;set;}public NavigationState State{get;set;}public bool Accept=true;public int Requests,Stops;public Vector3 Goal;
            public bool TryMove(Vector3 position){Requests++;Goal=position;State=Accept?NavigationState.Moving:NavigationState.Unreachable;return Accept;}public void Cancel(){Stops++;State=NavigationState.Idle;}
        }
        private GameObject owner,approach;private WorldTarget target;private EchoTargetPermissions permissions;private IntentBehaviorTree tree;private IntentDispatcher dispatcher;private int arrivals;
        private Port Start(bool withPoint=true,bool accept=true)
        {
            owner=new GameObject("InvestigationFixture");owner.SetActive(false);target=owner.AddComponent<WorldTarget>();target.Configure("investigation_fixture","夹具");permissions=owner.AddComponent<EchoTargetPermissions>();permissions.Configure(true,false);
            if(withPoint){approach=new GameObject("ApprovedPoint");approach.transform.position=new Vector3(3,0,0);permissions.SetApproachPoint(approach.transform);}owner.SetActive(true);
            var registry=new TargetRegistry();registry.TryRegister(target,out _);var port=new Port{Accept=accept};tree=new IntentBehaviorTree(port,new Dictionary<IntentKind,Func<IBehaviorNode>>{{IntentKind.Investigate,()=>new InvestigationNode(t=>{Assert.That(t,Is.SameAs(target));arrivals++;})}});dispatcher=new IntentDispatcher(registry,tree);return port;
        }
        [Test]public void E07_A01_UsesApprovedGroundPointAndArrivesExactlyOnceAfterSettling()
        {
            var port=Start();Assert.That(dispatcher.TryDispatch(ScriptCommands.Investigate(target.Id),out _),Is.True);Assert.That(port.Goal,Is.EqualTo(approach.transform.position));tree.Tick(.1f);Assert.That(arrivals,Is.Zero);
            port.Position=port.Goal;port.State=NavigationState.Arrived;tree.Tick(.3f);Assert.That(arrivals,Is.Zero);tree.Tick(.31f);tree.Tick(2);Assert.That(arrivals,Is.EqualTo(1));Assert.That(tree.Status,Is.EqualTo(BehaviorTreeStatus.Success));Assert.That(owner.transform.position,Is.EqualTo(Vector3.zero));
        }
        [TestCase(false,true)][TestCase(true,false)]
        public void E07_A02_MissingPointOrRejectedPathCannotIssueArrival(bool point,bool path)
        {
            var port=Start(point,path);dispatcher.TryDispatch(ScriptCommands.Investigate(target.Id),out _);tree.Tick(.1f);Assert.That(tree.Status,Is.EqualTo(BehaviorTreeStatus.Failure));Assert.That(arrivals,Is.Zero);Assert.That(port.Requests,Is.EqualTo(point?1:0));
        }
        [TestCase(false)][TestCase(true)]public void E07_A03_DestroyedTargetOrRevokedPermissionStopsInvestigation(bool destroy)
        {
            var port=Start();dispatcher.TryDispatch(ScriptCommands.Investigate(target.Id),out _);if(destroy){Object.DestroyImmediate(owner);owner=null;}else permissions.Configure(false,false);
            tree.Tick(.1f);Assert.That(tree.Status,Is.EqualTo(BehaviorTreeStatus.Failure));Assert.That(port.State,Is.EqualTo(NavigationState.Idle));Assert.That(arrivals,Is.Zero);
        }
        [Test]public void E07_A04_CancelDoesNotReportArrivalAndTravelTimeoutIsBounded()
        {
            Start();dispatcher.TryDispatch(ScriptCommands.Investigate(target.Id),out _);tree.Cancel();tree.Tick(20);Assert.That(arrivals,Is.Zero);Assert.That(tree.Status,Is.EqualTo(BehaviorTreeStatus.Cancelled));
            dispatcher.TryDispatch(ScriptCommands.Investigate(target.Id),out _);tree.Tick(12.01f);Assert.That(tree.Status,Is.EqualTo(BehaviorTreeStatus.Failure));Assert.That(arrivals,Is.Zero);
        }
        [TearDown]public void After(){tree?.Dispose();tree=null;if(owner)Object.DestroyImmediate(owner);if(approach)Object.DestroyImmediate(approach);arrivals=0;}
    }
}
