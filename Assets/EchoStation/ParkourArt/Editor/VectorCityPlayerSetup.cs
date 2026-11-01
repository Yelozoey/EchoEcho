using System;
using System.Linq;
using EchoStation.Player;
using EchoStation.Rendering.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.ParkourArt.Editor
{
    public static class VectorCityPlayerSetup
    {
        public static void SetFirstPersonView(bool firstPerson)
        {
            var player=UnityEngine.Object.FindAnyObjectByType<VectorCityFirstPerson>();
            if(!player)return;
            player.enabled=firstPerson;player.GetComponent<PlayerInputReader>().enabled=firstPerson;player.View.enabled=firstPerson;
            var hero=GameObject.Find("Camera_Hero").GetComponent<Camera>();hero.enabled=!firstPerson;
            hero.tag=firstPerson?"Untagged":"MainCamera";player.View.tag=firstPerson?"MainCamera":"Untagged";
        }
        [MenuItem("Tools/EchoStation/Parkour Art/Views/First Person Player")]
        public static void UsePlayerView()
        {
            SceneSafetyGuard.RequireClean();if(SceneManager.GetActiveScene().path!=VectorCityBuilder.ScenePath)EditorSceneManager.OpenScene(VectorCityBuilder.ScenePath);
            SetFirstPersonView(true);var scene=SceneManager.GetActiveScene();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        [MenuItem("Tools/EchoStation/Parkour Art/06 Add First Person Player")]
        public static void Configure()
        {
            SceneSafetyGuard.RequireClean();var scene=SceneManager.GetActiveScene();
            if(scene.path!=VectorCityBuilder.ScenePath)throw new InvalidOperationException("Open the owned parkour scene first.");
            var root=scene.GetRootGameObjects().Single(g=>g.hideFlags==HideFlags.None).transform;
            var actor=root.Find("11_Player_FirstPerson");
            if(!actor){actor=new GameObject("11_Player_FirstPerson").transform;actor.SetParent(root,false);}
            var cc=actor.GetComponent<CharacterController>();if(!cc)cc=actor.gameObject.AddComponent<CharacterController>();
            cc.height=1.75f;cc.radius=.3f;cc.center=new Vector3(0,.875f,0);cc.skinWidth=.03f;cc.stepOffset=.25f;cc.slopeLimit=45;cc.minMoveDistance=0;
            if(!actor.GetComponent<PlayerInputReader>())actor.gameObject.AddComponent<PlayerInputReader>();
            var child=actor.Find("Camera_FirstPerson");if(!child){child=new GameObject("Camera_FirstPerson",typeof(Camera),typeof(AudioListener)).transform;child.SetParent(actor,false);}
            child.localPosition=new Vector3(0,1.6f,0);child.localRotation=Quaternion.identity;child.tag="MainCamera";
            var camera=child.GetComponent<Camera>();camera.enabled=true;camera.fieldOfView=70;camera.nearClipPlane=.05f;camera.farClipPlane=280;camera.allowHDR=true;camera.allowMSAA=true;camera.clearFlags=CameraClearFlags.Skybox;
            var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(0);data.allowHDROutput=false;data.renderPostProcessing=true;data.volumeLayerMask=1;data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
            var motor=actor.GetComponent<VectorCityFirstPerson>();if(!motor)motor=actor.gameObject.AddComponent<VectorCityFirstPerson>();
            Physics.SyncTransforms();
            var ground=Physics.RaycastAll(new Vector3(-22,40,-25.5f),Vector3.down,60)
                .Where(h=>h.collider.gameObject.scene==scene&&!h.collider.transform.IsChildOf(actor)).OrderByDescending(h=>h.point.y).ToArray();
            if(ground.Length==0)throw new InvalidOperationException("No safe starting roof found.");
            var spawn=new Vector3(-22,ground[0].point.y+.1f,-25.5f);motor.Configure(camera,spawn);actor.position=spawn;actor.rotation=Quaternion.identity;
            var hero=GameObject.Find("Camera_Hero").GetComponent<Camera>();hero.enabled=false;hero.tag="Untagged";
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=actor.gameObject;
        }
    }
}
