using System;
using System.Collections;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextRoomSweepTests
    {
        GameObject context;RoomPresenter presenter;RoomSceneOwner owner;RoomDemandPlanner policy;bool background;
        static IEnumerator Wait(Func<bool> ready,string stage){var timer=System.Diagnostics.Stopwatch.StartNew();while(!ready()){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(12),stage);yield return null;}}
        [UnityTest]public IEnumerator ProductPumpReleasesUniqueNativeAssetsWithoutTestSideUnusedAssetCalls()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;int bundles=AssetBundle.GetAllLoadedAssetBundles().Count(),sweeps=RoomScenePump.CompletedSweeps;context=new GameObject("NativeSweepContext");var view=context.AddComponent<Camera>();view.enabled=false;presenter=new RoomPresenter(view);policy=new RoomDemandPlanner();policy.Refresh(1,1,new StreamPoint(0,1,12),new StreamPoint(36,1,12),new StreamTaskLease(1,1,new StreamPoint(18,1,12)),new bool[3]);owner=RoomSceneOwner.Create(context,policy,presenter.Show,presenter.Hide);yield return Wait(()=>owner.IsReady(0)&&owner.IsReady(1)&&owner.IsReady(2),"Three rooms ready");
            var first=presenter.Room(0);var map=first.ColorMap(0);var mesh=first.Renderer(0).GetComponent<MeshFilter>().sharedMesh;var shared=presenter.Room(1).Renderer(0).sharedMaterial;first=null;
            policy.Refresh(1,1,new StreamPoint(36,1,12),new StreamPoint(36,1,12),null,new[]{true,true,true});yield return Wait(()=>owner.Phase(0)==RoomScenePhase.Empty&&RoomScenePump.CompletedSweeps>sweeps,"Product sweep completed");Assert.That(map==null,Is.True,"Unique room lightmap released by product");Assert.That(mesh==null,Is.True,"Unique room mesh released by product");Assert.That(shared!=null,Is.True,"Shared material retained for rooms one and two");owner.Dispose();yield return Wait(()=>owner.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0,"Product exit sweep/pump destroyed");Assert.That(AssetBundle.GetAllLoadedAssetBundles().Count(),Is.EqualTo(bundles));Assert.That(shared==null,Is.True);Assert.That(owner.Failures,Is.Zero,owner.LastError);
            const string path="Docs/Tests/SalvagePrototype/Evidence/R4-R03b-3c";Directory.CreateDirectory(path);File.WriteAllText(path+"/NativeSweep_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"nativeSweeps",RoomScenePump.CompletedSweeps-sweeps},{"uniqueMapReleased",map==null},{"uniqueMeshReleased",mesh==null},{"sharedReleasedAfterLastRoom",shared==null},{"testCallsUnusedAssets",false},{"liveHandles",owner.LiveHandles},{"bundlesAfter",bundles},{"pumpDestroyed",true}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup(){if(!Application.isPlaying)yield break;if(owner!=null){owner.Dispose();yield return Wait(()=>owner.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0,"Cleanup");}presenter?.Dispose();if(context)Object.Destroy(context);Application.runInBackground=background;yield return new ExitPlayMode();}
    }
}
