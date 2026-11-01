using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EchoStation.InfiltrationArt.Editor
{
    public static class ArtSampleAssets
    {
        public const string Root = "Assets/EchoStation/InfiltrationArt";
        public const string Generated = Root + "/Generated";
        public const string Prefabs = Root + "/Prefabs";
        public const string ThirdParty = Root + "/ThirdParty";
        public const string Evidence = "Docs/Tests/InfiltrationPrototype/Evidence/A00";
        public const string Owner = "EchoStation-A00-ArtSample";
        public static readonly string[] Roles = { "Floor", "Wall", "FeatureWall", "WallCap", "DoorFrame", "Column", "Terminal", "EquipmentCase", "CeilingLight", "Desk", "Chair", "Door", "Guard", "Echo", "Toolbox" };
        public static string PathFor(string role) => Prefabs + "/IA_" + role + ".prefab";

        public static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            Folder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
        public static void Save(Object obj)
        {
            AssetDatabase.SetLabels(obj, new[] { Owner });
            EditorUtility.SetDirty(obj);
            AssetDatabase.SaveAssetIfDirty(obj);
        }
        static void RequireOwned(Object obj)
        {
            if (obj && !AssetDatabase.GetLabels(obj).Contains(Owner))
                throw new InvalidOperationException("Preserve foreign asset: " + AssetDatabase.GetAssetPath(obj));
        }
        public static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer) && !r.GetComponent<TextMesh>()).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("Actual source geometry required: " + go.name);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        public static Bounds LocalBoundsOf(GameObject root)
        {
            bool first=true;var result=new Bounds();
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if(!filter.sharedMesh)continue;
                var b=filter.sharedMesh.bounds;var matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                for(int i=0;i<8;i++)
                {
                    var p=matrix.MultiplyPoint3x4(new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z));
                    if(first){result=new Bounds(p,Vector3.zero);first=false;}else result.Encapsulate(p);
                }
            }
            if(first)throw new InvalidOperationException("No collider geometry: "+root.name);
            return result;
        }

        static Material Adapt(Material source)
        {
            if (!source) throw new InvalidOperationException("Missing source material");
            if (!source.name.StartsWith("ES_",StringComparison.Ordinal) && source.name.ToLowerInvariant().Contains("trim"))
                source = EchoStation.SalvageNext.Editor.FacilityAssets.MaterialFor(source.name);
            string name = source.name.Replace(" (Instance)", "").Replace('.', '_').Replace(' ', '_');
            string path = Generated + "/IA_" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            RequireOwned(result);
            if (!result)
            {
                result = source.shader.name == "Universal Render Pipeline/Lit"
                    ? Object.Instantiate(source) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                result.name = "IA_" + name;
                AssetDatabase.CreateAsset(result, path);
                if (source.shader.name != "Universal Render Pipeline/Lit")
                {
                    if (source.HasProperty("_Color")) result.SetColor("_BaseColor", source.GetColor("_Color"));
                    string batch = name.Contains("Batch2") ? "Batch2" : name.Contains("Batch1") ? "Batch1" : null;
                    if (batch != null)
                    {
                        string textureRoot = ThirdParty + "/sci-fi-essentials-kit/T_Props_" + batch;
                        result.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + "_BaseColor.png"));
                        result.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(textureRoot + "_Normal.png"));
                        result.SetFloat("_BumpScale", .65f);
                        result.EnableKeyword("_NORMALMAP");
                    }
                }
            }
            result.enableInstancing = true;
            result.SetFloat("_Smoothness", .28f);
            result.SetFloat("_Metallic", .18f);
            if (name.StartsWith("ES_Trim", StringComparison.Ordinal) && !name.Contains("Dark"))
                result.SetColor("_BaseColor", new Color(.94f, .97f, 1));
            if (name == "ES_Light")
            {
                result.SetColor("_BaseColor", new Color(1, .94f, .82f));
                result.SetColor("_EmissionColor", new Color(1, .9f, .72f) * 2);
            }
            if(name=="Main")result.SetColor("_BaseColor",new Color(.20f,.48f,.66f));
            if(name=="Grey")result.SetColor("_BaseColor",new Color(.64f,.71f,.76f));
            Save(result);
            return result;
        }

        static void PrepareImportedModels()
        {
            foreach (string path in Directory.GetFiles(ThirdParty, "*.png", SearchOption.AllDirectories))
            {
                var t = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                bool normal = path.Contains("_Normal");
                t.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                t.sRGBTexture = !normal && !path.Contains("_ORM");
                t.maxTextureSize = 1024;
                t.isReadable = false;
                t.mipmapEnabled = true;
                t.textureCompression = TextureImporterCompression.CompressedHQ;
                t.SaveAndReimport();
            }
            foreach (string filename in Directory.GetFiles(ThirdParty, "*.fbx", SearchOption.AllDirectories))
            {
                string path = filename.Replace('\\', '/');
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                bool guard = path.EndsWith("/Robot.fbx", StringComparison.Ordinal);
                importer.globalScale = 1;
                importer.useFileScale = true;
                importer.isReadable = true;
                importer.importCameras = false;
                importer.importLights = false;
                importer.addCollider = false;
                importer.generateSecondaryUV = !guard;
                importer.importNormals = ModelImporterNormals.Import;
                importer.importTangents = ModelImporterTangents.CalculateMikk;
                importer.importAnimation = guard;
                if (guard) { importer.animationType = ModelImporterAnimationType.Generic; SetIdleLoop(importer); }
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
                importer.SaveAndReimport();
            }
        }
        static void SetIdleLoop(ModelImporter importer)
        {
            var clips=importer.clipAnimations.Length>0?importer.clipAnimations:importer.defaultClipAnimations;
            bool found=false;
            foreach(var clip in clips)if(clip.name.IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0){clip.loopTime=true;found=true;}
            if(!found)throw new InvalidOperationException("Authored idle take missing");
            importer.clipAnimations=clips;
        }
        public static void LoopGuardIdle()
        {
            SceneSafetyGuard.RequireClean();string source=ThirdParty+"/lowpoly-robot/Robot.fbx";
            string receipt=Evidence+"/GuardLoopFix_A.json";if(File.Exists(receipt))throw new InvalidOperationException("Preserve loop receipt");
            File.Copy(source+".meta",Evidence+"/RobotBeforeLoop_A.meta",false);
            var importer=(ModelImporter)AssetImporter.GetAtPath(source);SetIdleLoop(importer);importer.SaveAndReimport();
            var clip=AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Single(c=>c.name.IndexOf("idle",StringComparison.OrdinalIgnoreCase)>=0&&!c.name.StartsWith("__"));
            if(!clip.isLooping)throw new InvalidOperationException("Imported idle still not looping");
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Generated+"/IA_GuardIdle.controller");controller.layers[0].stateMachine.defaultState.motion=clip;Save(controller);
            File.WriteAllText(receipt,new JObject{{"source",source},{"clip",clip.name},{"looping",clip.isLooping},{"length",clip.length},{"sourceFbxBytesChanged",false}}.ToString());
        }

        static AnimatorController GuardController()
        {
            string path = Generated + "/IA_GuardIdle.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing) { RequireOwned(existing); return existing; }
            var clips = AssetDatabase.LoadAllAssetsAtPath(ThirdParty + "/lowpoly-robot/Robot.fbx")
                .OfType<AnimationClip>().Where(c => !c.name.StartsWith("__", StringComparison.Ordinal)).ToArray();
            var clip = clips.FirstOrDefault(c => c.name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0);
            if (!clip) throw new InvalidOperationException("Source idle animation required: " + string.Join(",", clips.Select(c => c.name)));
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var state = controller.layers[0].stateMachine.AddState("Idle");
            state.motion = clip;
            controller.layers[0].stateMachine.defaultState = state;
            Save(controller);
            return controller;
        }

        static string SourceFor(string role)
        {
            string old = "Assets/EchoStation/SalvageNext/Prefabs/Facilities/ES_";
            switch (role)
            {
                case "Floor": return old + "Platform_Simple.prefab";
                case "Wall": return old + "WallBand_Straight.prefab";
                case "FeatureWall": return old + "WallAstra_Straight.prefab";
                case "WallCap": return old + "ShortWall_Simple1_Straight.prefab";
                case "DoorFrame": return old + "Door_Frame_SquareTall.prefab";
                case "Column": return old + "Column_Simple.prefab";
                case "Terminal": return old + "Prop_Computer.prefab";
                case "EquipmentCase": return old + "Prop_Chest.prefab";
                case "CeilingLight": return old + "Prop_Light_Wide.prefab";
                case "Desk": return ThirdParty + "/sci-fi-essentials-kit/Prop_Desk_Medium.fbx";
                case "Chair": return ThirdParty + "/sci-fi-essentials-kit/Prop_Chair.fbx";
                case "Door": return ThirdParty + "/modular-sci-fi-megakit/Door_Simple.fbx";
                case "Guard": return ThirdParty + "/lowpoly-robot/Robot.fbx";
                case "Echo": return "Assets/EchoStation/SalvageNext/Prefabs/Echo/ES_Echo_Rover_Visual.prefab";
                case "Toolbox": return "Assets/EchoStation/SalvageNext/Prefabs/Cargo/ES_cargo_a_Visual.prefab";
                default: throw new ArgumentException(role);
            }
        }

        [MenuItem("Tools/EchoStation/Infiltration/A00/1 Prepare Art Assets")]
        public static void Prepare() => PrepareVersion("A");

        public static void PrepareVersion(string label)
        {
            SceneSafetyGuard.RequireClean();
            if(string.IsNullOrEmpty(label)||label.Any(c=>!char.IsLetterOrDigit(c)))throw new ArgumentException("Safe artifact version required");
            string receipt=Evidence+"/PreparedAssets_"+label+".json";
            if(File.Exists(receipt))throw new InvalidOperationException("Preserve previous asset receipt");
            Folder(Generated); Folder(Prefabs); Directory.CreateDirectory(Evidence);
            PrepareImportedModels();
            var controller = GuardController();
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var rows = new JArray();
            try
            {
                SceneManager.SetActiveScene(scene);
                foreach (string role in Roles)
                {
                    string sourcePath = SourceFor(role);
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                    if (!source) throw new InvalidOperationException("Missing source model " + sourcePath);
                    var root = new GameObject("IA_" + role);
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, scene);
                    instance.transform.SetParent(root.transform, false);
                    PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    // Remove gameplay scripts only from this new isolated instance, never from source assets.
                    foreach (var script in instance.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
                    foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                    foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        renderer.sharedMaterials = renderer.sharedMaterials.Select(Adapt).ToArray();
                        renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                        renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
                    }
                    if(role=="Guard" && !instance.GetComponentInChildren<Animator>(true)) instance.AddComponent<Animator>();
                    foreach (var animation in instance.GetComponentsInChildren<Animator>(true))
                    {
                        if (role == "Guard") { animation.runtimeAnimatorController = controller; animation.applyRootMotion = false; animation.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
                        else Object.DestroyImmediate(animation);
                    }
                    Bounds before = BoundsOf(instance);
                    float scale = role == "Guard" ? 1.8f / before.size.y
                        : role == "Desk" ? 2.25f / before.size.x
                        : role == "Chair" ? 1.1f / before.size.y
                        : role == "Door" ? 3.2f / before.size.y
                        : role == "Toolbox" ? .65f / Mathf.Max(before.size.x, before.size.y, before.size.z)
                        : 1;
                    instance.transform.localScale *= scale;
                    var bounds = BoundsOf(instance);
                    instance.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    root.AddComponent<ArtSampleModel>().Configure(role, sourcePath);
                    string destination = PathFor(role);
                    RequireOwned(AssetDatabase.LoadMainAssetAtPath(destination));
                    var prefab = PrefabUtility.SaveAsPrefabAsset(root, destination);
                    Save(prefab);
                    bounds = BoundsOf(root);
                    rows.Add(new JObject { { "role", role }, { "source", sourcePath }, { "prefab", destination },
                        { "size", new JArray(bounds.size.x, bounds.size.y, bounds.size.z) } });
                    Object.DestroyImmediate(root);
                }
            }
            finally { SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene, true); }
            File.WriteAllText(receipt, rows.ToString());
        }
    }
}
