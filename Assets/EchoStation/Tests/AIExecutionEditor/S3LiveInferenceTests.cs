using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EchoStation.AI.GameIntegration;
using EchoStation.AI.Inference;
using EchoStation.AI.IntentJson;
using EchoStation.Echo;
using EchoStation.Station;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Station;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public sealed class S3LiveInferenceTests:StationPlayFixture
 {
  [Serializable]private sealed class Sample
  {public string input,expected,expectedTarget,raw,actual,actualTarget,guard,deliveredKind,visibleReply;public bool json,semantic,accepted;public double seconds;public int owner,nativeThread,nativeCalls;}
  [Serializable]private sealed class Report
  {public string scope="Real Qwen3 formal factory -> live station dialogue -> guarded traditional execution; controlled positioning, not a full ending route";public bool completed,ownerCleanupComplete;public int main,owner,count,jsonCorrect,semanticCorrect,accepted;public double coldSeconds;public Sample[] samples;}
  private S3DialogueDriver driver;private readonly List<Sample> samples=new List<Sample>();private Report report;private string evidence;
  private void Save()
  {if(report==null||evidence==null)return;report.samples=samples.ToArray();report.count=samples.Count;report.jsonCorrect=samples.Count(s=>s.json);report.semanticCorrect=samples.Count(s=>s.semantic);report.accepted=samples.Count(s=>s.accepted);File.WriteAllText(evidence,JsonUtility.ToJson(report,true));}
  private IEnumerator Send(string input,LlmIntentKind expected,string target=null)
  {
   yield return Until(()=>driver.Session.Feedback==EchoStation.AI.Execution.FeedbackPhase.Ready,25,"Real session ready/cooldown");var previous=driver.Session.DeliveredGenerationCount;station.Dialogue.InputField.text=input;Assert.That(station.Dialogue.SubmitInput(),Is.True);
   yield return Until(()=>driver.Session.DeliveredGenerationCount==previous+1,25,"Real Qwen3 response: "+input);
   var parsed=IntentJsonParser.Parse(driver.Session.LastRawOutput);var actual=parsed.Intent;var actualTarget=actual?.TargetName??actual?.DangerTarget;var application=driver.Session.LastApplication;var activity=driver.Session.Activity;
   var sample=new Sample{input=input,expected=expected.ToString(),expectedTarget=target,raw=driver.Session.LastRawOutput,json=parsed.Success,actual=actual?.Kind.ToString(),actualTarget=actualTarget,semantic=parsed.Success&&actual.Kind==expected&&actualTarget==target,guard=application.Guard?.ToString(),deliveredKind=application.DeliveredKind?.ToString(),accepted=application.Accepted,visibleReply=station.Dialogue.Session.Messages.Last().Text,seconds=driver.Session.LastInferenceSeconds,owner=driver.Session.OwnerThreadId,nativeThread=activity.ThreadId,nativeCalls=activity.Calls};samples.Add(sample);report.owner=sample.owner;Save();
   Assert.That(sample.owner,Is.Not.EqualTo(report.main));Assert.That(sample.nativeThread,Is.EqualTo(sample.owner));Assert.That(sample.nativeCalls,Is.GreaterThan(0));
  }
  [UnityTest]public IEnumerator FormalQwen3ProducesSixRealIntentsAndTraditionalGameEffects()
  {
   yield return new EnterPlayMode();yield return Boot();var directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Tests/Stage3/Evidence/S3-E03c3"));Directory.CreateDirectory(directory);evidence=Path.Combine(directory,"LiveSix_"+DateTime.UtcNow.Ticks+".json");report=new Report{main=Thread.CurrentThread.ManagedThreadId};Save();
   var gates=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StationGate>()).ToArray();var gateStates=gates.Select(g=>g.IsOpen).ToArray();driver=station.gameObject.AddComponent<S3DialogueDriver>();yield return Until(()=>driver.Ready,5,"Real driver initialization");Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(driver.Session.OwnerThreadId,Is.Zero);Assert.That(station.Dialogue.SetOpen(true),Is.True);
   var cold=System.Diagnostics.Stopwatch.StartNew();Assert.That(driver.SetEnabled(true),Is.True);yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,35,"Real Qwen3 cold load");report.coldSeconds=cold.Elapsed.TotalSeconds;Save();
   yield return Send("跟随我",LlmIntentKind.Follow);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));yield return Send("原地待命",LlmIntentKind.Wait);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));
   WorldTarget.Active.TryGet("note_entry_inspection_01",out var entry);Move(new Vector3(-4,.05f,-31.8f));Look(entry.transform.position);station.RefreshZone();yield return R04ShadowProbe.Frame();yield return Send("扫描入口巡检记录",LlmIntentKind.Investigate,entry.Id);
   if(samples.Last().semantic&&samples.Last().accepted)yield return Until(()=>station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered(entry.Id),18,"Real model requested scan completes");
   Move(new Vector3(-4.5f,-3.95f,27.1f));WorldTarget.Active.TryGet("power_panel_01",out var danger);Look(danger.transform.position);station.RefreshZone();station.Echo.TrySubmit(ScriptCommands.Wait(),out _);Assert.That(station.Echo.GetComponent<EchoNavigation>().TryBind(new Vector3(-4.5f,-4,27.3f)),Is.True);var facing=danger.transform.position-station.Echo.transform.position;facing.y=0;station.Echo.transform.rotation=Quaternion.LookRotation(facing);Physics.SyncTransforms();scene.GetPhysicsScene().Simulate(.02f);yield return R04ShadowProbe.Frame();
   yield return Send("警告A区通风供电面板",LlmIntentKind.Warn,danger.Id);yield return Send("你是谁",LlmIntentKind.Answer);yield return Send("替我打开门",LlmIntentKind.Refuse);
   Assert.That(samples.Count,Is.EqualTo(6));Assert.That(samples.Count(s=>s.json),Is.EqualTo(6));Assert.That(samples.Count(s=>s.semantic),Is.EqualTo(6));Assert.That(samples.Count(s=>s.accepted),Is.EqualTo(6));Assert.That(station.Memories.Store.TryGetClue(entry.Id,out _),Is.True);Assert.That(station.Memories.Store.Characters.Any(c=>c.Kind==EchoStation.Memory.CharacterMemoryKind.KnownDanger&&c.TargetId==danger.Id),Is.True);CollectionAssert.AreEqual(gateStates,gates.Select(g=>g.IsOpen).ToArray());
   driver.SetEnabled(false);yield return Until(()=>driver.Session.CleanupComplete,35,"Real model release");report.ownerCleanupComplete=true;report.completed=true;Save();
  }
  [UnityTearDown]public IEnumerator ReleaseRealOwnerBeforeScene()
  {
   if(driver&&driver.Ready){driver.enabled=false;var session=driver.Session;session.Dispose();yield return Until(()=>session.CleanupComplete,40,"Live owner cleanup");if(report!=null)report.ownerCleanupComplete=true;}Save();
  }
 }
}
