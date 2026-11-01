using System.IO;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.Memory;
using Newtonsoft.Json.Linq;

namespace EchoStation.Tests.AI
{
 internal static class Qwen3RuntimeFixtureContext
 {
  public static InferenceSnapshot Create(JToken row,int index)
  {
   var scope=Qwen3TuningCaseContext.Create(row,true,index);
   if(row["summary"]?.Type!=JTokenType.String)throw new InvalidDataException("Known fixture summary required");
   var store=new MemoryStore();if(store.AddClue(new ClueMemory("fixture_summary","设备记录",row["summary"].Value<string>(),MemoryClueKind.Note))!=MemoryWriteResult.Added)throw new InvalidDataException("Fixture memory rejected");
   return new InferenceSnapshot(store,scope.Input,index,scope.Zone,scope.Targets.Select((target,i)=>new TargetCandidate(target.Id,target.Name,scope.Zone,true,target.CanInvestigate,target.IsDanger,true,i+1)));
  }
  public static MemorySearch Search()=>new MemorySearch(new[]{
   new MemoryKeywordGroup("设备记录","记录","资料","线索","摘要","设备","危险","风险"),
   new MemoryKeywordGroup("设备记录","温度探头","fixture_sensor_01"),
   new MemoryKeywordGroup("设备记录","继电器箱","fixture_relay_02"),
   new MemoryKeywordGroup("设备记录","检修标记","fixture_marker_03"),
   new MemoryKeywordGroup("设备记录","裸露电缆","fixture_cable_04"),
   new MemoryKeywordGroup("设备记录","高温阀","fixture_valve_05")});
 }
}
