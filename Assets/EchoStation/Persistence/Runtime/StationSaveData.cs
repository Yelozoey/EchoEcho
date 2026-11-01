using System;
using System.Linq;
using EchoStation.Dialogue;
using EchoStation.Memory;
using EchoStation.Puzzles;
using EchoStation.Story;
namespace EchoStation.Persistence
{
 public sealed class SavedPose
 {
  public float X,Y,Z,Yaw,Pitch;
  public SavedPose(){}
  public SavedPose(float x,float y,float z,float yaw=0,float pitch=0){X=x;Y=y;Z=z;Yaw=yaw;Pitch=pitch;}
 }
 public sealed class StationSaveData
 {
  public SavedPose Player,Echo;
  public string EchoMode="wait",MemoryJson;
  public bool Flashlight;
  public double Elapsed;
  public ProgressState Progress;
  public DialogueMessage[] Dialogue=Array.Empty<DialogueMessage>();
 }
 public static class StationSaveAudit
 {
  public static int Access(float z)=>z>58.3f?3:z>38.3f?2:z>5.5f?1:0;
  public static string Zone(float z)=>z>=-40&&z< -7?"valley":z>=-7&&z<4?"cabin":z>=4&&z<38?"lab_a":z>=38&&z<58?"vent":z>=58&&z<76?"core":null;
  public static string Validate(StationSaveData s)
  {
   if(s==null||!Pose(s.Player)||!Pose(s.Echo)||s.Echo.Pitch!=0||s.EchoMode!="follow"&&s.EchoMode!="wait"||double.IsNaN(s.Elapsed)||double.IsInfinity(s.Elapsed)||s.Elapsed<0)return "invalid_actor_or_clock";
   var dx=s.Player.X-s.Echo.X;var dz=s.Player.Z-s.Echo.Z;if(dx*dx+dz*dz<.85f*.85f)return "actor_overlap";
   if(s.Progress==null||s.Progress.Zone!=Zone(s.Player.Z)||s.Progress.AccessLevel!=Access(s.Player.Z)||Access(s.Echo.Z)>=1&&!s.Progress.CabinOpen||Access(s.Echo.Z)>=2&&!s.Progress.VentOpen||Access(s.Echo.Z)>=3&&!s.Progress.CoreOpen)return "pose_progress_mismatch";
   var audit=ProgressAudit.Validate(s.Progress);if(audit!=null)return audit;
   var read=EchoStation.Memory.MemoryJson.Decode(s.MemoryJson);if(!read.Success)return "invalid_memory";var memory=read.Store;
   if(!s.Progress.Events.OrderBy(id=>id,StringComparer.Ordinal).SequenceEqual(memory.Events.Select(e=>e.Id).OrderBy(id=>id,StringComparer.Ordinal)))return "memory_event_mismatch";
   if(!s.Progress.Journal.OrderBy(id=>id,StringComparer.Ordinal).SequenceEqual(memory.Clues.Select(e=>e.Id).OrderBy(id=>id,StringComparer.Ordinal)))return "memory_journal_mismatch";
   foreach(var c in memory.Clues){if(StationClues.TryGet(c.Id,out var canonical)){if(c.Title!=canonical.Title||c.Summary!=canonical.Summary||c.Kind!=canonical.Kind||c.Password!=canonical.Password||c.KeyMarker!=canonical.KeyMarker)return "noncanonical_clue";}else if(!ProgressAudit.DeviceIds.Contains(c.Id)||c.Kind!=MemoryClueKind.Scan||c.Password!=null||c.KeyMarker!=null)return "noncanonical_equipment";}
   foreach(var e in memory.Events){if(e.ElapsedGameSeconds>s.Elapsed+.05)return "event_after_clock";var puzzle=StationScript.Puzzles.FirstOrDefault(p=>p.CompletionId==e.Id);if(puzzle!=null&&(e.Priority!=MemoryPriority.Critical||e.Summary!="玩家完成"+puzzle.Title+"。"))return "noncanonical_progress_event";if(e.Id.StartsWith("ending_",StringComparison.Ordinal))return "ended_save_forbidden";}
   if(!memory.TryGetCharacter("player_current_zone",out var zone)||zone.Kind!=CharacterMemoryKind.PlayerKnowledge||zone.TargetId!=s.Progress.Zone)return "memory_zone_mismatch";
   var dialogue=new DialogueSession();if(!dialogue.TryRestoreRecent(s.Dialogue))return "invalid_dialogue";
   return null;
  }
  private static bool Pose(SavedPose p)=>p!=null&&Finite(p.X)&&Finite(p.Y)&&Finite(p.Z)&&Finite(p.Yaw)&&Finite(p.Pitch)&&Math.Abs(p.X)<24&&p.Y>=-4.3f&&p.Y<=4&&p.Z>=-39.5f&&p.Z<75.5f&&p.Yaw>=0&&p.Yaw<360&&p.Pitch>=-80&&p.Pitch<=80;
  private static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
 }
}
