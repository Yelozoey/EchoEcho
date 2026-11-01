using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EchoStation.ParkourArt.Editor
{
    public static class VectorCityReflections
    {
        [Serializable]class ProbeRecord{public string name,path;public bool bakeSucceeded;public long bytes;public int width;}
        [Serializable]class Records{public string utc;public ProbeRecord[] probes;}
        [MenuItem("Tools/EchoStation/Parkour Art/04 Bake Regional Reflections")]
        public static void ConfigureAndBake()
        {
            SceneSafetyGuard.RequireClean();var scene=SceneManager.GetActiveScene();if(scene.path!=VectorCityBuilder.ScenePath)throw new InvalidOperationException("Open the owned parkour scene.");
            var root=scene.GetRootGameObjects().Single(g=>g.hideFlags==HideFlags.None).transform;
            var owned=root.Cast<Transform>().Where(t=>t.name=="09_Reflections_BakedRegions"||t.name=="09 — Reflections / Baked regions").ToArray();
            var group=owned.FirstOrDefault();foreach(var duplicate in owned.Skip(1))UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
            if(!group){var go=new GameObject("09_Reflections_BakedRegions");go.transform.SetParent(root,false);group=go.transform;}else group.name="09_Reflections_BakedRegions";
            var centers=new[]{new Vector3(-22,15,-20),new Vector3(-22,17,0),new Vector3(1,19,5),new Vector3(8,22,28),new Vector3(-12,24,39)};
            var sizes=new[]{new Vector3(30,28,30),new Vector3(26,24,22),new Vector3(30,28,32),new Vector3(28,26,24),new Vector3(24,24,28)};
            var records=new Records{utc=DateTime.UtcNow.ToString("O"),probes=new ProbeRecord[5]};
            foreach(var mesh in root.GetComponentsInChildren<MeshRenderer>())GameObjectUtility.SetStaticEditorFlags(mesh.gameObject,GameObjectUtility.GetStaticEditorFlags(mesh.gameObject)|StaticEditorFlags.ReflectionProbeStatic);
            // Planar callbacks must not render while baking cube faces.
            var planars=root.GetComponentsInChildren<VectorCityPlanarReflection>();var prior=planars.Select(p=>p.enabled).ToArray();foreach(var p in planars)p.enabled=false;
            try{
                for(int i=0;i<centers.Length;i++)
                {
                    string name="Probe_Zone_"+(i+1);var t=group.Find(name);var g=t?t.gameObject:new GameObject(name,typeof(ReflectionProbe));g.transform.SetParent(group,false);g.transform.position=centers[i];
                    var probe=g.GetComponent<ReflectionProbe>();probe.mode=ReflectionProbeMode.Baked;probe.resolution=512;probe.hdr=true;probe.boxProjection=true;probe.size=sizes[i];probe.blendDistance=3;probe.intensity=1;probe.clearFlags=ReflectionProbeClearFlags.Skybox;probe.nearClipPlane=.15f;probe.farClipPlane=190;probe.cullingMask=~0;
                    var path=VectorCityBuilder.Root+"/Reflections/VC_Zone_"+(i+1)+".exr";
                    var success=Lightmapping.BakeReflectionProbe(probe,path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                    var texture=AssetDatabase.LoadAssetAtPath<Texture>(path);if(!success||!texture)throw new InvalidOperationException("Probe bake failed: "+name);
                    // Standalone BakeReflectionProbe doesn't persist native bakedTexture without LightingDataAsset.
                    // Store the actual offline bake as a serialized custom cubemap reference instead.
                    probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=texture;
                    records.probes[i]=new ProbeRecord{name=name,path=path,bakeSucceeded=success,bytes=new FileInfo(path).Length,width=texture.width};
                }
            }finally{for(int i=0;i<planars.Length;i++)if(planars[i])planars[i].enabled=prior[i];}
            foreach(var name in new[]{"Planar_Atrium_Floor","Planar_Glass_Wall"})
            {
                var go=GameObject.Find(name);var planar=go.GetComponent<VectorCityPlanarReflection>();if(!planar)planar=go.AddComponent<VectorCityPlanarReflection>();
                if(name.Contains("Wall")){planar.localPlaneNormal=Vector3.left;planar.localPlanePoint=new Vector3(-.5f,0,0);}
                planar.textureWidth=2048;
            }
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Docs/Tests/ParkourArt/Evidence/PK04");File.WriteAllText("Docs/Tests/ParkourArt/Evidence/PK04/ActualProbeBakes.json",JsonUtility.ToJson(records,true));
        }
    }
}
