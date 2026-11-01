using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextSceneOwnerTests
    {
        const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R03b-3b";
        RoomSceneOwner owner;RoomDemandPlanner policy;GameObject context;bool background,throwReady;int committed,retired,bundles;
        static readonly StreamPoint Here=new StreamPoint(0,1,12),Far=new StreamPoint(100,1,12);
        void Begin(bool needed=true)
        {
            throwReady=false;committed=retired=0;background=Application.runInBackground;Application.runInBackground=true;bundles=AssetBundle.GetAllLoadedAssetBundles().Count();context=new GameObject("NextOwnerTestContext");policy=new RoomDemandPlanner();policy.Refresh(1,1,needed?Here:Far,needed?Here:Far,null,new bool[3]);owner=RoomSceneOwner.Create(context,policy,(room,package)=>{if(throwReady)throw new InvalidOperationException("intentional binding failure");committed++;},(room,package)=>retired++);Directory.CreateDirectory(Evidence);
        }
        static IEnumerator Wait(Func<bool> predicate,string phase,double seconds=10){var timer=System.Diagnostics.Stopwatch.StartNew();while(!predicate()){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(seconds),phase);yield return null;}}
        IEnumerator Closed(string phase)
        {
            yield return Wait(()=>owner.IsClosed,phase);yield return Wait(()=>AssetBundle.GetAllLoadedAssetBundles().Count()==bundles,"Bundle cleanup");yield return Wait(()=>Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0,"Pump waits for native unused-resource sweep");Assert.That(owner.LiveHandles,Is.Zero);File.WriteAllText(Evidence+"/"+phase+"_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"commits",owner.Commits},{"callbacks",committed},{"retireCallbacks",retired},{"lateReleases",owner.LateReleases},{"failures",owner.Failures},{"lastError",owner.LastError},{"liveHandles",owner.LiveHandles},{"bundlesAfter",AssetBundle.GetAllLoadedAssetBundles().Count()},{"bundlesBefore",bundles},{"pumpDestroyed",true},{"nativeSweeps",RoomScenePump.CompletedSweeps}}.ToString());
        }
        [UnityTest]public IEnumerator ResidentRetiresOnDemandWithdrawalAndDisposesWithoutLeaks()
        {yield return new EnterPlayMode();Begin();yield return Wait(()=>owner.IsReady(0),"Ready");Assert.That(committed,Is.EqualTo(1));policy.Refresh(1,1,Far,Far,null,new[]{true,false,false});yield return Wait(()=>owner.Phase(0)==RoomScenePhase.Empty,"Withdraw");Assert.That(retired,Is.EqualTo(1));owner.Dispose();yield return Closed("Withdraw");Assert.That(owner.Failures,Is.Zero);}
        [UnityTest]public IEnumerator ImmediateDisposeWhileLoadingOnlyReleasesLateResultWithoutCommit()
        {yield return new EnterPlayMode();Begin();owner.Poll();Assert.That(owner.Phase(0),Is.EqualTo(RoomScenePhase.Loading));owner.Dispose();Assert.That(owner.IsClosed,Is.False);yield return Closed("LateDispose");Assert.That(owner.LateReleases,Is.EqualTo(1));Assert.That(committed,Is.Zero);Assert.That(retired,Is.Zero);Assert.That(owner.Failures,Is.Zero);}
        [UnityTest]public IEnumerator NewWorldEpochRejectsOldLoadThenCommitsOnlyTheCurrentRequest()
        {yield return new EnterPlayMode();Begin();owner.Poll();policy.Refresh(2,1,Here,Here,null,new[]{true,false,false});yield return Wait(()=>owner.IsReady(0),"Current world ready");Assert.That(owner.LateReleases,Is.EqualTo(1));Assert.That(committed,Is.EqualTo(1));owner.Dispose();yield return Closed("WorldEpoch");Assert.That(retired,Is.EqualTo(1));Assert.That(owner.Failures,Is.Zero);}
        [UnityTest]public IEnumerator DestroyedContextStillHasPersistentPumpForLateCleanup()
        {yield return new EnterPlayMode();Begin();owner.Poll();Object.DestroyImmediate(context);yield return Closed("ContextGone");Assert.That(owner.IsDisposed,Is.True);Assert.That(owner.LateReleases,Is.EqualTo(1));Assert.That(committed,Is.Zero);Assert.That(owner.Failures,Is.Zero);}
        [UnityTest]public IEnumerator BackgroundDisposeIsRejectedBeforeUnityAccessAndMainThreadCanClose()
        {yield return new EnterPlayMode();Begin(false);var task=Task.Run(()=>{try{owner.Dispose();return false;}catch(InvalidOperationException){return true;}});while(!task.IsCompleted)yield return null;Assert.That(task.Result,Is.True);Assert.That(owner.IsDisposed,Is.False);owner.Dispose();yield return Closed("ThreadGuard");}
        [UnityTest]public IEnumerator BindingExceptionRetiresPartialCommitAndKeepsHandleOwnership()
        {yield return new EnterPlayMode();Begin();throwReady=true;yield return Wait(()=>owner.Failures>0,"Binding failure observed");policy.Refresh(1,1,Far,Far,null,new[]{true,false,false});owner.Dispose();yield return Closed("BindingRollback");Assert.That(owner.Commits,Is.Zero);Assert.That(retired,Is.EqualTo(1));Assert.That(owner.LastError,Does.Contain("intentional binding failure"));}
        [UnityTest]public IEnumerator WorldChangeRevokesReadyImmediatelyBeforePumpPoll()
        {yield return new EnterPlayMode();Begin();yield return Wait(()=>owner.IsReady(0),"Initial ready");policy.Refresh(2,1,Here,Here,null,new[]{true,false,false});Assert.That(owner.IsReady(0),Is.False,"Old world must not appear ready in the refresh frame");Assert.That(owner.Phase(0),Is.EqualTo(RoomScenePhase.Loaded));yield return Wait(()=>owner.IsReady(0),"New world ready");Assert.That(owner.Commits,Is.EqualTo(2));owner.Dispose();yield return Closed("ImmediateReadyRevocation");}
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(!Application.isPlaying)yield break;try{if(owner!=null){owner.Dispose();yield return Wait(()=>owner.IsClosed,"Teardown owner");}if(context)Object.Destroy(context);yield return null;yield return null;}finally{Application.runInBackground=background;}yield return new ExitPlayMode();
        }
    }
}
