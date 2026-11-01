using System;
using EchoStation.Player;
using NUnit.Framework;
using UnityEngine;

namespace EchoStation.Tests.Player
{
    public class VerticalMotionTests
    {
        [Test]public void P02_A01_GroundedJumpUsesPhysicalSpeedAndAirRequestCannotResetIt()
        {
            var motion=new PlayerVerticalMotion();motion.Step(true,true,0);Assert.That(motion.Velocity,Is.EqualTo(Mathf.Sqrt(2*18*.85f)).Within(.0001f));var before=motion.Velocity;motion.Step(false,true,.1f);Assert.That(motion.Velocity,Is.EqualTo(before-1.8f).Within(.0001f));var falling=new PlayerVerticalMotion();falling.Step(false,true,.1f);Assert.That(falling.Velocity,Is.LessThan(0));
        }
        [Test]public void P02_A02_GravityCapsFallAndCeilingGroundContactsClearVelocity()
        {
            var motion=new PlayerVerticalMotion();for(var i=0;i<100;i++)motion.Step(false,false,.1f);Assert.That(motion.Velocity,Is.EqualTo(-30));motion.ResolveCollision(CollisionFlags.Below);Assert.That(motion.Velocity,Is.EqualTo(-2));motion.Step(true,true,0);motion.ResolveCollision(CollisionFlags.Above);Assert.That(motion.Velocity,Is.Zero);motion.Reset();Assert.That(motion.Step(true,false,0),Is.Zero);Assert.That(motion.Velocity,Is.EqualTo(-2));
        }
        [Test]public void P02_A03_InvalidTimeDoesNotMutateState()
        {
            var motion=new PlayerVerticalMotion();Assert.Throws<ArgumentException>(()=>motion.Step(true,true,-1));Assert.Throws<ArgumentException>(()=>motion.Step(true,true,float.NaN));Assert.Throws<ArgumentException>(()=>motion.Step(true,true,float.PositiveInfinity));Assert.That(motion.Velocity,Is.Zero);
        }
    }
}
