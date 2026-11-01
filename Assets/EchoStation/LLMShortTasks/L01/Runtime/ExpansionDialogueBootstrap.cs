using EchoStation.SalvageExpansion;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace EchoStation.L01
{
    public static class ExpansionDialogueBootstrap
    {
        public const string ScenePath="Assets/EchoStation/SalvageExpansion/Scenes/ES_EnvironmentalSalvage.unity";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Attach()
        {SceneManager.sceneLoaded-=SceneLoaded;SceneManager.sceneLoaded+=SceneLoaded;SceneLoaded(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
        static void SceneLoaded(Scene scene,LoadSceneMode mode)
        {if(scene.path!=ScenePath)return;foreach(var root in scene.GetRootGameObjects())foreach(var world in root.GetComponentsInChildren<ExpansionWorld>(true))if(!world.GetComponent<ExpansionDialoguePanel>())world.gameObject.AddComponent<ExpansionDialoguePanel>();}
    }
}
