using System;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoStation.Salvage.Editor
{
    public static class SalvageTargetSetup
    {
        [MenuItem("Tools/EchoStation/Salvage/R2-C01/Add Read Only Targets")]
        public static void Add()
        {
            SceneSafetyGuard.RequireClean();var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(SalvageWorldSetup.ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_Salvage_World");if(root.GetComponentInChildren<SalvageTargetView>())throw new InvalidOperationException("Targets already exist; preserve scene.");var carry=root.GetComponentInChildren<SalvageCarry>();
                foreach(var cargo in root.GetComponentsInChildren<CargoActor>())if(SalvageTargetCatalog.TryGet(cargo.Id,out _))Adapt(cargo.gameObject,cargo.Id,carry,new Vector3(0,0,-1.5f));
                var hazardMaterial=SalvageWorldSetup.Material("ES_Warning_Orange",new Color(.65f,.16f,.035f));
                Hazard(root,"danger_power_box",new Vector3(22,.8f,18),new Vector3(.8f,1.6f,.6f),carry,hazardMaterial);
                Hazard(root,"danger_leak_pipe",new Vector3(34,.8f,12),new Vector3(.5f,1.6f,.5f),carry,hazardMaterial);
                EditorSceneManager.SaveScene(scene);
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
        }
        static void Adapt(GameObject go,string id,SalvageCarry carry,Vector3 approachOffset)
        {
            go.SetActive(false);var approach=new GameObject("EchoApproach").transform;approach.SetParent(go.transform,false);approach.localPosition=approachOffset;
            go.AddComponent<SalvageTargetView>().Configure(id,carry,approach);go.SetActive(true);
        }
        static void Hazard(GameObject root,string id,Vector3 position,Vector3 size,SalvageCarry carry,Material mat)
        {
            var d=SalvageTargetCatalog.Items.Single(t=>t.Id==id);var go=SalvageWorldSetup.Block(root,d.Name,position,Vector3.one,mat);
            // Scale the physical child instead of the identity/approach root.
            var visual=new GameObject("HazardRoot");visual.transform.SetParent(root.transform);visual.transform.position=position;
            go.transform.SetParent(visual.transform,true);go.transform.localPosition=Vector3.zero;go.transform.localScale=size;
            var light=new GameObject("ReadOnlyWarningLight").AddComponent<Light>();light.transform.SetParent(visual.transform,false);light.transform.localPosition=new Vector3(0,.7f,-.4f);light.type=LightType.Point;light.range=3;light.intensity=1;light.color=new Color(1,.18f,.025f);light.shadows=LightShadows.None;
            var label=new GameObject("WarningLabel").AddComponent<TextMesh>();label.transform.SetParent(visual.transform,false);label.transform.localPosition=new Vector3(-.4f,1.1f,0);label.text=d.Name;label.font=EchoStation.Player.RuntimeUiFont.Get();label.fontSize=32;label.characterSize=.025f;label.color=new Color(1,.4f,.1f);label.GetComponent<Renderer>().sharedMaterial=label.font.material;
            Adapt(visual,id,carry,new Vector3(-1.6f,-.8f,0));
        }
    }
}
