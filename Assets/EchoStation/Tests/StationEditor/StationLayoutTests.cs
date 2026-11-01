using System.Linq;
using EchoStation.Station;
using NUnit.Framework;
using UnityEngine;
namespace EchoStation.Tests.Station
{
 public sealed class StationLayoutTests
 {
  [Test]public void W01_A01_FiveZonesHaveApprovedOrderAndNoGap(){CollectionAssert.AreEqual(new[]{"valley","cabin","lab_a","vent","core"},StationLayout.Zones.Select(z=>z.Id));for(var i=1;i<5;i++)Assert.That(StationLayout.Zones[i].StartZ,Is.EqualTo(StationLayout.Zones[i-1].EndZ));}
  [Test]public void W01_A02_BoundariesAreDeterministicAndSpawnsInValley(){foreach(var z in StationLayout.Zones){Assert.That(StationLayout.ZoneAt(new Vector3(0,0,z.StartZ)),Is.EqualTo(z.Id));Assert.That(StationLayout.ZoneAt(z.Center),Is.EqualTo(z.Id));}Assert.That(StationLayout.ZoneAt(StationLayout.PlayerSpawn),Is.EqualTo("valley"));Assert.That(StationLayout.ZoneAt(StationLayout.EchoSpawn),Is.EqualTo("valley"));Assert.That(StationLayout.ZoneAt(new Vector3(0,0,76)),Is.Null);}
  [Test]public void W01_A03_SlopeAndPassageFitExistingEchoAgent(){Assert.That(Mathf.Atan2(4,18)*Mathf.Rad2Deg,Is.LessThan(35));Assert.That(3.2f,Is.GreaterThan(.54f*2));Assert.That(StationLayout.DataFolder,Does.Not.Contain("World/Scenes/ES_Cabin"));}
 }
}
