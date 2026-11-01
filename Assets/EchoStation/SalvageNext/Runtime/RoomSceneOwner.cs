using System;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext
{
    public enum RoomScenePhase{Empty,Loading,Loaded,Unloading}
    /// <summary>Main-thread ownership. Dispose retires demand, not unfinished native operations.</summary>
    public sealed class RoomSceneOwner:IDisposable
    {
        sealed class Slot
        {
            public RoomScenePhase Phase;public RoomLoadTicket Ticket;public RoomPackage Package;
            public AsyncOperationHandle<SceneInstance> Load,Unload;public bool OwnLoad,OwnUnload,Committed;public float RetryAt;
        }
        readonly Slot[] slots={new Slot(),new Slot(),new Slot()};readonly RoomDemandPlanner policy;readonly int thread;readonly string addressPrefix;
        GameObject context;Action<int,RoomPackage> available,retiring;bool disposed;
        public int Commits{get;private set;}public int LateReleases{get;private set;}public int Failures{get;private set;}public string LastError{get;private set;}
        public bool IsDisposed=>disposed;
        public int LiveHandles{get{int count=0;foreach(var slot in slots){if(slot.OwnLoad)count++;if(slot.OwnUnload)count++;}return count;}}
        public bool IsClosed=>disposed&&LiveHandles==0;
        RoomSceneOwner(GameObject owner,RoomDemandPlanner demand,Action<int,RoomPackage> ready,Action<int,RoomPackage> release,string prefix){context=owner;policy=demand;available=ready;retiring=release;addressPrefix=prefix;thread=Thread.CurrentThread.ManagedThreadId;}
        public static RoomSceneOwner Create(GameObject context,RoomDemandPlanner policy,Action<int,RoomPackage> available,Action<int,RoomPackage> retiring,string addressPrefix="es-next-room-")
        {RoomScenePump.RequireMainThread();if(!Application.isPlaying||!context||policy==null||string.IsNullOrWhiteSpace(addressPrefix))throw new ArgumentException("Live context, policy and room address prefix required.");var owner=new RoomSceneOwner(context,policy,available,retiring,addressPrefix);RoomScenePump.Register(owner);return owner;}
        void MainThread(){if(Thread.CurrentThread.ManagedThreadId!=thread)throw new InvalidOperationException("Scene ownership is main-thread only.");}
        public RoomScenePhase Phase(int room){if(room<0||room>2)throw new ArgumentOutOfRangeException();return slots[room].Phase;}
        public bool Retained(int room)=>Phase(room)!=RoomScenePhase.Empty;
        public bool IsReady(int room)=>Phase(room)==RoomScenePhase.Loaded&&slots[room].Committed&&Current(slots[room]);
        internal bool HasTransitions{get{foreach(var slot in slots)if(slot.Phase==RoomScenePhase.Loading||slot.Phase==RoomScenePhase.Unloading)return true;return false;}}
        void Error(string message){Failures++;LastError=message;}
        bool Wanted(int index)=>!disposed&&policy.Wanted(index);
        bool Current(Slot slot)=>!disposed&&policy.IsCurrent(slot.Ticket);
        void BeginLoad(int index,Slot slot)
        {
            try{slot.Ticket=policy.Ticket(index);slot.Load=Addressables.LoadSceneAsync(addressPrefix+index,LoadSceneMode.Additive,true);slot.OwnLoad=true;slot.Phase=RoomScenePhase.Loading;}
            catch(Exception e){Error(e.Message);slot.RetryAt=Time.unscaledTime+1;}
        }
        void BeginUnload(int index,Slot slot)
        {
            if(slot.Committed){slot.Committed=false;try{retiring?.Invoke(index,slot.Package);}catch(Exception e){Error(e.Message);}}
            if(slot.Package)slot.Package.gameObject.SetActive(false);
            slot.Package=null;
            if(!slot.OwnLoad||!slot.Load.IsValid()){slot.OwnLoad=false;slot.Phase=RoomScenePhase.Empty;return;}
            try{slot.Unload=Addressables.UnloadSceneAsync(slot.Load,UnloadSceneOptions.UnloadAllEmbeddedSceneObjects,false);slot.OwnLoad=false;slot.OwnUnload=true;slot.Phase=RoomScenePhase.Unloading;}catch(Exception e){Error(e.Message);slot.Phase=RoomScenePhase.Loaded;slot.RetryAt=Time.unscaledTime+1;}
        }
        internal void Poll()
        {
            MainThread();if(!disposed&&!context)Dispose();
            for(int index=0;index<3;index++)
            {
                var slot=slots[index];switch(slot.Phase)
                {
                    case RoomScenePhase.Empty:if(Wanted(index)&&Time.unscaledTime>=slot.RetryAt)BeginLoad(index,slot);break;
                    case RoomScenePhase.Loading:
                        if(!slot.Load.IsDone)break;
                        if(slot.Load.Status!=AsyncOperationStatus.Succeeded){Error(slot.Load.OperationException?.Message??"Room load failed.");if(slot.Load.IsValid())Addressables.Release(slot.Load);slot.OwnLoad=false;slot.Phase=RoomScenePhase.Empty;slot.RetryAt=Time.unscaledTime+1;break;}
                        if(!Current(slot)){LateReleases++;BeginUnload(index,slot);break;}
                        try{var roots=slot.Load.Result.Scene.GetRootGameObjects();if(roots.Length!=1)throw new InvalidOperationException("Room must contain one visual root.");slot.Package=roots[0].GetComponent<RoomPackage>();if(!slot.Package||slot.Package.Index!=index||slot.Package.gameObject.activeSelf)throw new InvalidOperationException("Room identity/initial state invalid.");slot.Phase=RoomScenePhase.Loaded;slot.Committed=true;available?.Invoke(index,slot.Package);Commits++;}
                        catch(Exception e){Error(e.Message);slot.RetryAt=Time.unscaledTime+1;BeginUnload(index,slot);}break;
                    case RoomScenePhase.Loaded:if(!Current(slot)&&Time.unscaledTime>=slot.RetryAt)BeginUnload(index,slot);break;
                    case RoomScenePhase.Unloading:
                        if(!slot.Unload.IsDone)break;if(slot.Unload.Status!=AsyncOperationStatus.Succeeded)Error(slot.Unload.OperationException?.Message??"Room unload failed.");if(slot.Unload.IsValid())Addressables.Release(slot.Unload);slot.OwnUnload=false;slot.Phase=RoomScenePhase.Empty;RoomScenePump.RequestSweep();break;
                }
            }
        }
        public void Dispose()
        {
            MainThread();if(disposed)return;disposed=true;for(int i=0;i<3;i++)if(slots[i].Phase==RoomScenePhase.Loaded)BeginUnload(i,slots[i]);available=null;retiring=null;context=null;
        }
    }
}
