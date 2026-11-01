using System;
using EchoStation.World;

namespace EchoStation.Echo
{
    public sealed class WarnNode:IBehaviorNode
    {
        private readonly Func<WorldTarget,bool> visible;private WorldTarget target;private MovementLease movement;private float elapsed;private bool accepted;
        public WarnNode(Func<WorldTarget,bool> canPerceive)=>visible=canPerceive??throw new ArgumentNullException(nameof(canPerceive));
        public void Enter(BehaviorContext context)
        {movement=context.Movement;movement.Stop();target=context.Intent.Target;elapsed=0;accepted=ValidDanger()&&visible(target);}
        public NodeStatus Tick(float deltaSeconds)
        {
            if(!accepted||!movement.IsValid||!ValidDanger())return NodeStatus.Failure;
            if(movement.State==NavigationState.Moving)movement.Stop();if(!visible(target))return NodeStatus.Success;
            elapsed+=deltaSeconds;return elapsed>=1.5f?NodeStatus.Success:NodeStatus.Running;
        }
        public void Exit()=>movement?.Stop();
        private bool ValidDanger()=>target&&target.isActiveAndEnabled&&target.GetComponent<EchoTargetPermissions>()&&target.GetComponent<EchoTargetPermissions>().IsDanger;
    }
}
