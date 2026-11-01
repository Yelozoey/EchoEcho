using System;
using UnityEngine;

namespace EchoStation.Echo
{
    public readonly struct FollowTuning
    {
        public float StartDistance{get;}public float DesiredDistance{get;}public float RepathSeconds{get;}public float TargetMoveThreshold{get;}public float RetrySeconds{get;}
        public FollowTuning(float start,float desired,float repath,float displacement,float retry)
        {
            if(!float.IsFinite(start)||!float.IsFinite(desired)||!float.IsFinite(repath)||!float.IsFinite(displacement)||!float.IsFinite(retry)||
                desired<1||start<=desired+.2f||repath<.05f||displacement<.01f||retry<repath)throw new ArgumentException("Invalid follow hysteresis or scheduling budget.");
            StartDistance=start;DesiredDistance=desired;RepathSeconds=repath;TargetMoveThreshold=displacement;RetrySeconds=retry;
        }
        public static FollowTuning Default=>new FollowTuning(2.2f,1.7f,.3f,.25f,.75f);
    }
    public sealed class FollowNode:IBehaviorNode
    {
        private readonly Func<Vector3?> target;private readonly FollowTuning tuning;
        private MovementLease movement;private float elapsed,nextRequest;private bool pursuing;private Vector3 lastTarget;
        public FollowNode(Func<Vector3?> targetPosition,FollowTuning? settings=null)
        {
            target=targetPosition??throw new ArgumentNullException(nameof(targetPosition));var value=settings??FollowTuning.Default;
            tuning=new FollowTuning(value.StartDistance,value.DesiredDistance,value.RepathSeconds,value.TargetMoveThreshold,value.RetrySeconds);
        }
        public void Enter(BehaviorContext context)
        {movement=context.Movement;elapsed=nextRequest=0;pursuing=false;movement.Stop();}
        public NodeStatus Tick(float deltaSeconds)
        {
            elapsed+=deltaSeconds;var position=target();
            if(!movement.IsValid||!position.HasValue||!Finite(position.Value)){movement.Stop();return NodeStatus.Failure;}
            var player=position.Value;var delta=player-movement.Position;delta.y=0;var distance=delta.magnitude;
            if(distance<=tuning.DesiredDistance+.2f||(!pursuing&&distance<=tuning.StartDistance))
            {if(pursuing)movement.Stop();pursuing=false;return NodeStatus.Running;}
            if(elapsed<nextRequest)return NodeStatus.Running;
            var displacement=player-lastTarget;displacement.y=0;
            if(pursuing&&movement.State==NavigationState.Moving&&displacement.sqrMagnitude<tuning.TargetMoveThreshold*tuning.TargetMoveThreshold)return NodeStatus.Running;
            var goal=player-delta.normalized*tuning.DesiredDistance;
            pursuing=movement.TryMove(goal);lastTarget=player;nextRequest=elapsed+(pursuing?tuning.RepathSeconds:tuning.RetrySeconds);return NodeStatus.Running;
        }
        public void Exit(){movement?.Stop();pursuing=false;}
        private static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
    }
}
