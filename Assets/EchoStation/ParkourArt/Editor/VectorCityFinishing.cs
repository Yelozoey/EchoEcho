using System;
using System.IO;
using System.Linq;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.ParkourArt.Editor
{
    public static class VectorCityFinishing
    {
        public const string ProfilePath=VectorCityBuilder.Root+"/Settings/VC_ColorOnly.asset";
        [MenuItem("Tools/EchoStation/Parkour Art/05 Configure Color Only Volume")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var scene=SceneManager.GetActiveScene();if(scene.path!=VectorCityBuilder.ScenePath)throw new InvalidOperationException("Open the parkour scene first.");
            var root=scene.GetRootGameObjects().Single(g=>g.hideFlags==HideFlags.None).transform;
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,ProfilePath);}
            if(!profile.TryGet<ColorAdjustments>(out var color)){color=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(color,profile);}
            color.postExposure.Override(-.15f);color.contrast.Override(7);color.saturation.Override(-2);color.hueShift.Override(0);color.colorFilter.Override(Color.white);
            if(!profile.TryGet<WhiteBalance>(out var balance)){balance=profile.Add<WhiteBalance>(true);AssetDatabase.AddObjectToAsset(balance,profile);}
            balance.temperature.Override(2);balance.tint.Override(-1);
            var t=root.Cast<Transform>().FirstOrDefault(v=>v.name=="10_ColorOnly_Volume");var go=t?t.gameObject:new GameObject("10_ColorOnly_Volume",typeof(Volume));go.transform.SetParent(root,false);
            var volume=go.GetComponent<Volume>();volume.isGlobal=true;volume.weight=1;volume.priority=100;volume.sharedProfile=profile;
            var camera=GameObject.Find("Camera_Hero").GetComponent<Camera>();var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=1;data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;camera.allowMSAA=true;
            EditorUtility.SetDirty(profile);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        [MenuItem("Tools/EchoStation/Parkour Art/Views/Hero Overview")]
        public static void Hero()=>View(new Vector3(-55,39,-55),new Vector3(-5,13,10),49);
        [MenuItem("Tools/EchoStation/Parkour Art/Views/Runner Departure")]
        public static void Runner()=>View(new Vector3(-22,13.7f,-24),new Vector3(-22,14,1),67);
        [MenuItem("Tools/EchoStation/Parkour Art/Views/Reflective Atrium")]
        public static void Atrium()=>View(new Vector3(-7,17,-4),new Vector3(5,16,8),60);
        static void View(Vector3 position,Vector3 target,float fov)
        {
            SceneSafetyGuard.RequireClean();if(SceneManager.GetActiveScene().path!=VectorCityBuilder.ScenePath)EditorSceneManager.OpenScene(VectorCityBuilder.ScenePath);
            VectorCityPlayerSetup.SetFirstPersonView(false);
            var camera=GameObject.Find("Camera_Hero").GetComponent<Camera>();Undo.RecordObject(camera.transform,"Parkour viewpoint");Undo.RecordObject(camera,"Parkour lens");camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=fov;
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);EditorSceneManager.SaveScene(camera.gameObject.scene);
        }
        [MenuItem("Tools/EchoStation/Parkour Art/Export 1080p Views")]
        public static void ExportViews()
        {
            SceneSafetyGuard.RequireClean();if(SceneManager.GetActiveScene().path!=VectorCityBuilder.ScenePath)throw new InvalidOperationException("Open the parkour scene.");
            Directory.CreateDirectory("Docs/Tests/ParkourArt/Final");var camera=GameObject.Find("Camera_Hero").GetComponent<Camera>();
            var position=camera.transform.position;var rotation=camera.transform.rotation;var fov=camera.fieldOfView;
            try{using(var frames=new ParkourFrameCapture(camera)){
                camera.transform.position=new Vector3(-55,39,-55);camera.transform.LookAt(new Vector3(-5,13,10));camera.fieldOfView=49;frames.Capture("Docs/Tests/ParkourArt/Final/Hero_1080.png");
                camera.transform.position=new Vector3(-22,13.7f,-24);camera.transform.LookAt(new Vector3(-22,14,1));camera.fieldOfView=67;frames.Capture("Docs/Tests/ParkourArt/Final/Runner_1080.png");
                camera.transform.position=new Vector3(-7,17,-4);camera.transform.LookAt(new Vector3(5,16,8));camera.fieldOfView=60;frames.Capture("Docs/Tests/ParkourArt/Final/Atrium_1080.png");
            }}finally{camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;}
        }
    }
}
