using System.Collections;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Puzzles;
using EchoStation.Rendering;
using EchoStation.Station;
using EchoStation.Tests.Fixtures;
using EchoStation.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
namespace EchoStation.Tests.Station
{
 public sealed class PowerIntegrationTests:StationPlayFixture
 {
  private IEnumerator Open(){Move(new Vector3(-4.5f,-3.95f,26.8f));WorldTarget.Active.TryGet("power_panel_01",out var panel);Look(panel.transform.position);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);yield return Press(Key.E);Assert.That(station.GetComponent<StationPowerPanel>().IsOpen,Is.True);}
  private StationGate Barrier()=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StationGate>()).Single(g=>g.name=="Vent_Isolation");
  [UnityTest]public IEnumerator P03_C01_PrerequisiteWrongChoicesAndCancelHaveNoPowerOrPassageEffect()
  {
   yield return new EnterPlayMode();yield return Boot();yield return Open();var ui=station.GetComponent<StationPowerPanel>();Assert.That(ui.Submit(),Is.EqualTo(PuzzleResult.PrerequisiteMissing));Assert.That(ui.Cycle.Powered,Is.False);Assert.That(Barrier().IsOpen,Is.False);yield return Press(Key.T,Key.J,Key.Tab);Assert.That(station.Dialogue.IsOpen,Is.False);Assert.That(station.GetComponent<JournalPanel>().IsOpen,Is.False);yield return Press(Key.Escape);Assert.That(ui.IsOpen,Is.False);Assert.That(station.GetComponent<PlayerInputReader>().UiBlocked,Is.False);
   station.Progress.TryRestore(new[]{"pz02"});yield return Open();Assert.That(ui.Submit(),Is.EqualTo(PuzzleResult.WrongCircuit));Assert.That(station.Progress.Has("pz03"),Is.False);station.GetComponent<PlayerInputReader>().SetApplicationFocus(false);yield return R04ShadowProbe.Frame();Assert.That(ui.IsOpen,Is.False);Assert.That(Barrier().IsOpen,Is.False);
  }
  [UnityTest]public IEnumerator P03_C02_ActualPlayerPanelClearsDensityForThreeSecondsThenOpensWalkablePassageOnce()
  {
   yield return new EnterPlayMode();yield return Boot();station.Progress.TryRestore(new[]{"pz02"});yield return Open();var ui=station.GetComponent<StationPowerPanel>();foreach(var i in new[]{0,1,2})ui.ToggleButton(i).onClick.Invoke();var started=Time.realtimeSinceStartup;ui.SubmitButton.onClick.Invoke();Assert.That(ui.Cycle.Powered,Is.True);Assert.That(ui.Cycle.Elapsed,Is.Zero);Assert.That(Barrier().IsOpen,Is.False);Assert.That(station.Progress.Has("pz03"),Is.True);
   yield return Until(()=>ui.Cycle.Elapsed>=1,4,"Vent clock did not advance");Assert.That(ui.Cycle.PassageReady,Is.False);Assert.That(Barrier().IsOpen,Is.False);var field=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LocalFogField>()).Single();Assert.That(field.Recovery,Is.InRange(.3f,.7f));yield return Until(()=>ui.Cycle.PassageReady,6,"Vent did not clear in game time");Assert.That(Time.realtimeSinceStartup-started,Is.GreaterThanOrEqualTo(2.8f));Assert.That(Barrier().IsOpen,Is.True);Assert.That(field.Recovery,Is.EqualTo(1));Assert.That(field.VentDensity,Is.EqualTo(.015f).Within(1e-6));Assert.That(ui.Submit(),Is.EqualTo(PuzzleResult.AlreadyComplete));Assert.That(station.Memories.Store.Events.Count(e=>e.Id=="progress_pz03"),Is.EqualTo(1));yield return Press(Key.Escape);
   Move(new Vector3(0,-3.95f,37.2f));station.Player.SetLookAngles(0,0);station.GetComponent<PlayerInputReader>().SetPointerCaptured(true);yield return Hold(.7f,Key.W);Assert.That(station.transform.position.z,Is.GreaterThan(38.3f));yield return Hold(.7f,Key.S);Assert.That(station.transform.position.z,Is.LessThan(38));Assert.That(Barrier().IsOpen,Is.True);
  }
  [UnityTest]public IEnumerator P03_C03_ActualEchoPanelScanOnlyAddsInformationAndCannotSubmitCircuit()
  {
   yield return new EnterPlayMode();yield return Boot();Move(new Vector3(-2,-3.95f,27));Assert.That(station.Echo.GetComponent<EchoNavigation>().TryBind(new Vector3(-4.5f,-4,27.3f)),Is.True);Assert.That(station.Echo.TrySubmit(ScriptCommands.Investigate("power_panel_01"),out _),Is.True);yield return Until(()=>station.Echo.GetComponent<EchoScanController>().Scanner.Ledger.HasDiscovered("power_panel_01"),8,"Actual panel scan failed");Assert.That(station.Memories.Store.TryGetClue("power_panel_01",out _),Is.True);Assert.That(station.Progress.Has("pz03"),Is.False);Assert.That(station.GetComponent<StationPowerPanel>().Cycle.Powered,Is.False);Assert.That(Barrier().IsOpen,Is.False);
  }
 }
}
