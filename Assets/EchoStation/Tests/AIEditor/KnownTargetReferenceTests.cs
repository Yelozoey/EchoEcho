using System.Linq;
using EchoStation.AI.Context;
using EchoStation.Memory;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class KnownTargetReferenceTests
 {
  [Test]public void DefaultHiddenCrossZoneCandidateIsStillExcluded()
  {var target=new TargetCandidate("scan_mark_01","储藏室可疑点","cabin",true,true,false,false,12);Assert.That(target.KnownReference,Is.False);Assert.That(new InferenceSnapshot(new MemoryStore(),"调查储藏室可疑点",0,"valley",new[]{target}).Targets,Is.Empty);}
  [Test]public void ExplicitTrustedReferenceMayIncludeHiddenTargetWithoutChangingVisibility()
  {var target=new TargetCandidate("scan_mark_01","储藏室可疑点","cabin",true,true,false,false,12,true);var result=new InferenceSnapshot(new MemoryStore(),"调查储藏室可疑点",0,"valley",new[]{target});Assert.That(result.Targets.Single().Id,Is.EqualTo(target.Id));Assert.That(target.Visible,Is.False);}
  [TestCase(false,true,false)][TestCase(true,false,false)]
  public void KnownReferenceCannotOverrideAvailabilityOrActionPermissions(bool available,bool investigate,bool danger)
  {var target=new TargetCandidate("scan_mark_01","储藏室可疑点","cabin",available,investigate,danger,false,12,true);Assert.That(new InferenceSnapshot(new MemoryStore(),"调查储藏室可疑点",0,"valley",new[]{target}).Targets,Is.Empty);}
  [Test]public void TrustedReferencesStillObeyEightTargetBudget()
  {var targets=Enumerable.Range(0,12).Select(i=>new TargetCandidate("target_"+i,"已知目标"+i,"cabin",true,true,false,false,i,true));var result=new InferenceSnapshot(new MemoryStore(),"查询",0,"valley",targets);Assert.That(result.Targets.Count,Is.EqualTo(8));}
 }
}
