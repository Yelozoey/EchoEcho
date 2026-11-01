using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EchoStation.Salvage;
using UnityEngine;
using UnityEngine.AI;

namespace EchoStation.SalvageNext
{
    public sealed class RecoveryKnowledgeCapture
    {
        readonly int ownerThread=Thread.CurrentThread.ManagedThreadId;
        long revision;
        string previousSignature;
        public RecoveryKnowledge Capture(SalvageCommandHub hub,long epoch,RecoveryPreference preference)
        {
            if(Thread.CurrentThread.ManagedThreadId!=ownerThread)throw new InvalidOperationException("Capture belongs to its game thread.");
            if(!Application.isPlaying||!hub)throw new InvalidOperationException("Capture requires the live scene.");
            var carry=hub.GetComponent<SalvageCarry>();var delivery=hub.GetComponent<SalvageDelivery>();
            if(!carry||!delivery||carry.Registry.Count!=7)throw new InvalidOperationException("Incomplete recovery world.");
            var views=hub.Targets.ToDictionary(t=>t.Id,StringComparer.Ordinal);var rows=new List<ObservedTarget>();
            foreach(var definition in SalvageTargetCatalog.Items)
            {
                if(!hub.Known.Contains(definition.Id))continue; // Do not even read unknown target positions.
                CargoState? state=definition.Kind==SalvageTargetKind.Cargo?carry.Registry.Get(definition.Id).State:(CargoState?)null;
                double? meters=null;
                if((state==null||state==CargoState.World)&&views.TryGetValue(definition.Id,out var view)&&view&&view.isActiveAndEnabled)
                    meters=PathLength(hub.transform.position,view.transform.position);
                rows.Add(new ObservedTarget(definition.Id,true,state,meters.HasValue,meters));
            }
            // Sampling the same state must not invalidate every in-flight reply.
            // Distances are fresh for advice; executable membership/state changes invalidate the revision.
            var signature=epoch+":"+preference+":"+delivery.Count+":"+delivery.Points+":"+string.Join(";",rows.OrderBy(r=>r.Id,StringComparer.Ordinal).Select(r=>r.Id+":"+r.State+":"+r.Reachable));
            if(signature!=previousSignature){revision=checked(revision+1);previousSignature=signature;}
            return new RecoveryKnowledge(epoch,revision,preference,delivery.Count,delivery.Points,rows);
        }
        static double? PathLength(Vector3 from,Vector3 target)
        {
            if(!NavMesh.SamplePosition(from,out var start,1,NavMesh.AllAreas)||!NavMesh.SamplePosition(target,out var goal,1.8f,NavMesh.AllAreas))return null;
            var path=new NavMeshPath();if(!NavMesh.CalculatePath(start.position,goal.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)return null;
            var corners=path.corners;double length=0;for(int i=1;i<corners.Length;i++)length+=Vector3.Distance(corners[i-1],corners[i]);
            return length;
        }
    }
}
