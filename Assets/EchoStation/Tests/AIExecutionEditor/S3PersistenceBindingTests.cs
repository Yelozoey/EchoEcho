using System;
using System.Collections;
using System.IO;
using EchoStation.Station;
using EchoStation.Tests.Station;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public sealed class S3PersistenceBindingTests:StationPlayFixture
 {
  private static string Temporary(string stage)=>Path.GetFullPath(Path.Combine("Library/EchoStation/S3Tests/Binding",Guid.NewGuid().ToString("N"),stage));
  [UnityTest]public IEnumerator S2DefaultAndOldDirectoryOverloadKeepOriginalBehaviorWithoutWritingFiles()
  {
   yield return new EnterPlayMode();yield return Boot();var persistence=station.GetComponent<StationPersistence>();Assert.That(persistence.StageLabel,Is.EqualTo("S2"));Assert.That(persistence.Files.DirectoryPath,Is.EqualTo(Path.GetFullPath(Path.Combine(Application.persistentDataPath,"Saves","S2"))));
   var path=Temporary("S2");persistence.ConfigureDirectory(path);Assert.That(persistence.StageLabel,Is.EqualTo("S2"));Assert.That(persistence.Files.PrimaryPath,Is.EqualTo(Path.Combine(path,"slot1.json")));Assert.That(Directory.Exists(path),Is.False,"Binding must not create or overwrite a slot");
  }
  [UnityTest]public IEnumerator S3BindingUsesIndependentRootAndInvalidStageCannotPartiallyRebind()
  {
   yield return new EnterPlayMode();yield return Boot();var persistence=station.GetComponent<StationPersistence>();var path=Temporary("S3");persistence.ConfigureDirectory(path,"S3");Assert.That(persistence.Files.DirectoryPath,Is.EqualTo(path));Assert.That(persistence.StageLabel,Is.EqualTo("S3"));var original=persistence.Files;
   foreach(var label in new[]{null,"","S4"})Assert.Throws<ArgumentException>(()=>persistence.ConfigureDirectory(Temporary("invalid"),label));Assert.That(persistence.Files,Is.SameAs(original));Assert.That(persistence.StageLabel,Is.EqualTo("S3"));Assert.That(Directory.Exists(path),Is.False);
  }
  [UnityTest]public IEnumerator TransactionCannotSwitchDirectoryOrStageHalfwayThroughRestore()
  {
   yield return new EnterPlayMode();yield return Boot();var persistence=station.GetComponent<StationPersistence>();var path=Temporary("S3");persistence.ConfigureDirectory(path,"S3");var busy=typeof(StationPersistence).GetProperty("Busy");busy.SetValue(persistence,true);
   try{Assert.Throws<InvalidOperationException>(()=>persistence.ConfigureDirectory(Temporary("other"),"S2"));Assert.That(persistence.Files.DirectoryPath,Is.EqualTo(path));Assert.That(persistence.StageLabel,Is.EqualTo("S3"));}finally{busy.SetValue(persistence,false);}
  }
 }
}
