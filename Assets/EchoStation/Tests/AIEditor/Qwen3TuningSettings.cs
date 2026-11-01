using System.IO;
using Newtonsoft.Json.Linq;

namespace EchoStation.Tests.AI
{
 // Pending R04b: never allow unbounded CPU use from a mutable experiment file.
 internal static class Qwen3TuningSettings
 {
  public static int CpuThreads(JObject config)
  {
   if(config==null)throw new InvalidDataException("Experiment settings missing");
   var value=config["cpuThreads"];
   if(value==null)return 2;
   if(value.Type!=JTokenType.Integer||(value.Value<long>()!=2&&value.Value<long>()!=4))throw new InvalidDataException("Only CPU2 or CPU4 is authorized");
   return value.Value<int>();
  }
 }
}
