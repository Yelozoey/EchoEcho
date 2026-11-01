using System.IO;
using System.Text.RegularExpressions;
using EchoStation.Station;
using NUnit.Framework;
using UnityEditor;
namespace EchoStation.Tests.Station
{
 public sealed class StationPvsTests
 {
  [Test]public void W06_B301_ActualPvsAssetIsOwnedNonemptyAndSceneUsesRecordedParameters()
  {
   var scene=File.ReadAllText(StationLayout.ScenePath);Assert.That(scene,Does.Contain("smallestOccluder: 2"));Assert.That(scene,Does.Contain("smallestHole: 0.25"));var reference=Regex.Match(scene,@"m_OcclusionCullingData: \{fileID: 36300000, guid: ([a-f0-9]+)");Assert.That(reference.Success,Is.True);var path=AssetDatabase.GUIDToAssetPath(reference.Groups[1].Value);Assert.That(path,Does.StartWith("Assets/EchoStation/Station/"));Assert.That(new FileInfo(path).Length,Is.GreaterThan(128));var data=Regex.Match(File.ReadAllText(path),@"m_PVSData: ([a-fA-F0-9]+)");Assert.That(data.Success,Is.True);Assert.That(data.Groups[1].Length/2,Is.GreaterThan(128));
  }
 }
}
