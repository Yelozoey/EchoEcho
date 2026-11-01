using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using EchoStation.Salvage.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace EchoStation.SalvageNext.Editor
{
    public static class CargoAssets
    {
        public const string FolderPath=FacilityAssets.Root+"/Prefabs/Cargo",Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-A01d";
        const string Owner="EchoStation-SalvageNext-Generated";
        static readonly string[] Names={"MetalToolbox","Prop_Crate3","Prop_Crate4","Prop_Computer","Prop_AccessPoint","Prop_Chest","Prop_Barrel_Large"};
        static readonly Color[] Tints={new Color(.95f,.98f,1),new Color(.7f,.85f,1),new Color(.8f,1,.92f),new Color(.85f,.9f,1),new Color(.72f,.86f,1),new Color(.68f,.75f,.86f),new Color(1,.72f,.45f)};
        public static string PathFor(string id){SalvageCatalog.Get(id);return FolderPath+"/ES_"+id+"_Visual.prefab";}
        static void Owned(Object asset){if(asset&&!AssetDatabase.GetLabels(asset).Contains(Owner))throw new InvalidOperationException("Preserve foreign cargo asset.");}
        static void Save(Object asset){AssetDatabase.SetLabels(asset,new[]{Owner});EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);}
        static Bounds BoundsOf(GameObject go)
        {var renderers=go.GetComponentsInChildren<MeshRenderer>();if(renderers.Length==0)throw new InvalidOperationException("No cargo geometry.");var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);return bounds;}
        [MenuItem("Tools/EchoStation/SalvageNext/R4-A01d/Prepare Cargo Visuals")]
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();FacilityAssets.Folder(FolderPath);Directory.CreateDirectory(Evidence);var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                for(int i=0;i<SalvageCatalog.Items.Count;i++)
                {
                    var id=SalvageCatalog.Items[i].Id;string path=i==0?SalvageAssetImport.BoxModel:FacilityAssets.Prefab(FacilityAssets.Models().Single(p=>System.IO.Path.GetFileNameWithoutExtension(p)==Names[i]));
                    var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!source)throw new InvalidOperationException("Missing cargo model "+path);
                    var root=new GameObject("ES_"+id+"_Visual");var instance=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);instance.transform.SetParent(root.transform,false);
                    var sourceMaterials=i==0?new[]{AssetDatabase.LoadAssetAtPath<Material>(SalvageAssetImport.MaterialPath(1))}:instance.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Distinct().ToArray();
                    var materials=new System.Collections.Generic.Dictionary<Material,Material>();int index=0;
                    foreach(var original in sourceMaterials)
                    {
                        if(!original||original.shader.name!="Universal Render Pipeline/Lit")throw new InvalidOperationException("Unadapted source cargo material.");
                        string destination=FacilityAssets.Generated+"/ES_"+id+"_Material_"+(index++)+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(destination);Owned(material);
                        if(!material){material=Object.Instantiate(original);material.name=System.IO.Path.GetFileNameWithoutExtension(destination);AssetDatabase.CreateAsset(material,destination);}else EditorUtility.CopySerialized(original,material);
                        material.SetColor("_BaseColor",original.GetColor("_BaseColor")*Tints[i]);material.enableInstancing=true;Save(material);materials.Add(original,material);
                    }
                    foreach(var renderer in instance.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterials=i==0?Enumerable.Repeat(materials[sourceMaterials[0]],renderer.sharedMaterials.Length).ToArray():renderer.sharedMaterials.Select(m=>materials[m]).ToArray();
                    var bounds=BoundsOf(instance);float scale=(i==6?.8f:.44f)/Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);instance.transform.localScale*=scale;instance.transform.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;
                    var normalized=BoundsOf(instance);root.AddComponent<CargoVisualModel>().Configure(id,normalized);string output=PathFor(id);Owned(AssetDatabase.LoadMainAssetAtPath(output));var prefab=PrefabUtility.SaveAsPrefabAsset(root,output);if(!prefab)throw new IOException("Cargo visual save failed.");Save(prefab);Object.DestroyImmediate(root);
                }
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
        }
        public static CargoVisualModel Attach(CargoActor actor)
        {
            if(!actor)throw new ArgumentException("Cargo owner required.");var existing=actor.GetComponentInChildren<CargoVisualModel>(true);if(existing){if(existing.Id!=actor.Id)throw new InvalidOperationException("Wrong cargo visual.");return existing;}
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(actor.Id));if(!prefab)throw new InvalidOperationException("Prepare cargo visual first.");
            var labels=actor.GetComponentsInChildren<TextMesh>(true).Select(t=>t.GetComponent<Renderer>()).ToArray();
            foreach(var renderer in actor.GetComponentsInChildren<MeshRenderer>(true))if(!renderer.GetComponent<TextMesh>())renderer.enabled=false;
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(prefab,actor.gameObject.scene);visual.transform.SetParent(actor.transform,false);var model=visual.GetComponent<CargoVisualModel>();
            actor.Body.center=model.LocalBounds.center+Vector3.up*.005f;actor.Body.size=model.LocalBounds.size+new Vector3(.02f,.01f,.02f);
            var obstacle=actor.GetComponent<NavMeshObstacle>();if(obstacle){obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=actor.Body.center;obstacle.size=actor.Body.size;obstacle.carving=false;}
            foreach(var label in labels){var p=label.transform.localPosition;label.transform.localPosition=new Vector3(p.x,Mathf.Max(.5f,model.LocalBounds.size.y+.08f),p.z);}
            model.Bind(actor,labels);return model;
        }
    }
}
