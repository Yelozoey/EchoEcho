using System;
using System.IO;
using EchoStation.Echo;
using EchoStation.Salvage;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoStation.SalvageNext
{
    [DefaultExecutionOrder(-80),RequireComponent(typeof(SalvagePersistence))]
    public sealed class NextSession:MonoBehaviour
    {
        SalvagePersistence world;SalvageMission mission;NextSaveStore store;
        public long Epoch{get;private set;}=1;
        public RecoveryPreference Preference{get;private set;}=RecoveryPreference.Nearby;
        public RecoveryQuality Quality{get;private set;}=RecoveryQuality.Medium;
        public NextSaveStore Store{get{Cache();return store;}}
        public event Action Restored;
        public event Action SettingsChanged;
        public event Action<RecoveryQuality> QualityChanged;
        void Cache(){if(world)return;world=GetComponent<SalvagePersistence>();world.enabled=false;mission=GetComponent<SalvageMission>();store=new NextSaveStore(Path.Combine(Application.persistentDataPath,"SalvageNext"));}
        void Awake(){Cache();GameRuntimeOptions.UseLLM=false;}
        void OnEnable(){Cache();mission.Restarting+=NewRun;}
        void OnDisable(){if(mission)mission.Restarting-=NewRun;}
        void NewRun(){Epoch=checked(Epoch+1);Restored?.Invoke();}
        public void ConfigureStoreForTests(string directory){Cache();store=new NextSaveStore(directory);}
        public void SetPreference(RecoveryPreference value){if(!Enum.IsDefined(typeof(RecoveryPreference),value))throw new ArgumentException("Invalid preference.");if(Preference==value)return;Preference=value;SettingsChanged?.Invoke();}
        public void SetQuality(RecoveryQuality value){if(!Enum.IsDefined(typeof(RecoveryQuality),value))throw new ArgumentException("Invalid quality.");if(Quality==value)return;Quality=value;QualityChanged?.Invoke(value);SettingsChanged?.Invoke();}
        public NextSnapshot Capture(){Cache();return new NextSnapshot(world.Capture(),Preference,Quality);}
        public bool Save()
        {Cache();try{bool ok=Store.TryWrite(Capture(),out var error);mission.Notice=ok?"已保存到新版独立槽。":error;return ok;}catch(Exception e)when(e is ArgumentException||e is InvalidOperationException){mission.Notice=e.Message;return false;}}
        public bool TryRestore(NextSnapshot snapshot,out string error)
        {
            Cache();error="";if(snapshot==null||!Enum.IsDefined(typeof(RecoveryPreference),snapshot.Preference)||!Enum.IsDefined(typeof(RecoveryQuality),snapshot.Quality)){error="Invalid new-prototype settings.";return false;}
            // The core validates geometry and rolls back world/history before returning failure.
            if(!world.TryRestore(snapshot.World,out error))return false;
            Preference=snapshot.Preference;Quality=snapshot.Quality;Epoch=checked(Epoch+1);GameRuntimeOptions.UseLLM=false;QualityChanged?.Invoke(Quality);Restored?.Invoke();SettingsChanged?.Invoke();return true;
        }
        public bool Load()
        {Cache();if(!Store.TryRead(out var snapshot,out var message)){mission.Notice=message;return false;}if(!TryRestore(snapshot,out var error)){mission.Notice=error;return false;}mission.Notice=message==""?"已恢复新版回收存档。":message;return true;}
        void Update(){var display=GetComponent<NextDisplayPanel>();if(display&&display.IsOpen)return;var keyboard=Keyboard.current;if(keyboard==null)return;if(keyboard.f5Key.wasPressedThisFrame)Save();else if(keyboard.f9Key.wasPressedThisFrame)Load();}
    }
}
