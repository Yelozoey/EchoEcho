using System;
using System.Diagnostics;
using System.IO;
using EchoStation.AI.Inference;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class FiniteTensorValidatorTests
 {
  private static float From(uint bits)=>BitConverter.Int32BitsToSingle(unchecked((int)bits));
  [Test]
  public void EveryIeeeCategoryMatchesTheFrameworkDefinition()
  {
   var patterns=new uint[]{0,0x80000000,1,0x007fffff,0x00800000,0x3f800000,0x7f7fffff,0xff7fffff,
    0x7f800000,0xff800000,0x7fc00000,0xffc00000,0x7f800001,0xff800001,0x7fffffff,0xffffffff};
   foreach(var pattern in patterns)Check(From(pattern));
   uint state=0x261027;
   for(var i=0;i<8192;i++){state=unchecked(state*1664525u+1013904223u);Check(From(state));}
  }
  private static void Check(float value)
  {
   var values=new[]{value};
   if(float.IsNaN(value)||float.IsInfinity(value))Assert.Throws<InvalidDataException>(()=>FiniteTensorValidator.RequireFinite(values));
   else Assert.DoesNotThrow(()=>FiniteTensorValidator.RequireFinite(values));
  }
  [Test]
  public void EmptyAndSlicedBuffersAndBadFirstMiddleLastAreChecked()
  {
   FiniteTensorValidator.RequireFinite(Array.Empty<float>());
   var values=new float[1025];
   foreach(var position in new[]{0,512,1024})
   {
    values[position]=From(0x7f800001);Assert.Throws<InvalidDataException>(()=>FiniteTensorValidator.RequireFinite(values));values[position]=0;
   }
   values[0]=float.PositiveInfinity;values[1024]=float.NegativeInfinity;
   FiniteTensorValidator.RequireFinite(new ReadOnlySpan<float>(values,1,1023));
  }
  [Test]
  public void RecordControlledScanCostWithoutReplacingTheFullModelBenchmark()
  {
   const int count=1048576,rounds=8;var values=new float[count];
   for(var i=0;i<count;i++)values[i]=(i%101-50)*.125f;
   FiniteTensorValidator.RequireFinite(values);Reference(values);
   var before=Stopwatch.GetTimestamp();for(var i=0;i<rounds;i++)Reference(values);var original=Stopwatch.GetTimestamp()-before;
   before=Stopwatch.GetTimestamp();for(var i=0;i<rounds;i++)FiniteTensorValidator.RequireFinite(values);var optimized=Stopwatch.GetTimestamp()-before;
   var record=new JObject{{"scope","pure Mono scanner microbenchmark; not inference/game or hardware acceptance"},{"floatCount",count},{"rounds",rounds},{"referenceMs",original*1000.0/Stopwatch.Frequency},{"bitScanMs",optimized*1000.0/Stopwatch.Frequency}};
   var path="Docs/Tests/Stage3/Evidence/S3-J01/CandidateQwen3/Tuning/FiniteScan_"+DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ")+".json";
   File.WriteAllText(path,record.ToString());
  }
  private static void Reference(float[] values)
  {for(var i=0;i<values.Length;i++)if(float.IsNaN(values[i])||float.IsInfinity(values[i]))throw new InvalidDataException();}
 }
}
