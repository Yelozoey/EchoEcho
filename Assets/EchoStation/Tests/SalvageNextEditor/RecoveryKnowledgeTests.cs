using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using EchoStation.Salvage;

namespace EchoStation.SalvageNext.Tests
{
    public sealed class RecoveryKnowledgeTests
    {
        static ObservedTarget Cargo(string id,double? distance=5,bool known=true,CargoState state=CargoState.World)=>new ObservedTarget(id,known,state,distance.HasValue,distance);
        static RecoveryKnowledge Snapshot(params ObservedTarget[] rows)=>new RecoveryKnowledge(1,1,RecoveryPreference.Nearby,0,0,rows);
        static string[] Rank(RecoveryKnowledge s,RecoveryPreference p)=>s.Suggestions(p).Select(f=>f.Id).ToArray();
        [Test] public void UnknownFactsHaveNoLocationOrSummary()
        {
            var snapshot=Snapshot(Cargo("cargo_a",null,false),Cargo("cargo_b"));
            Assert.That(snapshot.Facts.Select(f=>f.Id),Is.EqualTo(new[]{"cargo_b"}));
            Assert.Throws<ArgumentException>(()=>Cargo("cargo_a",1,false));
        }
        [Test] public void ThreePreferenceOrdersUseCanonicalValues()
        {
            var s=Snapshot(Cargo("cargo_a",3),Cargo("cargo_b",2),Cargo("cargo_c",3),Cargo("cargo_d",9),Cargo("cargo_e",8),Cargo("cargo_f",4));
            Assert.That(Rank(s,RecoveryPreference.Nearby),Is.EqualTo(new[]{"cargo_b","cargo_c","cargo_a","cargo_f","cargo_e","cargo_d"}));
            Assert.That(Rank(s,RecoveryPreference.HighScore),Is.EqualTo(new[]{"cargo_e","cargo_d","cargo_f","cargo_b","cargo_c","cargo_a"}));
            Assert.That(Rank(s,RecoveryPreference.Lightweight),Is.EqualTo(new[]{"cargo_c","cargo_a","cargo_f","cargo_b","cargo_d","cargo_e"}));
            Assert.That(s.Preference,Is.EqualTo(RecoveryPreference.Nearby));
        }
        [Test] public void ExcludesHeldDeliveredUnreachableDangerAndUnknown()
        {
            var s=Snapshot(Cargo("cargo_a",1,true,CargoState.Held),Cargo("cargo_b",1,true,CargoState.Delivered),Cargo("cargo_c",null),Cargo("cargo_d",null,false),new ObservedTarget("danger_power_box",true,null,true,1),Cargo("cargo_e",5));
            foreach(RecoveryPreference p in Enum.GetValues(typeof(RecoveryPreference))) Assert.That(Rank(s,p),Is.EqualTo(new[]{"cargo_e"}));
            Assert.Throws<ArgumentException>(()=>Cargo("cargo_heavy"));
        }
        [Test] public void EmptyKnownSetReturnsNoInventedSuggestion()=>Assert.That(Snapshot().Suggestions(RecoveryPreference.HighScore),Is.Empty);
        [Test] public void DuplicateTargetsAndNullRowsReject(){
            Assert.Throws<ArgumentException>(()=>Snapshot(Cargo("cargo_a"),Cargo("cargo_a")));
            Assert.Throws<ArgumentException>(()=>Snapshot(new ObservedTarget[]{null}));
            Assert.Throws<ArgumentNullException>(()=>Snapshot(null));
        }
        [TestCase(double.NaN)] [TestCase(double.PositiveInfinity)] [TestCase(-1)]
        public void InvalidDistancesReject(double distance)=>Assert.Throws<ArgumentException>(()=>Cargo("cargo_a",distance));
        [Test] public void MismatchedKindsAndReachabilityReject(){
            Assert.Throws<ArgumentException>(()=>new ObservedTarget("danger_power_box",true,CargoState.World,false,null));
            Assert.Throws<ArgumentException>(()=>new ObservedTarget("cargo_a",true,null,false,null));
            Assert.Throws<ArgumentException>(()=>new ObservedTarget("cargo_a",true,(CargoState)42,false,null));
            Assert.Throws<ArgumentException>(()=>new ObservedTarget("cargo_a",true,CargoState.World,true,null));
        }
        [TestCase(0,1,0,0)] [TestCase(1,0,0,0)] [TestCase(1,1,7,28)] [TestCase(1,1,0,2)] [TestCase(1,1,2,29)]
        public void InvalidMetadataReject(long epoch,long revision,int count,int points)=>Assert.Throws<ArgumentException>(()=>new RecoveryKnowledge(epoch,revision,RecoveryPreference.Nearby,count,points,Array.Empty<ObservedTarget>()));
        [Test] public void PreferenceAndTargetValidationRejectUnknownValues(){
            Assert.Throws<ArgumentException>(()=>Snapshot().Suggestions((RecoveryPreference)40));
            Assert.Throws<ArgumentException>(()=>new RecoveryKnowledge(1,1,(RecoveryPreference)40,0,0,Array.Empty<ObservedTarget>()));
            Assert.Throws<ArgumentException>(()=>Cargo("door_01"));
        }
        [Test] public void SnapshotHasNoMutableSourceAliasAndOnlyCanonicalSummary()
        {
            var rows=new[]{Cargo("cargo_a")}; var s=Snapshot(rows); rows[0]=Cargo("cargo_b");
            Assert.That(s.Facts[0].Id,Is.EqualTo("cargo_a"));
            Assert.That(s.Facts[0].Summary,Is.EqualTo("常规维修工具，适合基础维护。"));
            Assert.Throws<NotSupportedException>(()=>((IList<KnownRecoveryFact>)s.Facts).Clear());
            Assert.That(s.Facts[0].Kilograms,Is.EqualTo(2));Assert.That(s.Facts[0].Points,Is.EqualTo(2));
        }
    }
}
