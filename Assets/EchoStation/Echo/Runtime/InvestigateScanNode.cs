using System;
using EchoStation.World;

namespace EchoStation.Echo
{
    public sealed class InvestigateScanNode:IBehaviorNode
    {
        private readonly EchoScanEngine scanner;private readonly InvestigationNode travel;
        private WorldTarget ready;private bool started;private ulong generation;private float waiting,elapsed;
        public InvestigateScanNode(EchoScanEngine sensor)
        {scanner=sensor??throw new ArgumentNullException(nameof(sensor));travel=new InvestigationNode(target=>ready=target);}
        public void Enter(BehaviorContext context)
        {ready=null;started=false;waiting=elapsed=0;scanner.Cancel();travel.Enter(context);}
        public NodeStatus Tick(float deltaSeconds)
        {
            if(!ready)
            {
                var state=travel.Tick(deltaSeconds);if(state==NodeStatus.Failure)return NodeStatus.Failure;if(state==NodeStatus.Running)return NodeStatus.Running;if(!ready)return NodeStatus.Failure;
            }
            if(!started)
            {
                waiting+=deltaSeconds;if(scanner.IsBusy||scanner.CooldownRemaining>0)return waiting>3?NodeStatus.Failure:NodeStatus.Running;
                if(!scanner.TryBegin(ready))return NodeStatus.Failure;started=true;generation=scanner.Generation;return NodeStatus.Running;
            }
            elapsed+=deltaSeconds;if(elapsed>4||generation!=scanner.Generation)return NodeStatus.Failure;
            if(scanner.Phase==ScanPhase.Completed)return scanner.LastReceipt!=null&&scanner.LastReceipt.TargetId==ready.Id?NodeStatus.Success:NodeStatus.Failure;
            return scanner.Phase==ScanPhase.Failed||scanner.Phase==ScanPhase.Cancelled?NodeStatus.Failure:NodeStatus.Running;
        }
        public void Exit()
        {travel.Exit();if(started&&scanner.Generation==generation&&scanner.IsBusy)scanner.Cancel();}
    }
}
