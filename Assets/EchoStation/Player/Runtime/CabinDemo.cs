using System;
using System.IO;
using EchoStation.Echo;
using EchoStation.Rendering;
using UnityEngine;

namespace EchoStation.Player
{
    [RequireComponent(typeof(SaveGameAdapter),typeof(CabinControls)),DisallowMultipleComponent]
    public sealed class CabinDemo:MonoBehaviour
    {
        [SerializeField]private EchoBrain echo;
        private LocalSaveStore store;private int oldFrameRate,oldVsync;private bool pacingCaptured;
        public bool Ready{get;private set;}
        public EchoBrain Echo=>echo;
        public SaveGameAdapter Saves=>GetComponent<SaveGameAdapter>();
        public string Status{get;private set;}="正在初始化。";
        public event Action<string> StatusChanged;
        private void Start()
        {
            GameRuntimeOptions.UseLLM=false;oldFrameRate=Application.targetFrameRate;oldVsync=QualitySettings.vSyncCount;pacingCaptured=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=60;if(!echo||!echo.GetComponent<EchoNavigation>().TryBind(echo.transform.position)||!echo.Initialize(transform,IntentKind.Follow))throw new InvalidOperationException("Cabin Echo initialization failed: "+(echo?echo.GetComponent<EchoNavigation>().Failure.ToString():"missing reference"));Saves.Configure(echo);GetComponentInChildren<Camera>().GetComponent<ScanWaveView>().Source=echo.GetComponent<ScanWaveEmitter>();store=new LocalSaveStore(Path.Combine(Application.persistentDataPath,"Saves"));Ready=true;SetStatus("观测站已连接。请检查小屋内的记录。Tab 可查看 Echo 指令。");
        }
        private void OnDestroy(){if(pacingCaptured&&Application.isEditor){Application.targetFrameRate=oldFrameRate;QualitySettings.vSyncCount=oldVsync;}}
        public void UseDiagnosticStore(LocalSaveStore value){if(Array.IndexOf(Environment.GetCommandLineArgs(),"--s1-probe")<0)throw new InvalidOperationException("Diagnostic store requires explicit probe CLI.");store=value??throw new ArgumentNullException(nameof(value));}
        public bool Save(){if(!Ready)return false;var ok=Saves.TrySave(store,out var error);SetStatus(ok?"已保存小屋探索进度。":"无法保存："+error);return ok;}
        public bool Load(){if(!Ready)return false;var ok=Saves.TryLoad(store,out var message);SetStatus(message);return ok;}
        public void SetStatus(string message){Status=message??"";StatusChanged?.Invoke(Status);}
    }
}
