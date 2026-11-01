using System;
using EchoStation.Player;
using NUnit.Framework;

namespace EchoStation.Tests.Player
{
    public class InteractionFocusStateTests
    {
        [Test]public void P06_A01_FirstTargetClampsAndFadesInThenOut()
        {
            var state=new InteractionFocusState();state.Step(.1f,.075f);Assert.That(state.Distance,Is.EqualTo(.35f));Assert.That(state.Weight,Is.EqualTo(.5f).Within(.0001f));state.Step(10,.075f);Assert.That(state.Distance,Is.InRange(.35f,2.5f));Assert.That(state.Weight,Is.EqualTo(1));var distance=state.Distance;state.Step(null,.075f);Assert.That(state.Distance,Is.EqualTo(distance));Assert.That(state.Weight,Is.EqualTo(.5f).Within(.0001f));state.Step(null,.075f);Assert.That(state.Active,Is.False);state.Step(20,0);Assert.That(state.Distance,Is.EqualTo(2.5f));
        }
        [Test]public void P06_A02_ExponentialSmoothingIsTimeStepInvariantAndResetClears()
        {
            var a=new InteractionFocusState();var b=new InteractionFocusState();a.Step(1,.15f);b.Step(1,.15f);a.Step(2,.1f);for(var i=0;i<10;i++)b.Step(2,.01f);Assert.That(a.Distance,Is.EqualTo(b.Distance).Within(.00001f));Assert.That(a.Distance,Is.InRange(1.63f,1.64f));a.Reset();Assert.That(a.Active,Is.False);Assert.That(a.Distance,Is.EqualTo(1));
        }
        [Test]public void P06_A03_InvalidInputCannotPartiallyMutateState()
        {
            var state=new InteractionFocusState();foreach(var value in new[]{0,-1,float.NaN,float.PositiveInfinity})Assert.Throws<ArgumentOutOfRangeException>(()=>state.Step(value,.1f));foreach(var dt in new[]{-1,float.NaN,float.PositiveInfinity})Assert.Throws<ArgumentOutOfRangeException>(()=>state.Step(1,dt));Assert.That(state.Weight,Is.Zero);Assert.That(state.Distance,Is.EqualTo(1));
        }
    }
}
