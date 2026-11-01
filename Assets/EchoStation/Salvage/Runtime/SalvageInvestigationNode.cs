using System;
using EchoStation.Echo;
using EchoStation.World;
using UnityEngine;
namespace EchoStation.Salvage
{
    public sealed class SalvageInvestigationNode:IBehaviorNode
    {
        readonly Func<WorldTarget,NodeStatus> arrived;MovementLease movement;SalvageTargetView target;Vector3 goal,lastProgress;float elapsed,settling,stalled;bool accepted,atGoal;
        public SalvageInvestigationNode(Func<WorldTarget,NodeStatus> sensor)=>arrived=sensor;
        public void Enter(BehaviorContext context){movement=context.Movement;target=context.Intent.Target?context.Intent.Target.GetComponent<SalvageTargetView>():null;elapsed=settling=stalled=0;atGoal=false;lastProgress=movement.Position;accepted=Valid()&&Move();}
        bool Valid()=>target&&target.Allows(IntentKind.Investigate)&&target.GetComponent<EchoTargetPermissions>().ApproachPoint;
        bool Move(){goal=target.GetComponent<EchoTargetPermissions>().ApproachPoint.position;return movement.TryMove(goal);}
        public NodeStatus Tick(float dt)
        {
            if(!accepted||!movement.IsValid||!Valid())return NodeStatus.Failure;elapsed+=dt;if(elapsed>30)return NodeStatus.Failure;
            var desired=target.GetComponent<EchoTargetPermissions>().ApproachPoint.position;
            if(Vector3.Distance(desired,goal)>.25f){atGoal=false;settling=stalled=0;accepted=Move();if(!accepted)return NodeStatus.Failure;}
            if(!atGoal)
            {
                var delta=movement.Position-goal;delta.y=0;
                if(delta.sqrMagnitude<=.2f*.2f||movement.State==NavigationState.Arrived){atGoal=true;movement.Stop();}
                else
                {
                    if(movement.State==NavigationState.Unreachable)return NodeStatus.Failure;if(movement.State==NavigationState.Idle&&!Move())return NodeStatus.Failure;
                    var progress=movement.Position-lastProgress;progress.y=0;if(progress.magnitude>.08f){stalled=0;lastProgress=movement.Position;}else stalled+=dt;
                    return stalled>=3?NodeStatus.Failure:NodeStatus.Running;
                }
            }
            settling+=dt;return settling<.6f?NodeStatus.Running:arrived?.Invoke(target.World)??NodeStatus.Success;
        }
        public void Exit()=>movement?.Stop();
    }
}
