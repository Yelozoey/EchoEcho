using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Tests
{
    public sealed class NextCorruptRoomTests
    {
        const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-R03c-2",Prefix="es_next_rooms_scenes_es-next-room-0_";
        readonly List<string> expected=new List<string>(),unexpected=new List<string>();
        Func<IResourceLocation,string> previousTransform;Action<AsyncOperationHandle,Exception> previousHandler;bool hooks,inject,background;string badPath,bundle,beforeHash;GameObject context;RoomSceneOwner owner;RoomPresenter presenter;
        AsyncOperationHandle<IList<IResourceLocation>> locations;bool ownLocations;
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static IEnumerator Wait(Func<bool> condition,string stage){var timer=System.Diagnostics.Stopwatch.StartNew();while(!condition()){Assert.That(timer.Elapsed.TotalSeconds,Is.LessThan(12),stage);yield return null;}}
        string Transform(IResourceLocation location){string path=previousTransform!=null?previousTransform(location):location.InternalId;return inject&&path.IndexOf(Prefix,StringComparison.OrdinalIgnoreCase)>=0?badPath:path;}
        void Fault(AsyncOperationHandle operation,Exception error)
        {
            string message=error.ToString();if(message.IndexOf("corrupt_room.bundle",StringComparison.Ordinal)>=0){expected.Add(message);return;}unexpected.Add(message);if(previousHandler!=null)previousHandler(operation,error);else Debug.LogException(error);
        }
        void RestoreHooks(){if(!hooks)return;Addressables.InternalIdTransformFunc=previousTransform;ResourceManager.ExceptionHandler=previousHandler;hooks=false;}
        [UnityTest,Timeout(120000)]public IEnumerator ActualInvalidLocalBundleFailsWithoutCommitThenRetriesOriginalAndReleasesAllHandles()
        {
            yield return new EnterPlayMode();background=Application.runInBackground;Application.runInBackground=true;Directory.CreateDirectory(Evidence);int baseline=AssetBundle.GetAllLoadedAssetBundles().Count();bundle=Directory.GetFiles(Addressables.BuildPath,Prefix+"*.bundle",SearchOption.AllDirectories).Single();beforeHash=Hash(bundle);badPath=Path.GetFullPath("Library/EchoStation/SalvageNext/BadBundle_"+Guid.NewGuid().ToString("N")+"/corrupt_room.bundle");Directory.CreateDirectory(Path.GetDirectoryName(badPath));File.WriteAllBytes(badPath,new byte[4096]);
            locations=Addressables.LoadResourceLocationsAsync("es-next-room-0",typeof(SceneInstance));ownLocations=true;yield return Wait(()=>locations.IsDone,"Official scene location initialized");Assert.That(locations.Status,Is.EqualTo(AsyncOperationStatus.Succeeded));Assert.That(locations.Result.Count,Is.EqualTo(1));Addressables.Release(locations);ownLocations=false;
            previousTransform=Addressables.InternalIdTransformFunc;previousHandler=ResourceManager.ExceptionHandler;hooks=true;inject=true;Addressables.InternalIdTransformFunc=Transform;ResourceManager.ExceptionHandler=Fault;
            LogAssert.Expect(LogType.Error,new Regex(@"(Unable to read header from archive file|Failed to read data for the AssetBundle|Unable to open archive file).*corrupt_room\.bundle",RegexOptions.Singleline));
            context=new GameObject("CorruptRoomTestContext");var camera=context.AddComponent<Camera>();camera.enabled=false;presenter=new RoomPresenter(camera);var policy=new RoomDemandPlanner();policy.Refresh(1,1,new StreamPoint(0,1,12),new StreamPoint(0,1,12),null,new bool[3]);owner=RoomSceneOwner.Create(context,policy,presenter.Show,presenter.Hide);yield return Wait(()=>owner.Failures>0,"Real native bad bundle failure");Assert.That(owner.Commits,Is.Zero);Assert.That(presenter.Room(0),Is.Null);Assert.That(owner.LiveHandles,Is.Zero,"Failed load ownership released before retry");Assert.That(owner.LastError,Is.Not.Null.And.Not.Empty);Assert.That(expected.Count,Is.GreaterThan(0));Assert.That(unexpected,Is.Empty);inject=false;
            yield return Wait(()=>owner.IsReady(0),"Retry real unmodified scene bundle");Assert.That(owner.Commits,Is.EqualTo(1));Assert.That(owner.Failures,Is.EqualTo(1));Assert.That(presenter.Room(0).gameObject.activeSelf,Is.True);owner.Dispose();yield return Wait(()=>owner.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0,"Final owned native cleanup");Assert.That(AssetBundle.GetAllLoadedAssetBundles().Count(),Is.EqualTo(baseline));Assert.That(Hash(bundle),Is.EqualTo(beforeHash));Assert.That(unexpected,Is.Empty);RestoreHooks();Assert.That(Addressables.InternalIdTransformFunc,Is.SameAs(previousTransform));Assert.That(ResourceManager.ExceptionHandler,Is.SameAs(previousHandler));
            File.WriteAllText(Evidence+"/BadBundle_"+Guid.NewGuid().ToString("N")+".json",new JObject{{"badFile",badPath},{"badBytes",4096},{"originalBundle",bundle},{"originalShaBefore",beforeHash},{"originalShaAfter",Hash(bundle)},{"commits",owner.Commits},{"failures",owner.Failures},{"expectedProviderExceptions",new JArray(expected)},{"unexpectedProviderExceptions",new JArray(unexpected)},{"liveHandles",owner.LiveHandles},{"bundlesAfter",baseline},{"hooksExactRestored",true},{"blanketLogIgnore",false}}.ToString());
        }
        [UnityTearDown]public IEnumerator Cleanup()
        {
            if(!Application.isPlaying){RestoreHooks();yield break;}bool closed=true;
            try{inject=false;if(ownLocations&&locations.IsValid()){while(!locations.IsDone)yield return null;Addressables.Release(locations);ownLocations=false;}if(owner!=null){owner.Dispose();var timer=System.Diagnostics.Stopwatch.StartNew();while((!owner.IsClosed||Resources.FindObjectsOfTypeAll<RoomScenePump>().Length>0)&&timer.Elapsed.TotalSeconds<15)yield return null;closed=owner.IsClosed&&Resources.FindObjectsOfTypeAll<RoomScenePump>().Length==0;}presenter?.Dispose();if(context)Object.Destroy(context);}
            finally{RestoreHooks();Application.runInBackground=background;}yield return new ExitPlayMode();Assert.That(closed,Is.True,"Fault fixture cleanup must finish");
        }
    }
}
