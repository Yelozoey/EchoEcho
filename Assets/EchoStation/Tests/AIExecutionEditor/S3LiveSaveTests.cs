using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using EchoStation.AI.Execution;
using EchoStation.AI.GameIntegration;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.Echo;
using EchoStation.Station;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public sealed class S3LiveSaveTests:S3SaveFixture
 {
  [Serializable]private sealed class Report{public bool completed,ownerCleanupComplete;public int main,owner,nativeThread,delivered;public double coldSeconds;public string[] raw;}
  private S3DialogueDriver driver;private Report report;private string evidence;private readonly List<string> raw=new List<string>();
  private void SaveReport(){if(report==null||evidence==null)return;report.raw=raw.ToArray();File.WriteAllText(evidence,JsonUtility.ToJson(report,true));}
  private IEnumerator Send(string input,LlmIntentKind kind)
  {
   yield return Until(()=>driver.Session.Feedback==FeedbackPhase.Ready,25,"Native restored session ready");Assert.That(station.Dialogue.SetOpen(true),Is.True);var before=driver.Session.DeliveredGenerationCount;station.Dialogue.InputField.text=input;Assert.That(station.Dialogue.SubmitInput(),Is.True);yield return Until(()=>driver.Session.DeliveredGenerationCount==before+1,25,"Native save response");raw.Add(driver.Session.LastRawOutput);report.owner=driver.Session.OwnerThreadId;report.nativeThread=driver.Session.Activity.ThreadId;report.delivered=driver.Session.DeliveredGenerationCount;SaveReport();var parsed=IntentJsonParser.Parse(raw[raw.Count-1]);Assert.That(parsed.Success,Is.True);Assert.That(parsed.Intent.Kind,Is.EqualTo(kind));Assert.That(driver.Session.LastApplication.Accepted,Is.True);Assert.That(report.nativeThread,Is.EqualTo(report.owner));Assert.That(report.owner,Is.Not.EqualTo(report.main));
  }
  [UnityTest]public IEnumerator RealQwen3SurvivesS3StateRestoreButModelChoiceAndRequestsAreNeverPersisted()
  {
   yield return new EnterPlayMode();yield return BootS3();BindTemporarySlot();var fresh=persistence.Capture();report=new Report{main=Thread.CurrentThread.ManagedThreadId};var folder=Path.GetFullPath("Docs/Tests/Stage3/Evidence/S3-S01d");Directory.CreateDirectory(folder);evidence=Path.Combine(folder,"NativeSave_"+DateTime.UtcNow.Ticks+".json");SaveReport();driver=station.GetComponent<S3DialogueDriver>();
   var controls=station.GetComponent<StationControls>();Assert.That(controls.SetOpen(true),Is.True);var watch=System.Diagnostics.Stopwatch.StartNew();station.GetComponent<LlmModeToggle>().Button.onClick.Invoke();Assert.That(GameRuntimeOptions.UseLLM,Is.True);controls.SetOpen(false);yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,35,"S3 native cold load");report.coldSeconds=watch.Elapsed.TotalSeconds;SaveReport();
   yield return Send("跟随我",LlmIntentKind.Follow);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(persistence.TrySave(),Is.True,persistence.LastMessage);var saved=persistence.Files.Read().Data;var bytes=File.ReadAllBytes(persistence.Files.PrimaryPath);Assert.That(File.ReadAllText(persistence.Files.PrimaryPath),Does.Not.Contain("UseLLM").And.Not.Contain("past_key_values"));
   yield return persistence.RestoreData(fresh);Assert.That(persistence.LastSucceeded,Is.True,persistence.LastMessage);yield return persistence.Load();Assert.That(persistence.LastSucceeded,Is.True,persistence.LastMessage);Assert.That(station.Memories.CaptureJson(),Is.EqualTo(saved.MemoryJson));Assert.That(GameRuntimeOptions.UseLLM,Is.True,"Reading progress must not change the currently selected mode");yield return Send("原地待命",LlmIntentKind.Wait);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));
   driver.SetEnabled(false);yield return Until(()=>driver.Session.CleanupComplete,35,"S3 native owner release");report.ownerCleanupComplete=true;yield return persistence.Load();Assert.That(persistence.LastSucceeded,Is.True,persistence.LastMessage);Assert.That(GameRuntimeOptions.UseLLM,Is.False,"Slot saved during LLM on must not reenable it");Assert.That(driver.Session.DeliveredGenerationCount,Is.EqualTo(2));Assert.That(File.ReadAllBytes(persistence.Files.PrimaryPath),Is.EqualTo(bytes));report.completed=true;SaveReport();
  }
  [UnityTearDown]public IEnumerator FinishNativeReport()
  {if(driver&&driver.Ready){driver.enabled=false;driver.Session.Dispose();yield return Until(()=>driver.Session.CleanupComplete,40,"Native save cleanup");if(report!=null)report.ownerCleanupComplete=true;}SaveReport();}
 }
}
