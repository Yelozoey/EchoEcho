using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EchoStation.AI.Authorization;
using EchoStation.AI.Context;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Station;
using EchoStation.World;
using UnityEngine;

namespace EchoStation.AI.GameIntegration
{
 public sealed class StationInferenceCapture
 {
  public InferenceSnapshot Snapshot {get;}public IReadOnlyList<TrustedTargetIdentity> Catalog {get;}
  internal StationInferenceCapture(InferenceSnapshot snapshot,TrustedTargetIdentity[] catalog){Snapshot=snapshot;Catalog=Array.AsReadOnly(catalog);}
 }
 // This object stays on the Unity thread. Only Snapshot crosses into inference.
 public sealed class StationInferenceSnapshotSource
 {
  private readonly StationSession station;private readonly int main;private readonly Plane[] planes=new Plane[6];private readonly RaycastHit[] hits=new RaycastHit[32];
  public StationInferenceSnapshotSource(StationSession session,int unityMainThread)
  {
   if(unityMainThread<=0||Thread.CurrentThread.ManagedThreadId!=unityMainThread)throw new InvalidOperationException("Snapshot source must be created on the Unity thread");
   if(!session)throw new ArgumentNullException(nameof(session));station=session;main=unityMainThread;
  }
  public StationInferenceCapture Capture(string input)
  {
   if(Thread.CurrentThread.ManagedThreadId!=main)throw new InvalidOperationException("No Unity objects may be read by the inference worker");
   if(!station||!station.Ready||station.Restoring)throw new InvalidOperationException("Station snapshot is not available");
   var zone=StationLayout.ZoneAt(station.transform.position);if(zone==null)throw new InvalidOperationException("Player is outside the bounded station zones");
   var camera=station.GetComponentInChildren<Camera>();if(!camera)throw new InvalidOperationException("Player camera is missing");GeometryUtility.CalculateFrustumPlanes(camera,planes);
   var catalog=station.AvailableTargets();if(catalog.Length>128)throw new ArgumentException("World target catalog exceeds budget");
   var identities=catalog.Select(t=>new TrustedTargetIdentity(t.Id,t.Name,t.Aliases.ToArray())).ToArray();var candidates=new List<TargetCandidate>();
   var hasStorageReference=station.Memories.Store.TryGetClue("note_maintenance_01",out var note)&&note.Kind==MemoryClueKind.Note;
   foreach(var identity in identities)
   {
    if(!WorldTarget.Active.TryGet(identity.Id,out var target)||!target||target.gameObject.scene!=station.gameObject.scene)continue;
    var targetZone=StationLayout.ZoneAt(target.transform.position);if(targetZone==null)continue;var permission=target.GetComponent<EchoTargetPermissions>();
    var available=target.isActiveAndEnabled&&target.Registered;var visible=available&&targetZone==zone&&Visible(camera,target);
    // Existing maintenance record explicitly points to the storage wall; never inspect source FullText here.
    var referenced=hasStorageReference&&identity.Id=="scan_mark_01";
    candidates.Add(new TargetCandidate(identity.Id,identity.Name,targetZone,available,permission&&permission.CanInvestigate,permission&&permission.IsDanger,visible,Vector3.Distance(station.transform.position,target.transform.position),referenced));
   }
   return new StationInferenceCapture(new InferenceSnapshot(station.Memories.Store,input,station.Elapsed,zone,candidates),identities);
  }
  private bool Visible(Camera camera,WorldTarget target)
  {
   var collider=target.GetComponent<Collider>();var bounds=collider?collider.bounds:new Bounds(target.transform.position,Vector3.one*.2f);
   if(!GeometryUtility.TestPlanesAABB(planes,bounds))return false;var origin=camera.transform.position;var delta=bounds.center-origin;var distance=delta.magnitude;if(distance<.01f)return true;
   var count=station.gameObject.scene.GetPhysicsScene().Raycast(origin,delta/distance,hits,distance+.02f,~0,QueryTriggerInteraction.Collide);if(count==hits.Length)return false;
   var nearest=float.PositiveInfinity;Collider first=null;
   for(var i=0;i<count;i++)
   {
    var hit=hits[i];if(!hit.collider||hit.collider.transform.IsChildOf(station.transform)||hit.collider.transform.IsChildOf(station.Echo.transform))continue;
    if(hit.distance<nearest){nearest=hit.distance;first=hit.collider;}
   }
   return !first||first.GetComponentInParent<WorldTarget>()==target;
  }
 }
}
