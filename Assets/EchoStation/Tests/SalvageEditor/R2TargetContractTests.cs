using System;
using System.Linq;
using EchoStation.Echo;
using NUnit.Framework;
namespace EchoStation.Salvage.Tests
{
    public class R2TargetContractTests
    {
        [Test]public void EightCanonicalTargetsPreserveApprovedCargoValues(){Assert.That(SalvageTargetCatalog.Items.Count,Is.EqualTo(8));Assert.That(SalvageTargetCatalog.Items.Select(t=>t.Id).Distinct().Count(),Is.EqualTo(8));Assert.That(SalvageTargetCatalog.Items.Count(t=>t.Kind==SalvageTargetKind.Danger),Is.EqualTo(2));Assert.That(SalvageCatalog.Items.Select(c=>c.Points),Is.EqualTo(new[]{2,4,3,6,8,5,1}));Assert.That(SalvageTargetCatalog.Items.All(t=>t.Summary.Length<=120),Is.True);}
        [TestCase(CargoState.Held)][TestCase(CargoState.Delivered)]public void NonWorldCargoCannotInvestigateOrWarn(CargoState state){Assert.That(SalvageTargetCatalog.Allows("cargo_a",IntentKind.Investigate,state),Is.False);Assert.That(SalvageTargetCatalog.Allows("cargo_a",IntentKind.Warn,state),Is.False);}
        [Test]public void OnlyInformationActionsAreAllowed(){Assert.That(SalvageTargetCatalog.Allows("cargo_a",IntentKind.Investigate,CargoState.World),Is.True);Assert.That(SalvageTargetCatalog.Allows("cargo_a",IntentKind.Warn,CargoState.World),Is.False);Assert.That(SalvageTargetCatalog.Allows("danger_power_box",IntentKind.Warn,null),Is.True);Assert.That(SalvageTargetCatalog.Allows("door_01",IntentKind.Investigate,CargoState.World),Is.False);Assert.That(SalvageTargetCatalog.Allows("cargo_heavy",IntentKind.Investigate,CargoState.World),Is.False);Assert.That(SalvageTargetCatalog.Allows("cargo_a",(IntentKind)99,CargoState.World),Is.False);}
        [TestCase("")][TestCase("BAD/target")][TestCase("../file")]
        public void InvalidDefinitionsReject(string id)=>Assert.Throws<ArgumentException>(()=>new SalvageTargetDefinition(id,"name","tool","summary",SalvageTargetKind.Cargo));
        [Test]public void UnknownInformationIsNotDisclosedAndCommitIsIdempotent(){var known=new SalvageDiscoveryIndex();Assert.That(known.Summary("cargo_a"),Is.Null);Assert.That(known.CommitValidatedSensor("cargo_a"),Is.True);Assert.That(known.CommitValidatedSensor("cargo_a"),Is.False);Assert.That(known.Count,Is.EqualTo(1));Assert.That(known.Summary("cargo_a"),Does.Contain("常规维修工具"));Assert.That(known.Summary("cargo_b"),Is.Null);}
        [Test]public void InvalidRestoreIsAtomicAndCannotInjectFacts(){var known=new SalvageDiscoveryIndex();known.CommitValidatedSensor("cargo_a");Assert.Throws<ArgumentException>(()=>known.RestoreValidatedIds(new[]{"cargo_b","unknown"}));Assert.That(known.Snapshot(),Is.EqualTo(new[]{"cargo_a"}));Assert.Throws<ArgumentException>(()=>known.RestoreValidatedIds(new[]{"cargo_b","cargo_b"}));known.RestoreValidatedIds(new[]{"danger_leak_pipe","cargo_b"});Assert.That(known.Summary("cargo_a"),Is.Null);Assert.That(known.Summary("cargo_b"),Does.Contain("标准接口备件"));}
    }
}
