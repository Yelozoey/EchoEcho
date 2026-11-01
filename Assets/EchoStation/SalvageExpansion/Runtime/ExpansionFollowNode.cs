using System;
using EchoStation.Echo;
using UnityEngine;
namespace EchoStation.SalvageExpansion
{
    /// <summary>Follow with a side-step when the player walks back toward the rover.</summary>
    sealed class ExpansionFollowNode:IBehaviorNode
    {
        readonly ExpansionWorld world;readonly FollowNode follow;BehaviorContext context;MovementLease movement;float yielding;
        public ExpansionFollowNode(ExpansionWorld player){world=player;follow=new FollowNode(()=>world?world.transform.position:(Vector3?)null);}
        public void Enter(BehaviorContext value){context=value;movement=value.Movement;yielding=0;follow.Enter(value);}
        public NodeStatus Tick(float seconds)
        {
            if(!movement.IsValid||!world)return NodeStatus.Failure;yielding-=seconds;if(yielding>0&&movement.State==NavigationState.Moving)return NodeStatus.Running;
            var delta=world.transform.position-movement.Position;delta.y=0;var wanted=world.Motor.DesiredHorizontalVelocity;
            if(delta.magnitude<1.8f&&wanted.sqrMagnitude>.001f&&Vector3.Dot(wanted.normalized,-delta.normalized)>.2f)
            {var side=Vector3.Cross(Vector3.up,wanted.normalized)*1.5f;if(Move(movement.Position+side)||Move(movement.Position-side)||Move(movement.Position+wanted.normalized*1.5f)){yielding=1.2f;return NodeStatus.Running;}}
            return follow.Tick(seconds);
        }
        bool Move(Vector3 goal)=>Vector3.Distance(goal,world.transform.position)>1.1f&&movement.TryMove(goal)&&movement.State==NavigationState.Moving;
        public void Exit(){yielding=0;follow.Exit();}
    }
}
