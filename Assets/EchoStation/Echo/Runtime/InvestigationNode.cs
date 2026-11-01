using System;
using EchoStation.World;
using UnityEngine;

namespace EchoStation.Echo
{
    public sealed class InvestigationNode:IBehaviorNode
    {
        private readonly Action<WorldTarget> arrived;
        private WorldTarget target;private EchoTargetPermissions permissions;private MovementLease movement;private Vector3 destination;
        private bool accepted,atDestination,completed;private float travel,settling;
        public InvestigationNode(Action<WorldTarget> onArrived=null)=>arrived=onArrived;
        public void Enter(BehaviorContext context)
        {
            movement=context.Movement;target=context.Intent.Target;permissions=target?target.GetComponent<EchoTargetPermissions>():null;
            accepted=atDestination=completed=false;travel=settling=0;
            if(!ValidTarget())return;destination=permissions.ApproachPoint.position;accepted=movement.TryMove(destination);
        }
        public NodeStatus Tick(float deltaSeconds)
        {
            if(completed)return NodeStatus.Success;
            if(!accepted||!movement.IsValid||!ValidTarget())return NodeStatus.Failure;
            travel+=deltaSeconds;if(!atDestination)
            {
                if(movement.State==NavigationState.Unreachable)return NodeStatus.Failure;
                var distance=movement.Position-destination;distance.y=0;
                if(movement.State==NavigationState.Arrived||distance.sqrMagnitude<=.2f*.2f){atDestination=true;movement.Stop();}
                else return travel>=12?NodeStatus.Failure:NodeStatus.Running;
            }
            settling+=deltaSeconds;if(settling<.6f)return NodeStatus.Running;
            completed=true;arrived?.Invoke(target);return NodeStatus.Success;
        }
        public void Exit()=>movement?.Stop();
        private bool ValidTarget()=>target&&target.isActiveAndEnabled&&permissions&&permissions.CanInvestigate&&permissions.ApproachPoint;
    }
}
