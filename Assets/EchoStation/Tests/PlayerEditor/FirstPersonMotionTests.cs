using System;
using EchoStation.Player;
using NUnit.Framework;
using UnityEngine;

namespace EchoStation.Tests.Player
{
    public class FirstPersonMotionTests
    {
        [Test]public void P01_A04_SpeedAndYawAreFrameRateIndependentAndDiagonalIsNotFaster()
        {
            var a=FirstPersonMotion.HorizontalDelta(Vector2.up,0,1f/30)*30;var b=FirstPersonMotion.HorizontalDelta(Vector2.up,0,1f/144)*144;Assert.That(Vector3.Distance(a,b),Is.LessThan(.0001f));Assert.That(a.z,Is.EqualTo(3).Within(.00001f));Assert.That(FirstPersonMotion.HorizontalDelta(Vector2.one,0,1).magnitude,Is.EqualTo(3).Within(.0001f));Assert.That(Vector3.Distance(FirstPersonMotion.HorizontalDelta(Vector2.up,90,1),Vector3.right*3),Is.LessThan(.0001f));
        }
        [Test]public void P01_A05_MouseSensitivityAndPitchLimitsDoNotDependOnDeltaTime()
        {
            Assert.That(FirstPersonMotion.LookAngles(new Vector2(50,20),Vector2.zero),Is.EqualTo(new Vector2(5,-2)));Assert.That(FirstPersonMotion.LookAngles(new Vector2(0,10000),Vector2.zero).y,Is.EqualTo(-80));Assert.That(FirstPersonMotion.LookAngles(new Vector2(0,-10000),Vector2.zero).y,Is.EqualTo(80));Assert.That(FirstPersonMotion.LookAngles(new Vector2(20,0),new Vector2(359,0)).x,Is.EqualTo(1));
        }
        [Test]public void P01_A06_InvalidInputsAreRejected()
        {
            Assert.Throws<ArgumentException>(()=>FirstPersonMotion.HorizontalDelta(Vector2.up,0,-1));Assert.Throws<ArgumentException>(()=>FirstPersonMotion.HorizontalDelta(Vector2.up,float.NaN,1));Assert.Throws<ArgumentException>(()=>FirstPersonMotion.LookAngles(new Vector2(float.PositiveInfinity,0),Vector2.zero));
        }
    }
}
