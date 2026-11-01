using System;
using EchoStation.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Echo
{
    public class ParticlePoolContractTests
    {
        private GameObject root;private ParticleSystem system;
        [SetUp]public void Setup(){root=new GameObject("ParticleContractFixture");system=root.AddComponent<ParticleSystem>();var main=system.main;main.loop=false;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;main.maxParticles=64;system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        [TearDown]public void After(){Object.DestroyImmediate(root);Assert.That(ParticleBudget.ActiveSystems,Is.Zero);}
        [Test]public void E13_A01_ValidSingleSystemAndBudgetLimits(){Assert.DoesNotThrow(()=>ParticleEffectPool.Validate(system,4));Assert.Throws<ArgumentException>(()=>ParticleEffectPool.Validate(system,0));Assert.Throws<ArgumentException>(()=>ParticleEffectPool.Validate(system,5));Assert.Throws<ArgumentException>(()=>ParticleEffectPool.Validate(null,1));Assert.That(ParticleBudget.MaximumConcurrentSystems*ParticleBudget.MaximumParticlesPerSystem,Is.EqualTo(256));}
        [Test]public void E13_A02_LoopAutomaticPlayDestroyAndExcessParticlesAreRejected()
        {
            var main=system.main;main.loop=true;Assert.Throws<ArgumentException>(()=>ParticleEffectPool.Validate(system,1));main.loop=false;main.playOnAwake=true;Assert.Throws<ArgumentException>(()=>ParticleEffectPool.Validate(system,1));main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.Destroy;Assert.Throws<ArgumentException>(()=>ParticleEffectPool.Validate(system,1));main.stopAction=ParticleSystemStopAction.None;main.maxParticles=65;Assert.Throws<ArgumentException>(()=>ParticleEffectPool.Validate(system,1));
        }
        [Test]public void E13_A03_ChildrenSubemittersAndGameplayCallbacksCannotBypassBudget()
        {
            var sub=system.subEmitters;sub.enabled=true;Assert.Throws<ArgumentException>(()=>ParticleEffectPool.Validate(system,1));sub.enabled=false;var collision=system.collision;collision.enabled=true;Assert.Throws<ArgumentException>(()=>ParticleEffectPool.Validate(system,1));collision.enabled=false;var trigger=system.trigger;trigger.enabled=true;Assert.Throws<ArgumentException>(()=>ParticleEffectPool.Validate(system,1));trigger.enabled=false;new GameObject("ForbiddenChild",typeof(ParticleSystem)).transform.SetParent(root.transform);Assert.Throws<ArgumentException>(()=>ParticleEffectPool.Validate(system,1));
        }
    }
}
