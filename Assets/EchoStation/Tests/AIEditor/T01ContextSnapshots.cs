using System;
using System.Collections.Generic;
using EchoStation.AI.Context;
using EchoStation.Memory;
using EchoStation.Story;

namespace EchoStation.Tests.AI
{
 // Controlled visibility/known-summary fixtures using existing game metadata.
 // No scoring label is accepted; this is not a claim of a player completing a route.
 internal static class T01ContextSnapshots
 {
  public static InferenceSnapshot Create(string input,string setup)
  {
   var store=new MemoryStore();string zone;
   switch(setup)
   {
    case "valley_empty":zone="valley";break;
    case "cabin_known":zone="cabin";Add(store,"note_maintenance_01");break;
    case "lab_known":zone="lab_a";Add(store,"note_maintenance_01");Add(store,"scan_mark_01");Add(store,"note_lab_circuit_01");break;
    default:throw new ArgumentException("Unknown T01 controlled context");
   }
   var ids=new[]{"note_entry_inspection_01","note_evacuation_01","note_maintenance_01","note_roster_01","scan_mark_01","scan_lab_calibration_01","note_lab_circuit_01","power_panel_01"};var candidates=new List<TargetCandidate>();
   for(var i=0;i<ids.Length;i++)
   {
    var id=ids[i];var power=id=="power_panel_01";StationClues.TryGet(id,out var clue);var targetZone=power?"lab_a":clue.ZoneId;var name=power?"A区通风供电面板":clue.Title;
    var referenced=id=="scan_mark_01"&&store.TryGetClue("note_maintenance_01",out _);candidates.Add(new TargetCandidate(id,name,targetZone,true,true,power,targetZone==zone,targetZone==zone?i+1:100+i,referenced));
   }
   return new InferenceSnapshot(store,input,120,zone,candidates);
  }
  private static void Add(MemoryStore store,string id)
  {if(!StationClues.TryGet(id,out var clue))throw new ArgumentException("Game clue definition missing");if(store.AddClue(new ClueMemory(clue.Id,clue.Title,clue.Summary,clue.Kind,clue.Password,clue.KeyMarker))!=MemoryWriteResult.Added)throw new ArgumentException("Controlled clue could not be captured");}
 }
}
