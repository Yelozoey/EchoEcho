using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace EchoStation.SalvageNext.Editor
{
    public static class NextArtV2Dressing
    {
        public const string Evidence="Docs/Tests/SalvagePrototype/Evidence/Feedback02/V02";
        static Material lit;
        static GameObject Prop(string model,Transform parent,Vector3 center,Vector3 size,Quaternion rotation,string name)
        {
            var go=NextLayout.Spawn(model,parent,center,size,rotation);go.name=name;
            foreach(var renderer in go.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(source=>
            {
                string p=FacilityAssets.Generated+"/ES_NextLit_"+source.name+"_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)).Substring(0,8)+".mat";var own=AssetDatabase.LoadAssetAtPath<Material>(p);return own?own:lit;
            }).ToArray();return go;
        }
        static Material ColorMaterial(string name,Color tint)
        {
            string path=NextArtV2Layout.Generated+"/"+name+".mat";if(File.Exists(path))throw new InvalidOperationException("Keep dressing material");var m=new Material(lit){name=name};m.SetColor("_BaseColor",tint);m.SetFloat("_Smoothness",.35f);AssetDatabase.CreateAsset(m,path);AssetDatabase.SaveAssetIfDirty(m);return m;
        }
        static GameObject Box(string name,Transform parent,Vector3 center,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=center;go.transform.localScale=size;Object.DestroyImmediate(go.GetComponent<Collider>());var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.receiveGI=ReceiveGI.Lightmaps;r.scaleInLightmap=.5f;GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.OccluderStatic|StaticEditorFlags.OccludeeStatic|StaticEditorFlags.ReflectionProbeStatic);return go;
        }
        public static void Prepare()
        {
            SceneSafetyGuard.RequireClean();Directory.CreateDirectory(Evidence);if(File.Exists(Evidence+"/BeforeDressing.unity"))throw new InvalidOperationException("Keep dressing checkpoint");File.Copy(NextArtV2Layout.Authoring,Evidence+"/BeforeDressing.unity");lit=AssetDatabase.LoadAssetAtPath<Material>(FacilityAssets.Generated+"/ES_NextLit_ES_Trim01_8e527ec1.mat");if(!lit)throw new InvalidOperationException("Missing existing painted PBR material");
            var cyan=ColorMaterial("ES_V2_DockAccent",new Color(.22f,.65f,.72f));var amber=ColorMaterial("ES_V2_ServiceAccent",new Color(.72f,.42f,.18f));var old=SceneManager.GetActiveScene();
            using(NextSdkSaveScope.Begin(Evidence+"/SaveScope_A.json"))
            {
                var scene=EditorSceneManager.OpenScene(NextArtV2Layout.Authoring,OpenSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);var world=scene.GetRootGameObjects().Single();var rooms=world.GetComponentsInChildren<RoomVisual>().OrderBy(r=>r.Index).ToArray();var rows=new JArray();
                    for(int i=0;i<3;i++)
                    {
                        var group=new GameObject("ArtV2_Landmark_"+i);group.transform.SetParent(rooms[i].transform,false);var t=group.transform;
                        string texturePath=NextArtV2Layout.Generated+"/Signs/Zone_"+i+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
                        var sign=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="ES_V2_Sign_"+i};sign.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));sign.SetFloat("_Smoothness",.12f);sign.SetFloat("_Cull",0);sign.doubleSidedGI=true;sign.EnableKeyword("_EMISSION");sign.SetTexture("_EmissionMap",sign.GetTexture("_BaseMap"));sign.SetColor("_EmissionColor",Color.white*.12f);sign.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;AssetDatabase.CreateAsset(sign,NextArtV2Layout.Generated+"/ES_V2_Sign_"+i+".mat");AssetDatabase.SaveAssetIfDirty(sign);
                        // A flat sign facing toward the interior. Collider-free and above head clearance.
                        var panel=GameObject.CreatePrimitive(PrimitiveType.Quad);panel.name="ArtV2_ZoneSign_"+i;panel.transform.SetParent(t,false);panel.transform.position=new Vector3(6+12*i,3.45f,23.9f);panel.transform.localScale=new Vector3(4,1,1);Object.DestroyImmediate(panel.GetComponent<Collider>());panel.GetComponent<MeshRenderer>().sharedMaterial=sign;panel.GetComponent<MeshRenderer>().scaleInLightmap=.5f;GameObjectUtility.SetStaticEditorFlags(panel,StaticEditorFlags.ContributeGI|StaticEditorFlags.OccludeeStatic);
                        if(i==0)
                        {
                            Box("Dock_LeftGuide",t,new Vector3(3.35f,.095f,12),new Vector3(.12f,.015f,3.3f),cyan);Box("Dock_RightGuide",t,new Vector3(6.65f,.095f,12),new Vector3(.12f,.015f,3.3f),cyan);Box("Dock_BackGuide",t,new Vector3(5,.095f,13.65f),new Vector3(3.4f,.015f,.12f),cyan);
                            for(int x=0;x<3;x++)Prop("Prop_AccessPoint",t,new Vector3(2+x*4,2.75f,23.65f),new Vector3(1.2f,.8f,.5f),Quaternion.identity,"Dock_ServicePanel_"+x);
                            Prop("TopSimple_Straight",t,new Vector3(6,4.15f,23.7f),new Vector3(10,.2f,.35f),Quaternion.Euler(0,90,0),"Dock_HeaderRail");
                        }
                        else if(i==1)
                        {
                            for(int shelf=0;shelf<2;shelf++)
                            {
                                float y=2.3f+shelf*.9f;Prop("Platform_Metal2",t,new Vector3(18,y,23.25f),new Vector3(9,.05f,1.1f),Quaternion.identity,"Storage_WallShelf_"+shelf);
                                for(int c=0;c<4;c++)Prop(c%2==0?"Prop_Crate4":"Prop_Chest",t,new Vector3(14.5f+c*2.3f,y+.32f,23.3f),new Vector3(1.05f,.6f,.65f),Quaternion.identity,"Storage_OverheadCargo_"+shelf+"_"+c);
                            }
                            // Label on opposite wall so stacked storage never occludes it.
                            panel.transform.position=new Vector3(18,3.55f,.1f);panel.transform.rotation=Quaternion.Euler(0,180,0);
                            for(int p=0;p<3;p++)Prop("Column_MetalSupport",t,new Vector3(14+p*4,3.05f,23.85f),new Vector3(.18f,1.8f,.3f),Quaternion.identity,"Storage_WallBracket_"+p);
                        }
                        else
                        {
                            Prop("Prop_Fan_Small",t,new Vector3(30,3.1f,23.45f),new Vector3(2,1.7f,.6f),Quaternion.Euler(90,0,0),"Maintenance_Extractor");panel.transform.position=new Vector3(30,4.35f,22.4f);
                            for(int p=0;p<2;p++)Prop("Prop_Cable_1",t,new Vector3(30,4.1f+p*.35f,22.95f),new Vector3(10,.18f,.25f),Quaternion.Euler(0,90,0),"Maintenance_HeaderCable_"+p);
                            for(int p=0;p<2;p++)Prop("Prop_AccessPoint",t,new Vector3(26+p*8,2.85f,23.65f),new Vector3(1.2f,1.1f,.5f),Quaternion.identity,"Maintenance_DiagnosticPanel_"+p);
                            Box("Maintenance_WarningRail",t,new Vector3(30,2.2f,23.9f),new Vector3(9,.12f,.06f),amber);
                        }
                        rows.Add(new JObject{{"room",i},{"landmark",group.name},{"renderers",group.GetComponentsInChildren<MeshRenderer>().Length},{"newColliders",group.GetComponentsInChildren<Collider>().Length}});
                    }
                    // Give only new geometry independent UV2; never rewrite imported model meshes.
                    var meshes=new Dictionary<Mesh,Mesh>();foreach(var filter in rooms.SelectMany(r=>r.GetComponentsInChildren<MeshFilter>()))
                    {
                        if(filter.sharedMesh.uv2.Length==filter.sharedMesh.vertexCount)continue;var source=filter.sharedMesh;if(!meshes.TryGetValue(source,out var mesh)){mesh=Object.Instantiate(source);mesh.name="ES_V2_Uv2_"+meshes.Count;Unwrapping.GenerateSecondaryUVSet(mesh);AssetDatabase.CreateAsset(mesh,NextArtV2Layout.Generated+"/"+mesh.name+".asset");AssetDatabase.SaveAssetIfDirty(mesh);meshes.Add(source,mesh);}if(PrefabUtility.IsPartOfPrefabInstance(filter))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(filter),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);filter.sharedMesh=mesh;
                    }
                    if(!EditorSceneManager.SaveScene(scene))throw new IOException("Dressing scene save failed");File.WriteAllText(Evidence+"/Prepared_A.json",new JObject{{"rooms",rows},{"newCollisionFootprints",0},{"sourceMeshesUnmodified",true},{"uv2Copies",meshes.Count}}.ToString());
                }
                finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);}
            }
        }
    }
}
