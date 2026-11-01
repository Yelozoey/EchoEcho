using System;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.SalvageNext
{
    [DisallowMultipleComponent]public sealed class NextProbeScope:MonoBehaviour
    {
        [SerializeField]LightProbes probes;LightProbes prior;bool bound;
        public LightProbes Probes=>probes;
        public void Configure(LightProbes asset){if(!asset||asset.count<180)throw new ArgumentException("Persistent baked probes required.");probes=asset;}
        void Rebind(){if(bound&&probes)LightmapSettings.lightProbes=probes;}
        void Loaded(Scene scene,LoadSceneMode mode)=>Rebind();
        void Unloaded(Scene scene)=>Rebind();
        void OnEnable(){if(Application.isPlaying&&probes){prior=LightmapSettings.lightProbes;bound=true;SceneManager.sceneLoaded+=Loaded;SceneManager.sceneUnloaded+=Unloaded;Rebind();}}
        void OnDisable(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneUnloaded-=Unloaded;if(bound){if(LightmapSettings.lightProbes==probes)LightmapSettings.lightProbes=prior;bound=false;}}
    }
}
