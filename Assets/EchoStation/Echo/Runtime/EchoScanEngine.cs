using System;
using EchoStation.World;
using UnityEngine;

namespace EchoStation.Echo
{
    public enum ScanPhase{Idle,Aiming,Scanning,Completed,Failed,Cancelled}
    public enum ScanFailure{None,NotEcho,MissingTarget,Forbidden,InvalidData,OutOfRange,OutOfView,Obstructed,Busy,Cooldown}
    /// <summary>Main-thread sensor/state machine. Only this verified execution path commits the ledger.</summary>
    public sealed class EchoScanEngine
    {
        private readonly EchoRig rig;private readonly PhysicsScene physics;private readonly RaycastHit[] hits=new RaycastHit[16];
        private WorldTarget target;private Quaternion originalPose;private float clock,cooldownUntil,scanning;
        public EchoScanLedger Ledger{get;}
        public ScanPhase Phase{get;private set;}
        public ScanFailure Failure{get;private set;}
        public ScanFailure LastRequestFailure{get;private set;}
        public EchoScanReceipt LastReceipt{get;private set;}
        public bool IsBusy=>Phase==ScanPhase.Aiming||Phase==ScanPhase.Scanning;
        public float CooldownRemaining=>Mathf.Max(0,cooldownUntil-clock);
        public WorldTarget CurrentTarget=>target;
        public ulong Generation{get;private set;}
        public string LastOccluderName{get;private set;}
        public event Action<ScanPhase> PhaseChanged;
        public EchoScanEngine(EchoRig actor,PhysicsScene world,EchoScanLedger ledger=null)
        {rig=actor;physics=world;Ledger=ledger??new EchoScanLedger();}
        public ScanFailure Eligibility(WorldTarget candidate,bool requireAimed=false)
        {
            LastOccluderName=null;
            if(!rig||!rig.Body||!rig.ProbePitch||!rig.ScanOrigin||rig.Wheels==null||rig.Wheels.Count!=4||!rig.ScanOrigin.IsChildOf(rig.ProbePitch)||!physics.IsValid())return ScanFailure.NotEcho;
            if(!candidate||!candidate.isActiveAndEnabled)return ScanFailure.MissingTarget;var permission=candidate.GetComponent<EchoTargetPermissions>();
            if(!permission||!permission.CanInvestigate)return ScanFailure.Forbidden;
            var clue=candidate.GetComponent<ClueSource>();if(clue?clue.ClueId!=candidate.Id:string.IsNullOrWhiteSpace(permission.ScanFact))return ScanFailure.InvalidData;
            var origin=rig.ScanOrigin.position;var delta=candidate.transform.position-origin;
            if(!Finite(origin)||!Finite(delta)||delta.magnitude>2.2f||delta.magnitude<.2f)return ScanFailure.OutOfRange;
            if(!DesiredPose(candidate,out _))return ScanFailure.OutOfView;
            if(requireAimed&&Vector3.Angle(rig.ScanOrigin.forward,delta)>30)return ScanFailure.OutOfView;
            var count=physics.Raycast(origin,delta.normalized,hits,delta.magnitude+.01f,~0,QueryTriggerInteraction.Collide);
            if(count==hits.Length){LastOccluderName="HitBudgetFull";return ScanFailure.Obstructed;}var nearest=float.PositiveInfinity;Collider first=null;
            for(var i=0;i<count;i++)
            {
                var collider=hits[i].collider;if(!collider||collider.transform.IsChildOf(rig.transform))continue;
                if(hits[i].distance<nearest){nearest=hits[i].distance;first=collider;}
            }
            if(first&&first.GetComponentInParent<WorldTarget>()!=candidate){LastOccluderName=first.name;return ScanFailure.Obstructed;}return ScanFailure.None;
        }
        public bool TryBegin(WorldTarget candidate)
        {
            LastRequestFailure=IsBusy?ScanFailure.Busy:CooldownRemaining>0?ScanFailure.Cooldown:Eligibility(candidate);
            if(LastRequestFailure!=ScanFailure.None)return false;
            Generation++;target=candidate;originalPose=rig.ProbePitch.localRotation;Failure=ScanFailure.None;LastReceipt=null;scanning=0;SetPhase(ScanPhase.Aiming);return true;
        }
        public void Tick(float deltaSeconds)
        {
            if(!float.IsFinite(deltaSeconds)||deltaSeconds<0)throw new ArgumentOutOfRangeException(nameof(deltaSeconds));clock+=deltaSeconds;if(!IsBusy)return;
            var eligibility=Eligibility(target);if(eligibility!=ScanFailure.None){Fail(eligibility);return;}
            DesiredPose(target,out var pose);rig.ProbePitch.localRotation=Quaternion.RotateTowards(rig.ProbePitch.localRotation,pose,240*deltaSeconds);
            if(Phase==ScanPhase.Aiming)
            {if(Quaternion.Angle(rig.ProbePitch.localRotation,pose)<=1.5f){scanning=0;SetPhase(ScanPhase.Scanning);}return;}
            eligibility=Eligibility(target,true);if(eligibility!=ScanFailure.None){Fail(eligibility);return;}
            scanning+=deltaSeconds;if(scanning<.6f)return;
            if(!Ledger.TryCommit(target,out var receipt)){Fail(ScanFailure.InvalidData);return;}LastReceipt=receipt;cooldownUntil=clock+1;SetPhase(ScanPhase.Completed);
        }
        public void Cancel()
        {if(!IsBusy)return;if(rig&&rig.ProbePitch)rig.ProbePitch.localRotation=originalPose;LastReceipt=null;SetPhase(ScanPhase.Cancelled);}
        internal void ResetForPersistence(){Cancel();Generation++;target=null;LastReceipt=null;Failure=LastRequestFailure=ScanFailure.None;cooldownUntil=clock;scanning=0;SetPhase(ScanPhase.Idle);}
        private void Fail(ScanFailure reason){Failure=reason;if(rig&&rig.ProbePitch)rig.ProbePitch.localRotation=originalPose;LastReceipt=null;SetPhase(ScanPhase.Failed);}
        private void SetPhase(ScanPhase phase){if(Phase==phase)return;Phase=phase;PhaseChanged?.Invoke(phase);}
        private bool DesiredPose(WorldTarget candidate,out Quaternion pose)
        {
            pose=Quaternion.identity;var direction=candidate.transform.position-rig.ProbePitch.position;
            if(rig.ProbePitch.parent)direction=rig.ProbePitch.parent.InverseTransformDirection(direction);
            var horizontal=Mathf.Sqrt(direction.x*direction.x+direction.z*direction.z);var pitch=-Mathf.Atan2(direction.y,horizontal)*Mathf.Rad2Deg;
            if(!Finite(direction)||Mathf.Abs(pitch)>50)return false;var yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;pose=Quaternion.Euler(pitch,yaw,0);return true;
        }
        private static bool Finite(Vector3 point)=>float.IsFinite(point.x)&&float.IsFinite(point.y)&&float.IsFinite(point.z);
    }
}
