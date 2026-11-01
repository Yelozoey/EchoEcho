using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using EchoStation.AI.Probe;
using EchoStation.Echo;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AI
{
 public sealed class CompatibilityTests
 {
  private static string Evidence=>Path.GetFullPath("Docs/Tests/Stage3/Evidence/S3-G01");
  [Test]public void PluginsAreExplicitlyWindows64Only()
  {
   foreach(var relative in new[]{"Managed/Microsoft.ML.OnnxRuntime.dll","Plugins/x86_64/onnxruntime.dll","Plugins/x86_64/onnxruntime_providers_shared.dll"})
   {
    var plugin=AssetImporter.GetAtPath("Assets/EchoStation/AI/ThirdParty/OnnxRuntime/"+relative) as PluginImporter;
    Assert.That(plugin,Is.Not.Null);Assert.That(plugin.GetCompatibleWithAnyPlatform(),Is.False);Assert.That(plugin.GetCompatibleWithEditor(),Is.True);
    Assert.That(plugin.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64),Is.True);
    foreach(var platform in new[]{BuildTarget.StandaloneWindows,BuildTarget.StandaloneLinux64,BuildTarget.StandaloneOSX})Assert.That(plugin.GetCompatibleWithPlatform(platform),Is.False);
    if(plugin.isNativePlugin){Assert.That(plugin.GetEditorData("OS"),Is.EqualTo("Windows"));Assert.That(plugin.GetEditorData("CPU"),Is.EqualTo("x86_64"));}
   }
  }
  [Test]public void ProbeSceneUsesIndependentNavigationAndPreservesS2()
  {
   var record=JObject.Parse(File.ReadAllText(Path.Combine(Evidence,"ScenePrepared.json")));var nav=record["navGuid"].ToString();
   Assert.That(nav,Is.Not.EqualTo("7076a28f627fb964b88368ee1aad74df"));
   Assert.That(File.ReadAllText(record["scene"].ToString()),Does.Contain(nav).And.Not.Contain("7076a28f627fb964b88368ee1aad74df"));
   Assert.That(File.ReadAllText("Assets/EchoStation/Station/Scenes/ES_Station_Greybox.unity"),Does.Contain("7076a28f627fb964b88368ee1aad74df"));
   Assert.That(AssetDatabase.GUIDToAssetPath(nav),Is.EqualTo(record["nav"].ToString()));
  }
  [Test]public void ModelProvenanceContainsTrueEightBitMainWeights()
  {
   var audit=JObject.Parse(File.ReadAllText(Path.Combine(Evidence,"Weight8GraphAudit.json")));Assert.That(audit["quantNodes"].Count(),Is.EqualTo(168));
   foreach(var node in audit["quantNodes"]){Assert.That((int)node["attributes"]["bits"],Is.EqualTo(8));Assert.That((int)node["attributes"]["block_size"],Is.EqualTo(32));}
   var transform=JObject.Parse(File.ReadAllText(Path.Combine(Evidence,"LastHeadProbeTransformation.json")));Assert.That((bool)transform["weightsUnchanged"],Is.True);
   Assert.That((long)transform["bytes"],Is.LessThan(1L<<30));Assert.That(GameRuntimeOptions.UseLLM,Is.False);
  }
  [UnityTest]public IEnumerator RealFinalModelRunsAndReleasesOnBackgroundOwner()
  {
   var path=Path.Combine(Evidence,"NUnit_Real_"+DateTime.UtcNow.Ticks+".json");var main=Thread.CurrentThread.ManagedThreadId;
   var thread=OrtCompatibilityProbe.Start(Path.GetFullPath("Library/EchoStation/S3Cache/models/qwen-int4/onnx/model_weight8_last.onnx"),Path.Combine(Evidence,"PrefillFixture.json"),Path.Combine(Evidence,"WEIGHT8_ACC4_A01"),path,main,1);
   var clock=System.Diagnostics.Stopwatch.StartNew();while(thread.IsAlive&&clock.Elapsed.TotalSeconds<30)yield return null;
   Assert.That(thread.IsAlive,Is.False,"Owner did not finish");Assert.That(File.Exists(path),Is.True);
   var result=JObject.Parse(File.ReadAllText(path));Assert.That((bool)result["passed"],Is.True,result["error"]?.ToString());Assert.That((int)result["ownerThread"],Is.Not.EqualTo(main));Assert.That((int)result["releasedOnThread"],Is.EqualTo((int)result["ownerThread"]));
   Assert.That(result["rounds"].Count(),Is.EqualTo(1));foreach(var step in result["rounds"][0]["steps"]){Assert.That((double)step["referenceMaxAbsoluteError"],Is.LessThanOrEqualTo(.002));Assert.That((int)step["ownerThread"],Is.EqualTo((int)result["ownerThread"]));}
   Assert.That(GameRuntimeOptions.UseLLM,Is.False);
  }
  [UnityTest]public IEnumerator MissingWeightFailsWithoutMainThreadOwnership()
  {
   var path=Path.Combine(Evidence,"NUnit_Missing_"+DateTime.UtcNow.Ticks+".json");var main=Thread.CurrentThread.ManagedThreadId;
   var thread=OrtCompatibilityProbe.Start(Path.Combine(Evidence,"missing-model-for-test.onnx"),Path.Combine(Evidence,"PrefillFixture.json"),Path.Combine(Evidence,"WEIGHT8_ACC4_A01"),path,main,1);
   var clock=System.Diagnostics.Stopwatch.StartNew();while(thread.IsAlive&&clock.Elapsed.TotalSeconds<10)yield return null;
   Assert.That(thread.IsAlive,Is.False);var result=JObject.Parse(File.ReadAllText(path));Assert.That((bool)result["passed"],Is.False);Assert.That(result["error"].ToString(),Does.Contain("[ErrorCode:NoSuchFile]"));
   Assert.That((int)result["ownerThread"],Is.Not.EqualTo(main));Assert.That((int)result["releasedOnThread"],Is.EqualTo((int)result["ownerThread"]));
  }
 }
}
