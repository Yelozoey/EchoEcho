using System;
using System.Collections;
using System.IO;
using EchoStation.Rendering;
using EchoStation.Tests.Fixtures;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Echo
{
    public class ParticlePoolRuntimeTests
    {
        private Scene scene,previous;private bool background;private ParticleSystem source;
        [Serializable]private class Result{public bool passed,fifthRejected,naturalReturn,staleRejected,globalRejected;public int peakParticles,stressPlays,instancesBefore,instancesAfter;}
        private void Setup()
        {
            Assert.That(Application.isPlaying,Is.True);background=Application.runInBackground;Application.runInBackground=true;previous=SceneManager.GetActiveScene();scene=SceneManager.CreateScene("ParticlePoolRuntimeFixture");SceneManager.SetActiveScene(scene);
            source=new GameObject("TestOnlyParticleTemplate").AddComponent<ParticleSystem>();source.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=source.main;main.loop=false;main.playOnAwake=false;main.stopAction=ParticleSystemStopAction.None;main.maxParticles=64;main.duration=.1f;main.startLifetime=.2f;main.startSpeed=0;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;var emission=source.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,64)});source.GetComponent<ParticleSystemRenderer>().enabled=false;source.gameObject.SetActive(false);Directory.CreateDirectory("Docs/Tests/Stage1/Evidence/S1-E13");
        }
        private ParticleEffectPool Pool(string name){var pool=new GameObject(name).AddComponent<ParticleEffectPool>();pool.Initialize(source,4);return pool;}
        [UnityTest]public IEnumerator E13_B01_ActualBurstHonorsGlobalFourAnd256ThenNaturallyReturns()
        {
            yield return new EnterPlayMode();Setup();var pool=Pool("PoolA");for(var i=0;i<4;i++)Assert.That(pool.TryPlay(Vector3.zero,Quaternion.identity,out _),Is.True);var result=new Result{fifthRejected=!pool.TryPlay(Vector3.zero,Quaternion.identity,out _)};Assert.That(ParticleBudget.ActiveSystems,Is.EqualTo(4));var timer=System.Diagnostics.Stopwatch.StartNew();
            while(pool.ActiveCount>0){if(timer.Elapsed.TotalSeconds>4)Assert.Fail("Nonlooping particle instances did not return.");yield return R04ShadowProbe.Frame();result.peakParticles=Math.Max(result.peakParticles,pool.LiveParticleCount);Assert.That(pool.LiveParticleCount,Is.LessThanOrEqualTo(256));}
            result.naturalReturn=ParticleBudget.ActiveSystems==0;result.passed=result.fifthRejected&&result.naturalReturn&&result.peakParticles==256;File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-E13/ActualParticleBudget.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));Assert.That(pool.TryPlay(Vector3.zero,Quaternion.identity,out var reused),Is.True);Assert.That(reused.Stop(),Is.True);
        }
        [UnityTest]public IEnumerator E13_B02_GlobalBudgetStaleLeasesDisableAndThousandCyclesDoNotGrowObjects()
        {
            yield return new EnterPlayMode();Setup();var a=Pool("PoolA");var b=Pool("PoolB");var leases=new ParticleLease[4];a.TryPlay(Vector3.zero,Quaternion.identity,out leases[0]);a.TryPlay(Vector3.zero,Quaternion.identity,out leases[1]);b.TryPlay(Vector3.zero,Quaternion.identity,out leases[2]);b.TryPlay(Vector3.zero,Quaternion.identity,out leases[3]);var result=new Result{globalRejected=!b.TryPlay(Vector3.zero,Quaternion.identity,out _)};var old=leases[0];Assert.That(old.Stop(),Is.True);Assert.That(a.TryPlay(Vector3.zero,Quaternion.identity,out leases[0]),Is.True);result.staleRejected=!old.Stop()&&ParticleBudget.ActiveSystems==4;
            a.enabled=false;b.enabled=false;Assert.That(ParticleBudget.ActiveSystems,Is.Zero);a.enabled=true;b.enabled=true;result.instancesBefore=a.GetComponentsInChildren<ParticleSystem>(true).Length+b.GetComponentsInChildren<ParticleSystem>(true).Length;
            for(var cycle=0;cycle<1000;cycle++){for(var i=0;i<4;i++){Assert.That(a.TryPlay(Vector3.zero,Quaternion.identity,out leases[i]),Is.True);result.stressPlays++;}Assert.That(b.TryPlay(Vector3.zero,Quaternion.identity,out _),Is.False);foreach(var lease in leases)Assert.That(lease.Stop(),Is.True);}
            Assert.That(a.TryPlay(new Vector3(float.NaN,0,0),Quaternion.identity,out _),Is.False);result.instancesAfter=a.GetComponentsInChildren<ParticleSystem>(true).Length+b.GetComponentsInChildren<ParticleSystem>(true).Length;result.passed=result.globalRejected&&result.staleRejected&&result.instancesBefore==8&&result.instancesAfter==8&&result.stressPlays==4000&&ParticleBudget.ActiveSystems==0;File.WriteAllText("Docs/Tests/Stage1/Evidence/S1-E13/PoolStress.json",JsonUtility.ToJson(result,true));Assert.That(result.passed,Is.True,JsonUtility.ToJson(result,true));yield return R04ShadowProbe.Frame();
        }
        [UnityTest]public IEnumerator E13_B03_DestroyingComponentReleasesBudgetAndOwnedInstances()
        {
            yield return new EnterPlayMode();Setup();var pool=Pool("DisposablePool");var root=pool.gameObject;Assert.That(pool.TryPlay(Vector3.zero,Quaternion.identity,out var old),Is.True);Object.Destroy(pool);yield return R04ShadowProbe.Frame();yield return R04ShadowProbe.Frame();Assert.That(ParticleBudget.ActiveSystems,Is.Zero);Assert.That(root.GetComponentsInChildren<ParticleSystem>(true),Is.Empty);Assert.That(old.Stop(),Is.False);
        }
        [UnityTearDown]public IEnumerator After()
        {
            if(Application.isPlaying){if(previous.IsValid())SceneManager.SetActiveScene(previous);var operation=scene.IsValid()?SceneManager.UnloadSceneAsync(scene):null;var timer=System.Diagnostics.Stopwatch.StartNew();while(operation!=null&&!operation.isDone){if(timer.Elapsed.TotalSeconds>5)throw new TimeoutException("Particle fixture unload timeout.");yield return null;}Assert.That(ParticleBudget.ActiveSystems,Is.Zero);Application.runInBackground=background;yield return new ExitPlayMode();}
        }
    }
}
