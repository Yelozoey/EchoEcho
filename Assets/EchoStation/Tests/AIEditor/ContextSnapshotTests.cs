using System;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.Memory;
using NUnit.Framework;

namespace EchoStation.Tests.AI
{
 public sealed class ContextSnapshotTests
 {
  private static TargetCandidate Target(string id="panel_01",string zone="cabin",bool active=true,bool investigate=true,bool danger=false,bool visible=true,double distance=1,string name="供电面板")=>new TargetCandidate(id,name,zone,active,investigate,danger,visible,distance);
  private static InferenceSnapshot Snapshot(MemoryStore store,params TargetCandidate[] targets)=>new InferenceSnapshot(store,"调查供电面板",10,"cabin",targets);
  [Test]public void CapturedMemoryAndTargetsStayStableWhenSourceChanges()
  {
   var store=new MemoryStore();store.AddClue(new ClueMemory("note_a","笔记","只是一条摘要",MemoryClueKind.Note));store.AddCharacter(new CharacterMemory("state_a",CharacterMemoryKind.PlayerKnowledge,"旧状态"));var targets=new[]{Target()};var old=Snapshot(store,targets);store.AddClue(new ClueMemory("note_b","另一笔记","新摘要",MemoryClueKind.Note));store.UpdateCharacter(new CharacterMemory("state_a",CharacterMemoryKind.PlayerKnowledge,"新状态"));targets[0]=Target("other_01");
   Assert.That(old.Known.Clues.Count,Is.EqualTo(1));Assert.That(old.Known.Characters[0].Summary,Is.EqualTo("旧状态"));Assert.That(old.Targets[0].Id,Is.EqualTo("panel_01"));Assert.That(Snapshot(store).Known.Clues.Count,Is.EqualTo(2));
  }
  [TestCase("cabin",true,true,false,true,1)]
  [TestCase("cabin",true,false,true,true,1)]
  [TestCase("cabin",true,false,false,true,0)]
  [TestCase("cabin",false,true,true,true,0)]
  [TestCase("cabin",true,true,false,false,0)]
  [TestCase("lab_a",true,true,false,true,0)]
  public void UnknownTargetsNeedCurrentZoneVisibilityAndPermission(string zone,bool active,bool investigate,bool danger,bool visible,int expected)
  {var s=Snapshot(new MemoryStore(),Target(zone:zone,active:active,investigate:investigate,danger:danger,visible:visible));Assert.That(s.Targets.Count,Is.EqualTo(expected));}
  [Test]public void OnlyExactKnownClueOrDangerReferenceAllowsDistantInvisibleTargets()
  {
   var store=new MemoryStore();store.AddClue(new ClueMemory("scan_a","扫描","指向校准架",MemoryClueKind.Scan,keyMarker:"distant_01"));store.AddCharacter(new CharacterMemory("known_hazard",CharacterMemoryKind.KnownDanger,"已知危险", "hazard_01"));store.AddCharacter(new CharacterMemory("agreement",CharacterMemoryKind.Agreement,"约定", "other_01"));
   var s=Snapshot(store,Target("distant_01","lab_a",visible:false),Target("hazard_01","vent",investigate:false,danger:true,visible:false),Target("other_01","core",visible:false));Assert.That(s.Targets.Select(t=>t.Id),Is.EquivalentTo(new[]{"distant_01","hazard_01"}));
  }
  [Test]public void KnownReferenceCannotBypassUnavailableOrForbiddenTarget()
  {var store=new MemoryStore();store.AddClue(new ClueMemory("scan_a","扫描","摘要",MemoryClueKind.Scan,keyMarker:"panel_01"));Assert.That(Snapshot(store,Target(active:false)).Targets,Is.Empty);Assert.That(Snapshot(store,Target(investigate:false,danger:false)).Targets,Is.Empty);}
  [Test]public void SummaryAndPasswordStringsDoNotCreateTargetAuthority()
  {var store=new MemoryStore();store.AddClue(new ClueMemory("note_a","hidden_01","hidden_01",MemoryClueKind.Note,"hidden_01","hidden_010"));Assert.That(Snapshot(store,Target("hidden_01","core",visible:false)).Targets,Is.Empty);}
  [Test]public void EightTargetCapAndExplicitMentionRankingAreStable()
  {var store=new MemoryStore();var targets=Enumerable.Range(0,12).Reverse().Select(i=>Target("panel_"+i,distance:i,name:"设备"+i)).ToArray();var s=new InferenceSnapshot(store,"调查panel_11",0,"cabin",targets);Assert.That(s.Targets.Count,Is.EqualTo(8));Assert.That(s.Targets[0].Id,Is.EqualTo("panel_11"));Assert.That(s.Targets.Skip(1).Select(t=>t.Id),Is.EqualTo(Enumerable.Range(0,7).Select(i=>"panel_"+i)));}
  [Test]public void DuplicateNullAndTooManyCandidatesFailClosed()
  {Assert.Throws<ArgumentException>(()=>Snapshot(new MemoryStore(),Target(),Target()));Assert.Throws<ArgumentException>(()=>Snapshot(new MemoryStore(),new TargetCandidate[]{null}));Assert.Throws<ArgumentException>(()=>Snapshot(new MemoryStore(),Enumerable.Range(0,129).Select(i=>Target("p_"+i)).ToArray()));}
  [Test]public void BadTargetValuesAndRequestBoundsAreRejected()
  {
   Assert.Throws<ArgumentException>(()=>Target("Bad-ID"));Assert.Throws<ArgumentException>(()=>Target(zone:"bad-zone"));Assert.Throws<ArgumentException>(()=>Target(distance:double.NaN));Assert.Throws<ArgumentException>(()=>Target(distance:-1));Assert.Throws<ArgumentException>(()=>Target(name:new string('中',81)));Assert.Catch<ArgumentException>(()=>Target(name:"\ud800"));
   Assert.Throws<ArgumentException>(()=>new InferenceSnapshot(new MemoryStore(),new string('中',201),0,"cabin",Array.Empty<TargetCandidate>()));Assert.Throws<ArgumentException>(()=>new InferenceSnapshot(new MemoryStore()," ",0,"cabin",Array.Empty<TargetCandidate>()));Assert.Catch<ArgumentException>(()=>new InferenceSnapshot(new MemoryStore(),"\ud800",0,"cabin",Array.Empty<TargetCandidate>()));Assert.Throws<ArgumentException>(()=>new InferenceSnapshot(new MemoryStore(),"问题",double.PositiveInfinity,"cabin",Array.Empty<TargetCandidate>()));
  }
  [Test]public void SnapshotContractHasNoOriginalJournalTextField()
  {Assert.That(typeof(InferenceSnapshot).GetProperties().Select(p=>p.Name),Does.Not.Contain("FullText"));Assert.That(typeof(MemoryRecord).GetProperties().Select(p=>p.Name),Does.Not.Contain("FullText"));Assert.That(typeof(AllowedTarget).GetProperties().Length,Is.EqualTo(4));}
 }
}
