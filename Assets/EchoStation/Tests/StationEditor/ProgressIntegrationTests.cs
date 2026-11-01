using System.Collections;
using EchoStation.Dialogue.Integration;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Puzzles;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class ProgressIntegrationTests:StationPlayFixture
 {
  [UnityTest]public IEnumerator F01_B01_ActualBootAuditsAndShortcutsRecordSameAgreements()
  {
   yield return new EnterPlayMode();yield return Boot();Assert.That(ProgressAudit.Validate(station.CaptureProgressState()),Is.Null);var initial=station.Memories.Store.Events.Count;yield return Press(Key.Digit1);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));Assert.That(station.Memories.Store.Events.Count,Is.EqualTo(initial+1));Assert.That(station.Memories.Store.TryGetCharacter("echo_position_agreement",out var agreement),Is.True);Assert.That(agreement.Summary,Does.Contain("跟随"));yield return Press(Key.Digit2);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));Assert.That(station.Memories.Store.TryGetCharacter("echo_position_agreement",out agreement),Is.True);Assert.That(agreement.Summary,Does.Contain("待命"));Assert.That(ProgressAudit.Validate(station.CaptureProgressState()),Is.Null);
  }
  [UnityTest]public IEnumerator F01_B02_OrdinaryBudgetStopsAt500WithoutEvictionOrBlockingAgreementsAndLeaves12Slots()
  {
   yield return new EnterPlayMode();yield return Boot();var store=station.Memories.Store;for(var i=store.Events.Count;i<499;i++)Assert.That(store.AddEvent("fixture_"+i,0,"夹具容量记录。"),Is.EqualTo(MemoryWriteResult.Added));var first=store.Events[0];var controller=station.GetComponent<ScriptDialogueController>();Assert.That(controller.SubmitPlayerIntent(ScriptCommands.Follow()),Is.True);Assert.That(store.Events.Count,Is.EqualTo(500));Assert.That(controller.SubmitPlayerIntent(ScriptCommands.Wait()),Is.True);Assert.That(store.Events.Count,Is.EqualTo(500));Assert.That(controller.LastMemoryError,Does.Contain("空间已保留"));Assert.That(store.TryGetCharacter("echo_position_agreement",out var agreement),Is.True);Assert.That(agreement.Summary,Does.Contain("待命"));for(var i=0;i<12;i++)Assert.That(store.AddEvent("required_fixture_"+i,0,"必需流程事件容量验证。",MemoryPriority.Critical),Is.EqualTo(MemoryWriteResult.Added));Assert.That(store.Events.Count,Is.EqualTo(512));Assert.That(store.Events[0],Is.SameAs(first));Assert.That(controller.SubmitPlayerIntent(ScriptCommands.Follow()),Is.True);Assert.That(store.Events.Count,Is.EqualTo(512));Assert.That(store.TryGetCharacter("echo_position_agreement",out agreement),Is.True);Assert.That(agreement.Summary,Does.Contain("跟随"));Assert.That(controller.LastDispatchedKind,Is.EqualTo(IntentKind.Follow));
  }
 }
}
