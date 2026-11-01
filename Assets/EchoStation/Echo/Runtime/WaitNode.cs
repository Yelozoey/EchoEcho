namespace EchoStation.Echo
{
    public sealed class WaitNode:IBehaviorNode
    {
        private MovementLease movement;
        public void Enter(BehaviorContext context){movement=context.Movement;movement.Stop();}
        public NodeStatus Tick(float deltaSeconds)
        {
            if(!movement.IsValid)return NodeStatus.Failure;
            if(movement.State==NavigationState.Moving)movement.Stop();return NodeStatus.Running;
        }
        public void Exit()=>movement?.Stop();
    }
}
