using System.Collections;
using System.IO;
using System.Linq;
using EchoStation.AI.GameIntegration;
using EchoStation.Echo;
using EchoStation.Station;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EchoStation.Tests.AIExecution
{
 public sealed class S3StartupTests:S3PlayFixture
 {
  [UnityTest]public IEnumerator DedicatedSceneStartsInScriptModeWithS3DirectoryAndActualNavigation()
  {
   yield return new EnterPlayMode();yield return BootS3();var persistence=station.GetComponent<StationPersistence>();var driver=station.GetComponent<S3DialogueDriver>();Assert.That(persistence.StageLabel,Is.EqualTo("S3"));Assert.That(persistence.Files.DirectoryPath,Is.EqualTo(Path.GetFullPath(S3Bootstrap.SaveDirectory)));Assert.That(Path.GetFileName(persistence.Files.PrimaryPath),Is.EqualTo("slot1.json"));Assert.That(GameRuntimeOptions.UseLLM,Is.False);Assert.That(driver.Session.OwnerThreadId,Is.Zero);Assert.That(station.Echo.GetComponent<EchoNavigation>().IsBound,Is.True);Assert.That(station.GetComponents<S3Bootstrap>().Length,Is.EqualTo(1));
   var title=station.GetComponent<StationLifecycle>().Canvas.GetComponentsInChildren<Text>(true);Assert.That(title.Single(t=>t.name=="SaveNotice").text,Does.Contain("S3独立单槽"));Assert.That(title.Single(t=>t.name=="Footer").text,Does.Contain("离线AI与脚本模式"));Assert.That(station.GetComponent<LlmModeToggle>().Caption.text,Does.Contain("脚本模式"));Assert.That(((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).supportsHDR,Is.True);Assert.That(HDROutputSettings.main.active,Is.False);
  }
  [UnityTest]public IEnumerator ActualMovementAndScriptTextWorkWithoutInitializingModel()
  {
   yield return new EnterPlayMode();yield return BootS3();var start=station.transform.position;yield return Hold(.25f,Key.W);Assert.That(Vector3.Distance(start,station.transform.position),Is.GreaterThan(.1f));Assert.That(station.Dialogue.SetOpen(true),Is.True);station.Dialogue.InputField.text="跟随我";Assert.That(station.Dialogue.SubmitInput(),Is.True);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Follow));station.Dialogue.InputField.text="原地待命";Assert.That(station.Dialogue.SubmitInput(),Is.True);Assert.That(station.Echo.CurrentMode,Is.EqualTo(IntentKind.Wait));yield return R04ShadowProbe.Frame();Assert.That(station.GetComponent<S3DialogueDriver>().Session.OwnerThreadId,Is.Zero);Assert.That(GameRuntimeOptions.UseLLM,Is.False);
  }
 }
}
