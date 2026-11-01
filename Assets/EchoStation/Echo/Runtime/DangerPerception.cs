using System;
using EchoStation.World;
using UnityEngine;

namespace EchoStation.Echo
{
    public enum DangerSenseFailure{None,InvalidActor,MissingTarget,NotDanger,OutOfRange,OutOfView,Occluded,HitBudgetFull}
    public sealed class DangerPerception
    {
        private readonly EchoRig rig;private readonly PhysicsScene physics;
        private readonly Collider[] nearby=new Collider[32];private readonly RaycastHit[] hits=new RaycastHit[16];
        public DangerPerception(EchoRig actor,PhysicsScene world){rig=actor;physics=world;}
        public bool CanPerceive(WorldTarget target,out DangerSenseFailure failure)
        {
            failure=DangerSenseFailure.None;
            if(!rig||!rig.ScanOrigin||!physics.IsValid())return Reject(DangerSenseFailure.InvalidActor,out failure);
            if(!target||!target.isActiveAndEnabled)return Reject(DangerSenseFailure.MissingTarget,out failure);
            var permissions=target.GetComponent<EchoTargetPermissions>();if(!permissions||!permissions.IsDanger)return Reject(DangerSenseFailure.NotDanger,out failure);
            var origin=rig.ScanOrigin.position;var delta=target.transform.position-origin;
            if(!float.IsFinite(delta.x)||!float.IsFinite(delta.y)||!float.IsFinite(delta.z)||delta.magnitude>4.00001f)return Reject(DangerSenseFailure.OutOfRange,out failure);
            if(delta.sqrMagnitude>.000001f&&Vector3.Angle(rig.transform.forward,delta)>55.001f)return Reject(DangerSenseFailure.OutOfView,out failure);
            var count=physics.Raycast(origin,delta.normalized,hits,delta.magnitude+.01f,~0,QueryTriggerInteraction.Collide);if(count==hits.Length)return Reject(DangerSenseFailure.HitBudgetFull,out failure);
            var nearest=float.PositiveInfinity;Collider blocker=null;
            for(var i=0;i<count;i++)
            {var collider=hits[i].collider;if(!collider||collider.transform.IsChildOf(rig.transform))continue;if(hits[i].distance<nearest){nearest=hits[i].distance;blocker=collider;}}
            return !blocker||blocker.GetComponentInParent<WorldTarget>()==target||Reject(DangerSenseFailure.Occluded,out failure);
        }
        public bool TryFindNearest(out WorldTarget target,Predicate<WorldTarget> allowed=null)
        {
            target=null;if(!rig||!rig.ScanOrigin||!physics.IsValid())return false;
            var center=rig.ScanOrigin.position;var count=physics.OverlapSphere(center,4,nearby,~0,QueryTriggerInteraction.Collide);if(count==nearby.Length)return false;var best=float.PositiveInfinity;
            for(var i=0;i<count;i++)
            {var candidate=nearby[i]?nearby[i].GetComponentInParent<WorldTarget>():null;if(!candidate||(allowed!=null&&!allowed(candidate))||!CanPerceive(candidate,out _))continue;var distance=(candidate.transform.position-center).sqrMagnitude;if(distance<best){best=distance;target=candidate;}}
            return target;
        }
        private static bool Reject(DangerSenseFailure value,out DangerSenseFailure result){result=value;return false;}
    }
}
