using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Salvage.Editor
{
    public static class SalvageCargoSetup
    {
        public static readonly Vector3[] Positions={new Vector3(9,0,19),new Vector3(16,0,18),new Vector3(20,0,5),new Vector3(27,0,12),new Vector3(32,0,18),new Vector3(33,0,6),new Vector3(18,0,12)};
        [MenuItem("Tools/EchoStation/Salvage/R1-I02/Add Cargo")]
        public static void Add()
        {
            SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var world=scene.GetRootGameObjects().Single(r=>r.name=="ES_Salvage_World");
                if(world.GetComponentInChildren<CargoActor>())throw new InvalidOperationException("Cargo already exists; preserve it.");
                world.transform.Find("Floor_36x24").gameObject.AddComponent<SalvageDropSurface>();world.transform.Find("RecoveryPoint").gameObject.AddComponent<SalvageDropSurface>();
                var folder=new GameObject("Cargo");folder.transform.SetParent(world.transform);
                for(int i=0;i<SalvageCatalog.Items.Count;i++)
                {
                    var definition=SalvageCatalog.Items[i];var actor=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SalvagePrefabSetup.Path(1)),scene);
                    actor.name=definition.Name;actor.transform.SetParent(folder.transform);actor.transform.position=Positions[i];actor.AddComponent<CargoActor>().Configure(definition.Id);
                    var text=new GameObject("ID_Label").AddComponent<TextMesh>();text.transform.SetParent(actor.transform,false);text.transform.localPosition=new Vector3(-.2f,.5f,0);text.text=definition.Name+"\n"+definition.Kilograms+"kg / "+definition.Points+"分";text.font=EchoStation.Player.RuntimeUiFont.Get();text.fontSize=32;text.characterSize=.025f;text.GetComponent<Renderer>().sharedMaterial=text.font.material;
                }
                world.GetComponentInChildren<SalvageMotor>().gameObject.AddComponent<SalvageCarry>();EditorSceneManager.SaveScene(scene);
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
