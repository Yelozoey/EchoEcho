using System;
using System.IO;
using EchoStation.Station;
using NUnit.Framework;

namespace EchoStation.Tests.AIExecution
{
 public abstract class S3SaveFixture:S3PlayFixture
 {
  protected string directory;protected StationPersistence persistence;
  protected void BindTemporarySlot()
  {
   directory=Path.GetFullPath(Path.Combine("Library/EchoStation/S3Tests/WorldSaves",Guid.NewGuid().ToString("N")));persistence=station.GetComponent<StationPersistence>();persistence.ConfigureDirectory(Path.Combine(directory,"S3"),"S3");
  }
  [TearDown]public void CleanupFiles()
  {
   if(directory==null||!Directory.Exists(directory))return;var root=Path.GetFullPath("Library/EchoStation/S3Tests/WorldSaves")+Path.DirectorySeparatorChar;var resolved=Path.GetFullPath(directory);Assert.That(resolved.StartsWith(root,StringComparison.OrdinalIgnoreCase),Is.True);
   if(TestContext.CurrentContext.Result.Outcome.Status!=NUnit.Framework.Interfaces.TestStatus.Failed)Directory.Delete(resolved,true);
  }
 }
}
