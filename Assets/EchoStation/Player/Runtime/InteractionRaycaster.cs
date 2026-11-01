using System;
using EchoStation.World;
using UnityEngine;

namespace EchoStation.Player
{
    public readonly struct InteractionSelection
    {
        public readonly WorldTarget Target;public readonly ClueSource Clue;public readonly Vector3 Point;public readonly float Distance;
        public bool IsValid=>Target&&Target.isActiveAndEnabled;
        internal InteractionSelection(WorldTarget target,ClueSource clue,Vector3 point,float distance){Target=target;Clue=clue;Point=point;Distance=distance;}
    }
    public sealed class InteractionRaycaster
    {
        public const float MaximumDistance=2.5f;
        private readonly PhysicsScene physics;private readonly TargetRegistry registry;private readonly Transform playerRoot;private readonly RaycastHit[] hits=new RaycastHit[16];
        public bool QuerySaturated{get;private set;}
        public InteractionRaycaster(PhysicsScene physics,TargetRegistry registry,Transform playerRoot)
        {if(!physics.IsValid())throw new ArgumentException("A valid physics scene is required.");this.physics=physics;this.registry=registry??throw new ArgumentNullException(nameof(registry));this.playerRoot=playerRoot;}
        public bool TrySelect(Ray ray,out InteractionSelection selection)
        {
            selection=default;QuerySaturated=false;if(!Finite(ray.origin)||!Finite(ray.direction)||ray.direction.sqrMagnitude<.0001f)return false;
            var count=physics.Raycast(ray.origin,ray.direction,hits,MaximumDistance,~0,QueryTriggerInteraction.Collide);if(count==hits.Length){QuerySaturated=true;return false;}
            var nearest=float.PositiveInfinity;WorldTarget target=null;RaycastHit selected=default;
            for(var i=0;i<count;i++)
            {
                var hit=hits[i];var collider=hit.collider;if(!collider||(playerRoot&&collider.transform.IsChildOf(playerRoot)))continue;var candidate=collider.GetComponentInParent<WorldTarget>();if(collider.isTrigger&&!candidate)continue;
                if(hit.distance<nearest){nearest=hit.distance;target=candidate;selected=hit;}
            }
            if(!target||!target.isActiveAndEnabled||!registry.TryGet(target.Id,out var owner)||owner!=target)return false;var clue=target.GetComponent<ClueSource>();if(clue&&(!clue.isActiveAndEnabled||!clue.IsReadable))return false;
            selection=new InteractionSelection(target,clue,selected.point,selected.distance);return true;
        }
        private static bool Finite(Vector3 value)=>float.IsFinite(value.x)&&float.IsFinite(value.y)&&float.IsFinite(value.z);
    }
}
