using System;
using EchoStation.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace EchoStation.Tests.Echo
{
    public class ScanWaveStateTests
    {
        [Test]public void E12_A01_WaveHasExactDurationRadiusAndNoIdleAdvance()
        {
            var wave=new ScanWaveState();Assert.That(wave.Active,Is.False);wave.Tick(2);Assert.That(wave.Radius,Is.Zero);wave.Begin(Vector3.one);Assert.That(wave.Active,Is.True);wave.Tick(.6f);Assert.That(wave.Radius,Is.EqualTo(3).Within(.0001f));wave.Tick(.6f);Assert.That(wave.Active,Is.False);Assert.That(wave.Radius,Is.EqualTo(6));
        }
        [Test]public void E12_A02_RepeatedRequestReplacesSingleWaveAndCancelStopsIt()
        {
            var wave=new ScanWaveState();wave.Begin(Vector3.one);wave.Tick(.6f);wave.Begin(Vector3.left);Assert.That(wave.Generation,Is.EqualTo(2));Assert.That(wave.Origin,Is.EqualTo(Vector3.left));Assert.That(wave.Radius,Is.Zero);wave.Cancel();wave.Tick(.2f);Assert.That(wave.Active,Is.False);Assert.That(wave.Radius,Is.Zero);
        }
        [Test]public void E12_A03_NonfiniteAndNegativeInputsAreRejectedWithoutMutation()
        {
            var wave=new ScanWaveState();Assert.Throws<ArgumentException>(()=>wave.Begin(new Vector3(float.NaN,0,0)));wave.Begin(Vector3.zero);Assert.Throws<ArgumentException>(()=>wave.Tick(-1));Assert.Throws<ArgumentException>(()=>wave.Tick(float.PositiveInfinity));Assert.That(wave.Generation,Is.EqualTo(1));Assert.That(wave.Radius,Is.Zero);
        }
    }
}
