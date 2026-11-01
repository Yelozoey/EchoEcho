using System;
using System.Collections;
using System.IO;
using EchoStation.Station;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.PlayerFixtures;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class StationRouteTests:StationPlayFixture
 {
  private string folder;private StationRouteDriver driver;
  [SetUp]public void ResetEvidence(){folder=null;driver=null;}
  private IEnumerator Run(string id){folder=Path.GetFullPath("Docs/Tests/Stage2/Evidence/S2-V01/"+id+"_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));Directory.CreateDirectory(folder);station.GetComponent<StationPersistence>().ConfigureDirectory(Path.Combine(folder,"Save"));driver=new StationRouteDriver(station,inputs.Keyboard,inputs.Mouse,()=>R04ShadowProbe.Frame(),folder);yield return driver.Run(id);Assert.That(driver.Report.passed,Is.True);if(id!="seal_exit")Assert.That(driver.Report.reloaded,Is.True);}
  [UnityTest,Timeout(360000)]public IEnumerator V01_A01_SealExitFromNewGameThroughActualWalkReadTextAndMouseUi()
  {
   yield return new EnterPlayMode();yield return Boot();folder=Path.GetFullPath("Docs/Tests/Stage2/Evidence/S2-V01/Seal_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));Directory.CreateDirectory(folder);station.GetComponent<StationPersistence>().ConfigureDirectory(Path.Combine(folder,"Save"));driver=new StationRouteDriver(station,inputs.Keyboard,inputs.Mouse,()=>R04ShadowProbe.Frame(),folder);yield return driver.RunSeal();Assert.That(driver.Report.passed,Is.True);Assert.That(driver.Report.completed,Is.EquivalentTo(new[]{"pz02","pz03","pz04","pz05"}));
  }
  [UnityTest,Timeout(420000)]public IEnumerator V01_B01_MonitorStayFromNewGameWithActualEvidenceAndEndLoad(){yield return new EnterPlayMode();yield return Boot();yield return Run("monitor_stay");Assert.That(driver.Report.completed,Does.Not.Contain("pz01"));Assert.That(driver.Report.completed,Does.Contain("pz06"));}
  [UnityTest,Timeout(420000)]public IEnumerator V01_B02_ArchiveExitFromNewGameCollectsAll14AndRestoresPrechoice(){yield return new EnterPlayMode();yield return Boot();yield return Run("archive_exit");Assert.That(driver.Report.completed.Length,Is.EqualTo(6));Assert.That(driver.Report.catalogueClues,Is.EqualTo(14));}
  [TearDown]public void Archive(){if(driver!=null){driver.Dispose();if(!driver.Report.passed)driver.Report.error="See guarded Unity test result; last phase "+driver.Report.phase;File.WriteAllText(Path.Combine(folder,"Result.json"),JsonUtility.ToJson(driver.Report,true));TestContext.WriteLine("Actual route evidence: "+folder);Assert.That(driver.Report.inputRestored,Is.True);}}
 }
}
