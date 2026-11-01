using UnityEngine;
using UnityEngine.AI;

namespace EchoStation.Echo
{
    public enum NavigationState{Idle,Moving,Arrived,Unreachable}
    public enum NavigationFailure{None,NotBound,InvalidPosition,NoNearbySurface,IncompletePath,SetPathFailed}
    [RequireComponent(typeof(NavMeshAgent)),DisallowMultipleComponent]
    public sealed class EchoNavigation : MonoBehaviour,IMovementPort
    {
        private NavMeshAgent agent;
        private NavMeshPath path;
        public NavMeshAgent Agent=>agent?agent:agent=GetComponent<NavMeshAgent>();
        public NavigationState State{get;private set;}
        public NavigationFailure Failure{get;private set;}
        public Vector3 Goal{get;private set;}
        public Vector3 Position=>transform.position;
        public bool IsBound=>Agent&&Agent.enabled&&Agent.isOnNavMesh;
        public bool TryBind(Vector3 position)
        {
            if(!Finite(position))return Reject(NavigationFailure.InvalidPosition);
            var filter=new NavMeshQueryFilter{agentTypeID=Agent.agentTypeID,areaMask=Agent.areaMask};
            if(!NavMesh.SamplePosition(position,out var hit,.6f,filter))return Reject(NavigationFailure.NoNearbySurface);
            Cancel();Agent.enabled=false;transform.position=hit.position;Agent.enabled=true;
            if(!Agent.isOnNavMesh||!Agent.Warp(hit.position)){Agent.enabled=false;return Reject(NavigationFailure.NotBound);}
            State=NavigationState.Idle;Failure=NavigationFailure.None;Goal=hit.position;return true;
        }
        public bool TryMove(Vector3 destination)
        {
            if(!IsBound)return Reject(NavigationFailure.NotBound);if(!Finite(destination))return Reject(NavigationFailure.InvalidPosition);
            var filter=new NavMeshQueryFilter{agentTypeID=Agent.agentTypeID,areaMask=Agent.areaMask};
            if(!NavMesh.SamplePosition(destination,out var hit,.6f,filter))return Reject(NavigationFailure.NoNearbySurface);
            if(path==null)path=new NavMeshPath();
            if(!Agent.CalculatePath(hit.position,path)||path.status!=NavMeshPathStatus.PathComplete)return Reject(NavigationFailure.IncompletePath);
            Goal=hit.position;Failure=NavigationFailure.None;
            if(PlanarDistance(transform.position,Goal)<=Agent.stoppingDistance){Cancel();State=NavigationState.Arrived;return true;}
            Agent.isStopped=false;if(!Agent.SetPath(path))return Reject(NavigationFailure.SetPathFailed);State=NavigationState.Moving;return true;
        }
        public void Cancel()
        {
            if(IsBound){Agent.isStopped=true;Agent.ResetPath();Agent.velocity=Vector3.zero;}
            State=NavigationState.Idle;Failure=NavigationFailure.None;
        }
        private bool Reject(NavigationFailure reason){Cancel();State=NavigationState.Unreachable;Failure=reason;return false;}
        private void Update()
        {
            if(State!=NavigationState.Moving)return;
            if(!IsBound){Reject(NavigationFailure.NotBound);return;}if(Agent.pathPending)return;
            if(PlanarDistance(transform.position,Goal)<=Agent.stoppingDistance+.03f&&Agent.velocity.sqrMagnitude<.01f)
            {Cancel();State=NavigationState.Arrived;return;}
            if(Agent.pathStatus!=NavMeshPathStatus.PathComplete)Reject(NavigationFailure.IncompletePath);
        }
        private void OnDisable(){Cancel();if(Agent)Agent.enabled=false;}
        private static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
        private static float PlanarDistance(Vector3 a,Vector3 b){a.y=b.y=0;return Vector3.Distance(a,b);}
    }
}
