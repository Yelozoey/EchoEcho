using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class Qwen3TuningSettingsTests
 {
  [Test] public void HistoricalConfigsKeepCpu2()
  {Assert.That(Qwen3TuningSettings.CpuThreads(new JObject()),Is.EqualTo(2));}
  [TestCase(2)][TestCase(4)] public void OnlyConfirmedBudgetsAccepted(int threads)
  {Assert.That(Qwen3TuningSettings.CpuThreads(new JObject{{"cpuThreads",threads}}),Is.EqualTo(threads));}
  [Test] public void InvalidOrUnboundedBudgetsFailClosed()
  {
   foreach(var value in new JToken[]{new JValue(0),new JValue(1),new JValue(3),new JValue(8),new JValue(2.0),new JValue("4"),new JValue(true),JValue.CreateNull()})
    Assert.Throws<InvalidDataException>(()=>Qwen3TuningSettings.CpuThreads(new JObject{{"cpuThreads",value}}));
   Assert.Throws<InvalidDataException>(()=>Qwen3TuningSettings.CpuThreads(null));
  }
 }
}
