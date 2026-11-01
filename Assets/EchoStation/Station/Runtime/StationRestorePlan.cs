using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Persistence;
using EchoStation.Player;
using EchoStation.World;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
namespace EchoStation.Station
{
 internal sealed class StationRestorePlan
 {
  internal StationSaveData Data;internal MemoryReadResult Memory;internal JournalEntry[] Entries;internal WorldTarget[] Targets,Scanned;internal StationGate Cabin,Core;internal GameObject CabinData,VentData;
  internal static Vector3 Point(SavedPose p)=>new Vector3(p.X,p.Y,p.Z);
  internal static bool TryBuild(StationSession station,StationSaveData source,out StationRestorePlan plan,out string error,bool trustedRollback=false)
  {
   plan=null;error="";
   try{
    if(!station||!station.Ready||source==null)throw new ArgumentException("观测站尚未就绪。");
    var data=source;if(!trustedRollback){var read=StationSaveJson.Decode(StationSaveJson.Encode(source));if(!read.Success)throw new ArgumentException(read.Detail);data=read.Data;}
    var targets=station.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WorldTarget>(true)).ToArray();var lookup=targets.ToDictionary(t=>t.Id,StringComparer.Ordinal);var scanned=new HashSet<string>(data.Progress.Scans,StringComparer.Ordinal);var entries=new List<JournalEntry>();
    WorldTarget Target(string id)=>lookup.TryGetValue(id,out var target)?target:throw new ArgumentException("当前场景缺少存档目标："+id);
    var memory=MemoryJson.Decode(data.MemoryJson);if(!memory.Success)throw new ArgumentException(memory.Detail);
    foreach(var id in data.Progress.Scans){var t=Target(id);var permission=t.GetComponent<EchoTargetPermissions>();if(!permission||!permission.CanInvestigate)throw new ArgumentException("扫描目标权限已变更："+id);}
    foreach(var id in data.Progress.Journal){var t=Target(id);JournalEntry entry;if(t.GetComponent<ClueSource>()){if(!EchoPersistence.TryReadCanonical(t,scanned.Contains(id),out var canonical,true))throw new ArgumentException("存档线索缺少扫描授权："+id);entry=JournalEntry.FromClue(canonical);}else{var permissions=t.GetComponent<EchoTargetPermissions>();if(!scanned.Contains(id)||!permissions||!permissions.CanInvestigate)throw new ArgumentException("设备记录缺少真实扫描："+id);var summary=permissions.ScanFact;entry=new JournalEntry(id,t.DisplayName+" · 扫描记录","Echo已完成对"+t.DisplayName+"的扫描。\n"+summary,summary,ClueKind.Scan);}
     if(!memory.Store.TryGetClue(id,out var fact)||fact.Title!=entry.Title||fact.Summary!=entry.Summary||fact.Kind!=(MemoryClueKind)(int)entry.Kind)throw new ArgumentException("场景与存档摘要不一致："+id);entries.Add(entry);}
    var result=new StationRestorePlan{Data=data,Memory=memory,Entries=entries.ToArray(),Targets=targets,Scanned=data.Progress.Scans.Select(Target).ToArray(),Cabin=Target("cabin_door_01").GetComponent<StationGate>(),Core=Target("core_door_01").GetComponent<StationGate>(),CabinData=Target("terminal_data_01").gameObject,VentData=Target("terminal_vent_data_01").gameObject};
    if(!result.Cabin||!result.Core)throw new ArgumentException("通路门组件缺失。");
    if(!trustedRollback)ValidateSpace(station,result);plan=result;return true;
   }catch(Exception e)when(e is ArgumentException||e is InvalidOperationException){error=e.Message;return false;}
  }
  private static void ValidateSpace(StationSession station,StationRestorePlan p)
  {
   var physics=station.gameObject.scene.GetPhysicsScene();var overlaps=new Collider[64];var hits=new RaycastHit[32];var player=Point(p.Data.Player);var echo=Point(p.Data.Echo);
   bool Own(Collider c)=>c&&(c.transform.IsChildOf(station.transform)||c.transform.IsChildOf(station.Echo.transform));
   bool Blocked(int count){if(count==overlaps.Length)return true;for(var i=0;i<count;i++)if(overlaps[i]&&!Own(overlaps[i])&&!overlaps[i].GetComponent<StationGate>())return true;return false;}
   Physics.SyncTransforms();if(Blocked(physics.OverlapCapsule(player+Vector3.up*.3f,player+Vector3.up*1.45f,.29f,overlaps,~0,QueryTriggerInteraction.Ignore)))throw new ArgumentException("玩家位置与场景碰撞。");
   var n=physics.Raycast(player+Vector3.up*.1f,Vector3.down,hits,2.2f,~0,QueryTriggerInteraction.Ignore);var grounded=false;if(n==hits.Length)throw new ArgumentException("地面检测超出预算。");for(var i=0;i<n;i++)if(!Own(hits[i].collider)&&hits[i].normal.y>.5f&&hits[i].point.y<=player.y+.05f)grounded=true;if(!grounded)throw new ArgumentException("玩家位置缺少安全地面。");
   if(Blocked(physics.OverlapBox(echo+new Vector3(0,.435f,0),new Vector3(.37f,.425f,.37f),overlaps,Quaternion.Euler(0,p.Data.Echo.Yaw,0),~0,QueryTriggerInteraction.Ignore)))throw new ArgumentException("Echo位置与场景碰撞。");
   var agent=station.Echo.GetComponent<EchoNavigation>().Agent;var filter=new NavMeshQueryFilter{agentTypeID=agent.agentTypeID,areaMask=agent.areaMask};if(!NavMesh.SamplePosition(echo,out var nav,1.5f,filter)||Mathf.Abs(nav.position.y-echo.y)>.2f)throw new ArgumentException("Echo位置缺少同层邻近导航面。");
   // Gate colliders may currently be disabled/open. Validate the candidate's intended closed boxes explicitly.
   foreach(var gate in station.gameObject.scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StationGate>())){var open=gate==p.Cabin?p.Data.Progress.CabinOpen:gate==p.Core?p.Data.Progress.CoreOpen:p.Data.Progress.VentOpen;if(open)continue;var box=gate.GetComponent<BoxCollider>();var scale=box.transform.lossyScale;var bounds=new Bounds(box.transform.TransformPoint(box.center),Vector3.Scale(box.size,new Vector3(Mathf.Abs(scale.x),Mathf.Abs(scale.y),Mathf.Abs(scale.z))));bounds.Expand(new Vector3(.6f,0,.6f));if(bounds.Contains(player+Vector3.up*.8f)||bounds.Contains(echo+Vector3.up*.4f))throw new ArgumentException("存档角色位于拟关闭通路门内。");}
  }
 }
}
