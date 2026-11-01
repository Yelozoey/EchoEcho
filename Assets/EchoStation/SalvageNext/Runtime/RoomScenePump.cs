using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
namespace EchoStation.SalvageNext
{
    /// <summary>Small private survivor for late cleanup after the gameplay scene disappears.</summary>
    public sealed class RoomScenePump:MonoBehaviour
    {
        static RoomScenePump current;static int mainThread;static bool requested;static AsyncOperation sweep;static float requestedAt,nextSweep;bool quitting;
        readonly List<RoomSceneOwner> owners=new List<RoomSceneOwner>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){current=null;mainThread=Thread.CurrentThread.ManagedThreadId;requested=false;sweep=null;requestedAt=nextSweep=0;CompletedSweeps=0;}
        public static int CompletedSweeps{get;private set;}
        static RoomScenePump Ensure(){if(!current){var go=new GameObject("ES_Next_RoomScenePump"){hideFlags=HideFlags.HideInHierarchy};DontDestroyOnLoad(go);current=go.AddComponent<RoomScenePump>();}return current;}
        internal static void RequestSweep(){RequireMainThread();if(!requested){requested=true;requestedAt=Time.unscaledTime;}Ensure();}
        internal static void RequireMainThread(){if(Thread.CurrentThread.ManagedThreadId!=mainThread)throw new InvalidOperationException("Scene ownership is main-thread only.");}
        internal static void Register(RoomSceneOwner owner)
        {
            RequireMainThread();Ensure().owners.Add(owner);
        }
        void Update()
        {
            bool transitions=false;for(int i=owners.Count-1;i>=0;i--){owners[i].Poll();transitions|=owners[i].HasTransitions;if(owners[i].IsClosed)owners.RemoveAt(i);}
            if(sweep!=null&&sweep.isDone){sweep=null;CompletedSweeps++;}
            if(requested&&sweep==null&&Time.unscaledTime>=nextSweep&&(!transitions||Time.unscaledTime-requestedAt>=2)){requested=false;sweep=Resources.UnloadUnusedAssets();nextSweep=Time.unscaledTime+1;}
            if(owners.Count==0&&!requested&&sweep==null){if(current==this)current=null;Destroy(gameObject);}
        }
        void OnApplicationQuit(){quitting=true;}
        void OnDestroy()
        {
            if(current==this)current=null;if(quitting||!Application.isPlaying)return;foreach(var owner in owners){owner.Dispose();if(!owner.IsClosed)Register(owner);}owners.Clear();if(requested||sweep!=null)Ensure();
        }
    }
}
