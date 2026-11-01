using System.Collections;
using EchoStation.AI.GameIntegration;
using EchoStation.AI.Inference;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EchoStation.Tests.AIExecution
{
 public abstract class S3PendingSaveFixture:S3SaveFixture
 {
  protected S3DialogueDriver driver;internal S3DialogueDriverTests.Factory factory;internal S3DialogueDriverTests.Clock clock;
  protected IEnumerator UseControlledOwner()
  {
   var original=station.GetComponent<S3DialogueDriver>();Assert.That(original.Session.OwnerThreadId,Is.Zero,"Default S3 has never loaded native resources");original.enabled=false;original.Session.Dispose();Object.Destroy(original);var mode=station.GetComponent<LlmModeToggle>();if(mode)Object.Destroy(mode);yield return R04ShadowProbe.Frame();
   factory=new S3DialogueDriverTests.Factory();clock=new S3DialogueDriverTests.Clock();driver=station.gameObject.AddComponent<S3DialogueDriver>();driver.ConfigureForTests(factory,clock);yield return Until(()=>driver.Ready,5,"Controlled save driver");factory.Work.Reset();driver.SetEnabled(true);yield return Until(()=>driver.Session.Phase==RequestPhase.Ready,5,"Controlled save Owner ready");
  }
  protected IEnumerator StartPendingFollow()
  {Assert.That(station.Dialogue.SetOpen(true),Is.True);station.Dialogue.InputField.text="跟随我";Assert.That(station.Dialogue.SubmitInput(),Is.True);yield return Until(()=>System.Threading.Volatile.Read(ref factory.Requests)==1,5,"Controlled pending request");}
  [UnityTearDown]public IEnumerator ReleaseControlledOwner()
  {if(factory!=null){factory.Load.Set();factory.Work.Set();}if(driver&&driver.Ready){driver.enabled=false;driver.Session.Dispose();yield return Until(()=>driver.Session.CleanupComplete,5,"Controlled save Owner release");Assert.That(factory.Disposed,Is.EqualTo(factory.Creates));}}
 }
}
