using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using EchoStation.Rendering;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextHazardAssets
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/R4-P01a-2a";
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        static Material Copy(string source,string name)
        {
            string path=FacilityAssets.Generated+"/ES_Next_Hazard_"+name+".mat";if(AssetDatabase.LoadMainAssetAtPath(path))throw new InvalidOperationException("Keep existing hazard material.");var original=source.StartsWith("ES_NextLit_",StringComparison.Ordinal)?AssetDatabase.FindAssets("t:Material "+source,new[]{FacilityAssets.Generated}).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).Single():AssetDatabase.LoadAssetAtPath<Material>(FacilityAssets.Generated+"/"+source+".mat");if(!original)throw new InvalidOperationException("Hazard source material missing.");var material=Object.Instantiate(original);material.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(material,path);return material;
        }
        static void Save(Material material){AssetDatabase.SetLabels(material,new[]{"EchoStation-SalvageNext-Generated"});EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);}
        static GameObject Spawn(string model,Transform parent,Vector3 center,Vector3 size,Quaternion rotation,Dictionary<string,Material> materials)
        {
            var go=NextLayout.Spawn(model,parent,center,size,rotation);PrefabUtility.UnpackPrefabInstance(go,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);foreach(var collider in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>(true)){GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,0);renderer.receiveGI=ReceiveGI.LightProbes;renderer.lightmapIndex=-1;renderer.lightProbeUsage=LightProbeUsage.BlendProbes;renderer.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>materials.TryGetValue(m.name,out var value)?value:m).ToArray();}
            return go;
        }
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();Directory.CreateDirectory(Evidence);if(File.Exists(Evidence+"/BeforeHazards.unity"))throw new InvalidOperationException("Hazard setup already attempted.");File.Copy(NextRoomPartition.Main,Evidence+"/BeforeHazards.unity");File.WriteAllText(Evidence+"/NavBefore.json",new JObject{{"path",NextRoomPartition.Nav},{"sha256",Hash(NextRoomPartition.Nav)}}.ToString());
            using(NextSdkSaveScope.Begin(Evidence+"/PrepareSaveScope_A.json"))
            {
            var body1=Copy("ES_NextLit_ES_Trim01","Body01");var body2=Copy("ES_NextLit_ES_Trim02","Body02");var screen=Copy("ES_Screen","OrangeScreen");screen.SetColor("_BaseColor",new Color(1,.5f,.15f));screen.SetColor("_EmissionColor",new Color(1.5f,.25f,.045f));screen.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;screen.EnableKeyword("_EMISSION");var pipe=Copy("ES_Black","PipeMetal");pipe.SetColor("_BaseColor",new Color(.22f,.27f,.31f));pipe.SetFloat("_Metallic",.45f);pipe.SetFloat("_Smoothness",.4f);foreach(var material in new[]{body1,body2,screen,pipe})Save(material);var mapping=new Dictionary<string,Material>{{"ES_Trim01",body1},{"ES_Trim02",body2},{"ES_Screen",screen}};
            var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextRoomPartition.Main,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var camera=root.GetComponentInChildren<Camera>();var targets=root.GetComponentsInChildren<SalvageTargetView>().Where(t=>t.Definition.Kind==SalvageTargetKind.Danger).ToArray();if(targets.Length!=2||root.GetComponentsInChildren<HazardVisualModel>().Length!=0)throw new InvalidOperationException("Expected two original dangers.");var rows=new JArray();int before=root.GetComponentsInChildren<Collider>(true).Length;
                foreach(var target in targets)
                {
                    var original=target.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.GetComponent<TextMesh>()).ToArray();if(original.Length!=1)throw new InvalidOperationException("Expected one original warning cube.");var old=original[0];var collider=old.GetComponent<BoxCollider>();if(!collider)throw new InvalidOperationException("Original warning collision missing.");rows.Add(new JObject{{"id",target.Id},{"position",new JArray(target.transform.position.x,target.transform.position.y,target.transform.position.z)},{"colliderCenter",new JArray(collider.center.x,collider.center.y,collider.center.z)},{"colliderSize",new JArray(collider.size.x,collider.size.y,collider.size.z)},{"colliderLossyScale",new JArray(collider.transform.lossyScale.x,collider.transform.lossyScale.y,collider.transform.lossyScale.z)}});old.enabled=false;
                    var visual=new GameObject("NextHazardVisual_"+target.Id);visual.transform.SetParent(target.transform,false);visual.AddComponent<HazardVisualModel>().Configure(target.Id);var p=target.transform.position;
                    if(target.Id=="danger_power_box")Spawn("Prop_AccessPoint",visual.transform,p,new Vector3(.74f,1.45f,.52f),Quaternion.Euler(90,0,0),mapping);
                    else
                    {
                        var cylinder=GameObject.CreatePrimitive(PrimitiveType.Cylinder);cylinder.name="PipeBody_Visual";cylinder.transform.SetParent(visual.transform,false);cylinder.transform.localScale=new Vector3(.24f,.72f,.24f);Object.DestroyImmediate(cylinder.GetComponent<Collider>());var renderer=cylinder.GetComponent<MeshRenderer>();renderer.sharedMaterial=pipe;renderer.lightProbeUsage=LightProbeUsage.BlendProbes;renderer.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;
                        Spawn("Prop_Fan_Small",visual.transform,p+new Vector3(0,.32f,-.16f),new Vector3(.44f,.44f,.13f),Quaternion.Euler(90,0,0),mapping);Spawn("Prop_Cable_1",visual.transform,p+new Vector3(0,-.45f,0),new Vector3(.42f,.15f,.42f),Quaternion.identity,mapping);
                    }
                    target.gameObject.AddComponent<DistanceShadowCaster>().Configure(camera,visual.GetComponentsInChildren<Renderer>(),30,2,.1f);
                }
                int after=root.GetComponentsInChildren<Collider>(true).Length;if(after!=before||before!=118||Hash(NextRoomPartition.Nav)!=JObject.Parse(File.ReadAllText(Evidence+"/NavBefore.json"))["sha256"].Value<string>())throw new InvalidOperationException("Readonly visual changed physical/nav baseline.");if(!EditorSceneManager.SaveScene(scene))throw new IOException("Hazard scene save failed.");File.WriteAllText(Evidence+"/Prepared_A.json",new JObject{{"beforeColliders",before},{"afterColliders",after},{"targets",rows},{"navUnchanged",true},{"newRealtimeLights",0}}.ToString());
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
            }
        }
        static GameObject Primitive(PrimitiveType type,Transform parent,string name,Vector3 center,Vector3 size,Material material)
        {var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.position=center;go.transform.localScale=size;Object.DestroyImmediate(go.GetComponent<Collider>());var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.lightProbeUsage=LightProbeUsage.BlendProbes;renderer.reflectionProbeUsage=ReflectionProbeUsage.BlendProbes;return go;}
        public static void FaceApproach()
        {
            SceneSafetyGuard.RequireClean();if(File.Exists(Evidence+"/BeforeFacing.unity"))throw new InvalidOperationException("Facing correction already attempted.");File.Copy(NextRoomPartition.Main,Evidence+"/BeforeFacing.unity");
            using(NextSdkSaveScope.Begin(Evidence+"/FacingSaveScope_B.json"))
            {
                var body1=AssetDatabase.LoadAssetAtPath<Material>(FacilityAssets.Generated+"/ES_Next_Hazard_Body01.mat");var body2=AssetDatabase.LoadAssetAtPath<Material>(FacilityAssets.Generated+"/ES_Next_Hazard_Body02.mat");var screen=AssetDatabase.LoadAssetAtPath<Material>(FacilityAssets.Generated+"/ES_Next_Hazard_OrangeScreen.mat");var pipe=AssetDatabase.LoadAssetAtPath<Material>(FacilityAssets.Generated+"/ES_Next_Hazard_PipeMetal.mat");var indicator=Copy("ES_Light","Indicator");indicator.SetColor("_BaseColor",new Color(1,.35f,.06f));indicator.SetColor("_EmissionColor",new Color(2,.32f,.04f));indicator.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;indicator.EnableKeyword("_EMISSION");Save(indicator);var mapping=new Dictionary<string,Material>{{"ES_Trim01",body1},{"ES_Trim02",body2},{"ES_Screen",screen},{"ES_Light",indicator}};
                var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(NextRoomPartition.Main,OpenSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);var root=scene.GetRootGameObjects().Single(r=>r.name=="ES_SalvageNext_World");var camera=root.GetComponentInChildren<Camera>();var models=root.GetComponentsInChildren<HazardVisualModel>();if(models.Length!=2)throw new InvalidOperationException("Expected only owned models.");
                    foreach(var visual in models)
                    {
                        var target=visual.GetComponentInParent<SalvageTargetView>();if(!target||target.Id!=visual.Id)throw new InvalidOperationException("Owned visual identity changed.");var caster=target.GetComponent<DistanceShadowCaster>();caster.Configure(camera,Array.Empty<Renderer>());foreach(var child in visual.transform.Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);var p=target.transform.position;bool power=target.Id=="danger_power_box";
                        if(power)Spawn("Prop_Computer",visual.transform,p,new Vector3(.74f,1.45f,.52f),Quaternion.Euler(0,-90,0),mapping);
                        else{Primitive(PrimitiveType.Cylinder,visual.transform,"PipeBody_Visual",p,new Vector3(.24f,.72f,.24f),pipe);Spawn("Prop_Fan_Small",visual.transform,p+new Vector3(-.16f,.32f,0),new Vector3(.13f,.44f,.44f),Quaternion.Euler(0,0,90),mapping);Spawn("Prop_Cable_1",visual.transform,p+new Vector3(0,-.45f,0),new Vector3(.42f,.15f,.42f),Quaternion.identity,mapping);}
                        Primitive(PrimitiveType.Cube,visual.transform,"AnchoredFoot_Visual",p+new Vector3(0,-.765f,0),power?new Vector3(.78f,.06f,.58f):new Vector3(.46f,.06f,.46f),pipe);Primitive(PrimitiveType.Cube,visual.transform,"WarningIndicator_Visual",p+new Vector3(power?-.385f:-.24f,power ? .3f : .58f,0),power?new Vector3(.02f,.045f,.4f):new Vector3(.012f,.05f,.2f),indicator);caster.Configure(camera,visual.GetComponentsInChildren<Renderer>(),30,2,.1f);
                    }
                    if(root.GetComponentsInChildren<Collider>(true).Length!=118)throw new InvalidOperationException("Visual facing changed collision.");if(!EditorSceneManager.SaveScene(scene))throw new IOException("Facing correction save failed.");
                }
                finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
            }
        }
    }
}
