using System;
using System.Collections.Generic;
using System.IO;
using EchoStation.AI.Context;
using EchoStation.Memory;
using Newtonsoft.Json.Linq;

namespace EchoStation.Tests.AI
{
 // Diagnostic only: grammar receives every declared allowed target, never the answer label.
 internal static class Qwen3TuningCaseContext
 {
  public static InferenceSnapshot Create(JToken row,bool heldOut,int index)
  {
   if(row==null||row["input"]?.Type!=JTokenType.String)throw new InvalidDataException("Missing case input");
   if(!heldOut)return new InferenceSnapshot(new MemoryStore(),row["input"].Value<string>(),index,"cabin",new[]{
    new TargetCandidate("power_panel_01","供电面板","cabin",true,true,false,true,1),
    new TargetCandidate("hazard_01","危险设备","cabin",true,false,true,true,2)});
   if(row["zone"]?.Type!=JTokenType.String||!(row["targets"] is JArray targets)||targets.Count>8)throw new InvalidDataException("Invalid held-out target scope");
   var zone=row["zone"].Value<string>();var candidates=new List<TargetCandidate>();var seen=new HashSet<string>(StringComparer.Ordinal);
   foreach(var target in targets)
   {
    if(target["id"]?.Type!=JTokenType.String||target["name"]?.Type!=JTokenType.String||!(target["actions"] is JArray actions)||actions.Count<1||actions.Count>2)throw new InvalidDataException("Invalid target metadata");
    var id=target["id"].Value<string>();if(!seen.Add(id))throw new InvalidDataException("Duplicate target ID");
    var allowed=new HashSet<string>(StringComparer.Ordinal);
    foreach(var action in actions)
    {if(action.Type!=JTokenType.String||(action.Value<string>()!="investigate"&&action.Value<string>()!="warn")||!allowed.Add(action.Value<string>()))throw new InvalidDataException("Invalid target action");}
    candidates.Add(new TargetCandidate(id,target["name"].Value<string>(),zone,true,allowed.Contains("investigate"),allowed.Contains("warn"),true,candidates.Count+1));
   }
   return new InferenceSnapshot(new MemoryStore(),row["input"].Value<string>(),index,zone,candidates);
  }
 }
}
