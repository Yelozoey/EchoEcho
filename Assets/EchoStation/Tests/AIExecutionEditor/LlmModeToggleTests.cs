using System.Collections;
using System.Linq;
using EchoStation.AI.GameIntegration;
using EchoStation.AI.Inference;
using EchoStation.Echo;
using EchoStation.Station;
using EchoStation.Tests.Fixtures;
using EchoStation.Tests.Station;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EchoStation.Tests.AIExecution
{
 public sealed class LlmModeToggleTests:StationPlayFixture
 {
  private S3DialogueDriver driver;private LlmModeToggle toggle;private S3DialogueDriverTests.Factory factory;private S3DialogueDriverTests.Clock clock;
  private IEnumerator Setup()
  {
   yield return Boot();factory=new S3DialogueDriverTests.Factory();clock=new S3DialogueDriverTests.Clock();driver=station.gameObject.AddComponent<S3DialogueDriver>();driver.ConfigureForTests(factory,clock);toggle=station.gameObject.AddComponent<LlmModeToggle>();toggle.Configure(station.GetComponent<StationControls>(),driver);Assert.That(toggle.Button.interactable,Is.False);yield return Until(()=>driver.Ready,5,"Mode driver ready");yield return R04ShadowProbe.Frame();
  }
  [UnityTest]public IEnumerator TabButtonTogglesOnlyWhenVisibleAndCaptionFitsWithoutTitleOverlap()
  {
   yield return new EnterPlayMode();yield return Setup();var controls=station.GetComponent<StationControls>();Assert.That(toggle.Caption.text,Does.Contain("关闭"));Assert.That(factory.Creates,Is.Zero);Assert.That(controls.SetOpen(true),Is.True);toggle.Button.onClick.Invoke();Assert.That(GameRuntimeOptions.UseLLM,Is.True);yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,5,"Mode starts controlled Owner");Assert.That(toggle.Caption.text,Does.Contain("开启"));controls.SetOpen(false);toggle.Button.onClick.Invoke();Assert.That(GameRuntimeOptions.UseLLM,Is.True,"Hidden panel cannot toggle");
   Assert.That(controls.SetOpen(true),Is.True);Canvas.ForceUpdateCanvases();var title=controls.Canvas.GetComponentsInChildren<Text>(true).Single(t=>t.name=="Title");Assert.That(title.rectTransform.anchoredPosition.x+title.preferredWidth,Is.LessThan(toggle.Button.GetComponent<RectTransform>().anchoredPosition.x));Assert.That(toggle.Caption.preferredHeight,Is.LessThanOrEqualTo(toggle.Caption.rectTransform.rect.height));Assert.That(toggle.Caption.supportRichText,Is.False);
   toggle.Button.onClick.Invoke();Assert.That(GameRuntimeOptions.UseLLM,Is.False);yield return Until(()=>driver.Session.CleanupComplete,5,"Mode Owner release");yield return R04ShadowProbe.Frame();Assert.That(toggle.Caption.text,Does.Contain("脚本模式"));Assert.That(factory.Creates,Is.EqualTo(1));Assert.That(factory.Disposed,Is.EqualTo(1));toggle.Configure(controls,driver);Assert.That(controls.Canvas.GetComponentsInChildren<Button>(true).Count(b=>b.name=="S3_AI_Mode"),Is.EqualTo(1));
  }
  [UnityTest]public IEnumerator AutomaticThreeFaultDowngradeUpdatesHiddenTabCaptionAndDisabledDriverCannotEnable()
  {
   yield return new EnterPlayMode();yield return Setup();factory.Raw="{bad-json";driver.SetEnabled(true);yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,5,"Controlled Owner ready");station.Dialogue.SetOpen(true);
   for(var i=1;i<=3;i++){if(i>1)clock.Advance();station.Dialogue.InputField.text="跟随我";Assert.That(station.Dialogue.SubmitInput(),Is.True);var expected=i;yield return Until(()=>driver.Session.DeliveredGenerationCount==expected,5,"Mode fault completion");}
   yield return R04ShadowProbe.Frame();Assert.That(toggle.Caption.text,Does.Contain("关闭"));Assert.That(GameRuntimeOptions.UseLLM,Is.False);driver.enabled=false;yield return R04ShadowProbe.Frame();Assert.That(toggle.Button.interactable,Is.False);toggle.Button.onClick.Invoke();Assert.That(GameRuntimeOptions.UseLLM,Is.False);
  }
  [UnityTearDown]public IEnumerator ReleaseModeDriver()
  {if(factory!=null){factory.Load.Set();factory.Work.Set();}if(driver&&driver.Ready){driver.enabled=false;driver.Session.Dispose();yield return Until(()=>driver.Session.CleanupComplete,5,"Mode driver cleanup");}}
 }
}
