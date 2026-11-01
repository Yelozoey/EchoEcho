using System;
using System.IO;
using System.Linq;
using EchoStation.Player;
using EchoStation.Rendering.Editor;
using EchoStation.Salvage;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace EchoStation.InfiltrationArt.Editor
{
    public static class ArtSampleScene
    {
        public const string ScenePath = ArtSampleAssets.Root + "/Scenes/ES_Infiltration_ArtSample.unity";
        public const string PipelinePath = "Assets/EchoStation/Rendering/Settings/ES_Gameplay_ForwardHDR.asset";
        public const string LightingPath = ArtSampleAssets.Generated + "/IA_Lighting.asset";
        public const string VolumePath = ArtSampleAssets.Generated + "/IA_Volume.asset";
        public const string FontPath = "Assets/EchoStation/ThirdParty/Noto/Resources/NotoSansCJKsc-Regular.otf";
        public static readonly Vector3[] ViewPositions = { new Vector3(0, 1.6f, -4.7f), new Vector3(-2.6f, 1.6f, -.8f), new Vector3(2.6f, 1.6f, 3.1f), new Vector3(-.5f, 1.6f, -.7f) };
        public static readonly Vector3[] ViewTargets = { new Vector3(-.2f, 1.45f, 1.2f), new Vector3(-.5f, .65f, -2.1f), new Vector3(2.6f, 1.2f, 5.6f), new Vector3(-.5f, 1.05f, .8f) };
        static Transform parent;

        static GameObject Model(string role, string name, Vector3 position, Vector3 scale, float yaw = 0, bool isStatic = true, bool collide = true)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ArtSampleAssets.PathFor(role));
            if (!source) throw new InvalidOperationException("Prepare actual model: " + role);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(source, parent.gameObject.scene);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.position = position; go.transform.rotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = scale;
            if (isStatic)
                foreach (var t in go.GetComponentsInChildren<Transform>()) GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                    StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            if (collide)
            {
                if (role == "DoorFrame")
                {
                    foreach (var filter in go.GetComponentsInChildren<MeshFilter>()) filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
                }
                else
                {
                    var bounds = ArtSampleAssets.LocalBoundsOf(go);
                    var box = go.AddComponent<BoxCollider>();
                    box.center = bounds.center;
                    box.size = new Vector3(Mathf.Max(.05f,bounds.size.x), Mathf.Max(.12f, bounds.size.y), Mathf.Max(.05f,bounds.size.z));
                    if (role == "Floor") { box.center = new Vector3(0, -.08f, 0); box.size = new Vector3(4, .16f, 4); }
                }
            }
            return go;
        }
        static void Text(string name, string text, Vector3 position, float size, Color color, float yaw = 0)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = position; go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var mesh = go.AddComponent<TextMesh>(); mesh.text = text; mesh.font = ResolveFont(); mesh.fontSize = 80;
            mesh.characterSize = size; mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center; mesh.color = color;
            if (mesh.font) mesh.GetComponent<MeshRenderer>().sharedMaterial = LabelMaterial(mesh.font);
        }
        static Material LabelMaterial(Font font)
        {
            string path=ArtSampleAssets.Generated+"/IA_WorldLabel.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("EchoStation/InfiltrationWorldLabel"));material.name="IA_WorldLabel";AssetDatabase.CreateAsset(material,path);}
            material.mainTexture=font.material.mainTexture;ArtSampleAssets.Save(material);return material;
        }
        public static Font ResolveFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font) return font;
            var path = AssetDatabase.FindAssets("t:Font", new[] { "Assets/EchoStation" }).Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(p => p.IndexOf("Noto", StringComparison.OrdinalIgnoreCase) >= 0);
            if (path == null) throw new InvalidOperationException("Existing Chinese font required");
            return AssetDatabase.LoadAssetAtPath<Font>(path);
        }
        static LightingSettings Lighting()
        {
            var existing = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingPath); if (existing) return existing;
            var settings = new LightingSettings { name = "IA_Lighting", lightmapper = LightingSettings.Lightmapper.UnityComputeGPU,
                bakedGI = true, realtimeGI = false, autoGenerate = false, lightmapResolution = 12, lightmapMaxSize = 1024,
                lightmapPadding = 4, directionalityMode = LightmapsMode.CombinedDirectional, lightmapCompression = LightmapCompression.HighQuality,
                directSampleCount = 32, indirectSampleCount = 64, environmentSampleCount = 32, minBounces = 2, maxBounces = 3,
                indirectScale = 1, albedoBoost = 1, lightProbeSampleCountMultiplier = 2, ao = false,
                filteringMode = LightingSettings.FilterMode.Advanced, denoiserTypeDirect = LightingSettings.DenoiserType.None,
                denoiserTypeIndirect = LightingSettings.DenoiserType.None, filterTypeDirect = LightingSettings.FilterType.Gaussian,
                filterTypeIndirect = LightingSettings.FilterType.Gaussian, filteringGaussianRadiusDirect = 1, filteringGaussianRadiusIndirect = 2 };
            AssetDatabase.CreateAsset(settings, LightingPath); ArtSampleAssets.Save(settings); return settings;
        }
        static VolumeProfile VolumeProfile()
        {
            var old = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath); if (old) return old;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>(); profile.name = "IA_Volume"; AssetDatabase.CreateAsset(profile, VolumePath);
            var tone = profile.Add<Tonemapping>(true); tone.mode.value = TonemappingMode.ACES; AssetDatabase.AddObjectToAsset(tone, profile);
            var color = profile.Add<ColorAdjustments>(true); color.postExposure.value = .45f; color.contrast.value = 2; color.saturation.value = -5; AssetDatabase.AddObjectToAsset(color, profile);
            var bloom = profile.Add<Bloom>(true); bloom.intensity.value = .12f; bloom.threshold.value = 1.2f; AssetDatabase.AddObjectToAsset(bloom, profile);
            ArtSampleAssets.Save(profile); return profile;
        }

        [MenuItem("Tools/EchoStation/Infiltration/A00/2 Build Sample Scene")]
        public static void Create()
        {
            SceneSafetyGuard.RequireClean();
            if (AssetDatabase.LoadMainAssetAtPath(ScenePath)) throw new InvalidOperationException("Preserve existing sample scene");
            ArtSampleAssets.Folder(ArtSampleAssets.Root + "/Scenes");
            var lightSettings = Lighting(); var profile = VolumeProfile(); var font = ResolveFont();
            var oldScene = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                parent = new GameObject("IA_ArtSample_World").transform;
                for (int x = 0; x < 2; x++) for (int z = 0; z < 3; z++)
                {
                    Model("Floor", "Floor_" + x + "_" + z, new Vector3(-2 + x * 4, 0, -4 + z * 4), Vector3.one);
                    var roof = Model("Floor", "Ceiling_" + x + "_" + z, new Vector3(-2 + x * 4, 4.5f, -4 + z * 4), Vector3.one);
                    roof.transform.rotation = Quaternion.Euler(180, 0, 0);
                    Model("CeilingLight", "CeilingFixture_" + x + "_" + z, new Vector3(-2 + x * 4, 4.28f, -4 + z * 4), Vector3.one, collide: false);
                    var lamp = new GameObject("BakedCeiling_" + x + "_" + z).AddComponent<Light>(); lamp.transform.SetParent(parent, false);
                    lamp.type = LightType.Rectangle; lamp.lightmapBakeType = LightmapBakeType.Baked; lamp.areaSize = new Vector2(2.4f, 1.1f);
                    lamp.transform.position = new Vector3(-2 + x * 4, 4.2f, -4 + z * 4); lamp.transform.rotation = Quaternion.Euler(90, 0, 0);
                    lamp.color = new Color(1, .94f, .85f); lamp.intensity = 65; lamp.bounceIntensity = 1; lamp.shadows = LightShadows.Soft;
                }
                for (int z = 0; z < 3; z++)
                {
                    Model("Wall", "WestWall_" + z, new Vector3(-4, 0, -4 + z * 4), new Vector3(1, 1.5f, 1), 180);
                    Model("Wall", "EastWall_" + z, new Vector3(4, 0, -4 + z * 4), new Vector3(1, 1.5f, 1));
                }
                for (int x = 0; x < 2; x++)
                {
                    Model("Wall", "EntryBoundary_" + x, new Vector3(-2 + x * 4, 0, -6), new Vector3(1, 1.5f, 1), 90);
                    Model("FeatureWall", "FarWall_" + x, new Vector3(-2 + x * 4, 0, 6), new Vector3(.45f, 1.486f, 1), -90);
                }
                foreach (float x in new[] { -3.72f, 3.72f }) foreach (float z in new[] { -5.7f, 1.9f, 5.7f })
                    Model("Column", "StructurePillar", new Vector3(x, 0, z), new Vector3(.7f, .9f, .55f), x > 0 ? 180 : 0);
                Model("Wall", "CornerDivider", new Vector3(1.15f, 0, 4), new Vector3(1, 1.5f, 1));
                Model("DoorFrame", "CornerDoorFrame", new Vector3(2.55f, 0, 2), new Vector3(.53f, .68f, 1));
                Model("Door", "StaffDoor_ArtBoundary", new Vector3(2.55f, 0, 5.62f), Vector3.one, 180);
                Model("Desk", "ReceptionDesk", new Vector3(-.55f, 0, .55f), Vector3.one, 180);
                Model("Chair", "ReceptionChair", new Vector3(-2.7f, 0, 1.4f), Vector3.one, 160);
                Model("Chair", "WaitingChair", new Vector3(-3.1f, 0, -3.8f), Vector3.one, 90);
                Model("Terminal", "VisitorTerminal", new Vector3(1.05f, 0, .25f), Vector3.one, 180);
                Model("EquipmentCase", "CornerEquipment", new Vector3(2.6f, 0, 4.75f), Vector3.one * .55f);
                Model("Toolbox", "MaintenanceToolbox", new Vector3(-.55f, .75f, .55f), Vector3.one, 180);
                Model("Guard", "Guard_ArtOnly", new Vector3(-.5f, 0, 2), Vector3.one, 180, false);
                Model("Echo", "Echo_ArtOnly", new Vector3(-1.35f, 0, -2.1f), Vector3.one * 1.12f, 155, false);
                Text("ReceptionTitle", "ACCESS / 01", new Vector3(-1.45f, 3.1f, 5.65f), .035f, new Color(.38f, .62f, .78f));
                Text("ReceptionSubtitle", "访客接待 · 运维检查", new Vector3(-1.45f, 2.65f, 5.64f), .014f, new Color(.30f, .38f, .48f));
                Text("CornerTitle", "STAFF ONLY", new Vector3(2.55f, 3.8f, 1.75f), .018f, new Color(1, .6f, .24f));
                var key = new GameObject("RealtimeKey").AddComponent<Light>(); key.transform.SetParent(parent, false); key.type = LightType.Spot;
                key.transform.position = new Vector3(-.5f, 4.1f, .8f); key.transform.rotation = Quaternion.Euler(78, 0, 0); key.range = 8; key.spotAngle = 110;
                key.color = new Color(.82f, .91f, 1); key.intensity = 2.2f; key.shadows = LightShadows.Soft; key.shadowNearPlane = .15f;
                var probes = new GameObject("LightProbes").AddComponent<LightProbeGroup>(); probes.transform.SetParent(parent, false);
                var positions = new System.Collections.Generic.List<Vector3>();
                foreach (float x in new[] { -3f, 0, 3f }) foreach (float z in new[] { -4f, 0, 4f }) foreach (float y in new[] { .4f, 1.6f, 3.2f }) positions.Add(new Vector3(x, y, z));
                probes.probePositions = positions.ToArray();
                Lightmapping.lightingSettings = lightSettings;
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.24f, .29f, .36f); RenderSettings.fog = false;
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/EchoStation/SalvageNext/Generated/ES_Next_StudioReflection.asset");
                RenderSettings.reflectionIntensity = .7f;
                var volume = new GameObject("ArtVolume").AddComponent<Volume>(); volume.transform.SetParent(parent, false); volume.isGlobal = true; volume.sharedProfile = profile;
                var player = new GameObject("ArtSamplePlayer"); player.SetActive(false); player.transform.SetParent(parent, false); player.transform.position = new Vector3(0, .08f, -4.7f);
                var body = player.AddComponent<CharacterController>(); body.height = 1.75f; body.radius = .3f; body.center = Vector3.up * .875f; body.stepOffset = .25f; body.skinWidth = .03f; body.minMoveDistance=0;
                var camera = new GameObject("ArtSampleCamera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.transform.SetParent(player.transform, false); camera.transform.localPosition = Vector3.up * 1.6f;
                camera.allowHDR = true; camera.nearClipPlane = .05f; camera.farClipPlane = 45; camera.fieldOfView = 65; camera.backgroundColor = new Color(.08f, .1f, .13f); camera.clearFlags = CameraClearFlags.SolidColor;
                var data = camera.GetUniversalAdditionalCameraData(); data.allowHDROutput = false; data.renderPostProcessing = true; data.requiresDepthTexture = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camera.gameObject.AddComponent<AudioListener>();
                var input = player.AddComponent<PlayerInputReader>(); var motor = player.AddComponent<SalvageMotor>(); motor.Configure(camera.transform);
                var runtime = player.AddComponent<ArtSampleRuntime>(); runtime.Configure(AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(PipelinePath), input, font);
                player.SetActive(true);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Owned sample save failed");
                Directory.CreateDirectory(ArtSampleAssets.Evidence);
                File.WriteAllText(ArtSampleAssets.Evidence + "/CreatedScene_A.json", new JObject { { "scene", ScenePath }, { "heightMeters", 4.5 }, { "floorAreaMeters2", 96 },
                    { "models", parent.GetComponentsInChildren<ArtSampleModel>().Length }, { "bakedAreaLights", 6 }, { "realTimeLights", 1 }, { "probes", positions.Count },
                    { "aiConnected", false }, { "cpu", SystemInfo.processorType }, { "gpu", SystemInfo.graphicsDeviceName }, { "ramMiB", SystemInfo.systemMemorySize }, { "os", SystemInfo.operatingSystem } }.ToString());
            }
            finally { SceneManager.SetActiveScene(oldScene); EditorSceneManager.CloseScene(scene, true); parent = null; }
        }

        public static void RefineFirstReview()
        {
            SceneSafetyGuard.RequireClean();string receipt=ArtSampleAssets.Evidence+"/VisualRefinement_B.json";
            if(File.Exists(receipt))throw new InvalidOperationException("Preserve refinement receipt");
            string archive=ArtSampleAssets.Evidence+"/ArtDraft_A";Directory.CreateDirectory(archive);
            foreach(string file in Directory.GetFiles(Path.GetDirectoryName(ScenePath),"*",SearchOption.AllDirectories).Where(p=>!p.EndsWith(".meta")))
                File.Copy(file,archive+"/"+Path.GetFileName(file),false);
            var packed=new System.Collections.Generic.Dictionary<string,Texture2D>();
            foreach(string path in AssetDatabase.FindAssets("t:Material",new[]{ArtSampleAssets.Generated}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!material||material.shader.name!="Universal Render Pipeline/Lit")continue;
                if(material.name=="IA_ES_Trim02")
                {
                    material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EchoStation/SalvageNext/ThirdParty/Quaternius/Textures/T_Trim_02_BaseColor.png"));
                    material.SetColor("_BaseColor",new Color(.68f,.78f,.87f));
                }
                if(material.name=="IA_ES_Trim03")material.SetColor("_BaseColor",new Color(.72f,.79f,.86f));
                var source=material.GetTexture("_MetallicGlossMap");
                if(source)
                {
                    string original=AssetDatabase.GetAssetPath(source);
                    if(!packed.TryGetValue(original,out var texture))
                    {
                        string output=ArtSampleAssets.Generated+"/IA_Painted_"+source.name+".png";
                        if(File.Exists(output))throw new InvalidOperationException("Preserve previous painted mask");
                        var image=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                        try{if(!image.LoadImage(File.ReadAllBytes(original)))throw new IOException(original);var pixels=image.GetPixels32();for(int i=0;i<pixels.Length;i++)pixels[i].r=(byte)Mathf.RoundToInt(pixels[i].r*.12f);image.SetPixels32(pixels);image.Apply();File.WriteAllBytes(output,image.EncodeToPNG());}
                        finally{Object.DestroyImmediate(image);}
                        AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceSynchronousImport);var importer=(TextureImporter)AssetImporter.GetAtPath(output);
                        importer.sRGBTexture=false;importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
                        texture=AssetDatabase.LoadAssetAtPath<Texture2D>(output);packed.Add(original,texture);
                    }
                    material.SetTexture("_MetallicGlossMap",texture);material.SetTexture("_OcclusionMap",texture);
                }
                ArtSampleAssets.Save(material);
            }
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumePath);profile.TryGet<ColorAdjustments>(out var color);color.postExposure.value=.45f;ArtSampleAssets.Save(profile);
            var old=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);parent=scene.GetRootGameObjects().Single(g=>g.name=="IA_ArtSample_World").transform;
                foreach(var light in parent.GetComponentsInChildren<Light>())light.intensity=light.lightmapBakeType==LightmapBakeType.Baked?65:2.2f;
                var material=LabelMaterial(ResolveFont());
                foreach(var mesh in parent.GetComponentsInChildren<TextMesh>())
                {
                    mesh.transform.rotation=Quaternion.identity;mesh.GetComponent<MeshRenderer>().sharedMaterial=material;
                    mesh.characterSize=mesh.name=="ReceptionTitle"?.035f:mesh.name=="ReceptionSubtitle"?.014f:.018f;
                    if(mesh.name=="ReceptionTitle")mesh.color=new Color(.38f,.62f,.78f);
                    if(mesh.name=="ReceptionSubtitle")mesh.color=new Color(.30f,.38f,.48f);
                    if(mesh.name=="CornerTitle")mesh.transform.position=new Vector3(2.55f,3.9f,1.92f);
                }
                var floating=parent.Find("TerminalLabel");if(floating)Object.DestroyImmediate(floating.gameObject);
                var divider=parent.Find("CornerDivider");divider.position=new Vector3(1.10f,0,4);divider.localScale=new Vector3(.5f,1.5f,1);
                Model("Wall","CornerDivider_ReturnFace",new Vector3(1.20f,0,4),new Vector3(.5f,1.5f,1),180);
                Model("WallCap","CornerHeaderPanel",new Vector3(2.55f,3.4f,2),new Vector3(1,1.1f,.68f),-90,collide:false);
                for(int i=0;i<2;i++)
                {
                    var l=new GameObject("BakedFrontFill_"+i).AddComponent<Light>();l.transform.SetParent(parent,false);l.type=LightType.Rectangle;l.lightmapBakeType=LightmapBakeType.Baked;
                    l.transform.position=i==0?new Vector3(0,2.9f,-4.8f):new Vector3(2.6f,2.7f,3.3f);l.areaSize=new Vector2(i==0?4:2,.6f);l.color=new Color(.83f,.91f,1);l.intensity=12;
                }
                EditorSceneManager.SaveScene(scene);
            }
            finally{SceneManager.SetActiveScene(old);EditorSceneManager.CloseScene(scene,true);parent=null;}
            File.WriteAllText(receipt,new JObject{{"sourceImages","View_0-3_3bc3abeb.png"},{"textMirrorAndScaleCorrected",true},{"metalRedMultiplier",.12},{"greenBlueAlphaUnchanged",true},{"neutralWallAtlas",true},{"bakedCeilingIntensity",65},{"exposure",.45},{"doubleSidedDivider",true},{"rebakeRequired",true}}.ToString());
        }
    }
}
