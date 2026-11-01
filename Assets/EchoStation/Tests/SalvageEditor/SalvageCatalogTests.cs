using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace EchoStation.Salvage.Tests
{
    public class SalvageCatalogTests
    {
        [Test]public void ApprovedCatalogHasExactValuesAndUniqueIds()
        {Assert.That(SalvageCatalog.Items.Count,Is.EqualTo(7));Assert.That(SalvageCatalog.Items.Select(i=>i.Id).Distinct().Count(),Is.EqualTo(7));Assert.That(SalvageCatalog.Items.Select(i=>i.Kilograms),Is.EqualTo(new[]{2,4,2,6,8,4,12}));Assert.That(SalvageCatalog.Items.Select(i=>i.Points),Is.EqualTo(new[]{2,4,3,6,8,5,1}));}
        [TestCase("")][TestCase(" ")][TestCase("cargo/A")][TestCase("Cargo_A")]
        public void InvalidIdsCannotRegister(string id)=>Assert.Throws<ArgumentException>(()=>new CargoDefinition(id,"item",2,2));
        [Test]public void DuplicateRegistrationPreservesOriginalAndState()
        {var r=new CargoRegistry();Assert.That(r.Register(SalvageCatalog.Get("cargo_a"),Vector3.one),Is.True);Assert.That(r.Register(new CargoDefinition("cargo_a","wrong",12,99),Vector3.zero),Is.False);Assert.That(r.Count,Is.EqualTo(1));Assert.That(r.Get("cargo_a").Definition.Points,Is.EqualTo(2));Assert.That(r.Get("cargo_a").Position,Is.EqualTo(Vector3.one));Assert.That(r.Get("cargo_a").State,Is.EqualTo(CargoState.World));}
        [Test]public void UnknownAndNonfiniteCannotCreateCargo()
        {var r=new CargoRegistry();Assert.Throws<ArgumentException>(()=>r.Register(SalvageCatalog.Items[0],new Vector3(float.NaN,0,0)));Assert.Throws<ArgumentException>(()=>r.Get("unknown"));Assert.That(r.Count,Is.Zero);}
    }
}
