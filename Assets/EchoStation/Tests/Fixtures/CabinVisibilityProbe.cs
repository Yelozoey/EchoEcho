using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace EchoStation.Tests.Fixtures
{
    public sealed class CabinVisibilityProbe : IDisposable
    {
        public Camera Camera{get;}public SceneFrameCapture Frames{get;}
        private readonly UniversalAdditionalCameraData data;private readonly ScriptableRendererFeature[] features;private readonly bool[] states;
        private readonly Light fault;private readonly Vector3 position;private readonly Quaternion rotation;
        public CabinVisibilityProbe(Scene scene,UniversalRendererData renderer)
        {
            var roots=scene.GetRootGameObjects();Camera=roots.Single(o=>o.name=="Cabin_ValidationCamera").GetComponent<Camera>();Camera.scene=scene;Camera.aspect=16f/9f;
            position=Camera.transform.position;rotation=Camera.transform.rotation;data=Camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;
            features=renderer.rendererFeatures.Where(f=>f&&f.GetType().Name=="ScreenSpaceAmbientOcclusion").ToArray();states=features.Select(f=>f.isActive).ToArray();foreach(var feature in features)feature.SetActive(false);
            fault=roots.Single(o=>o.name=="Cabin_Fault_Realtime").GetComponent<Light>();fault.enabled=false;Frames=new SceneFrameCapture(Camera);
        }
        public void HiddenRoomView(bool occlusion){Camera.transform.SetPositionAndRotation(new Vector3(0,1.6f,-5.5f),Quaternion.Euler(0,90,0));Camera.useOcclusionCulling=occlusion;}
        public void InsideStorageView(){Camera.transform.position=new Vector3(5.7f,1.65f,-1.6f);Camera.transform.LookAt(new Vector3(8,1.1f,2.8f));Camera.useOcclusionCulling=true;}
        public void Dispose()
        {
            Frames.Dispose();fault.enabled=true;data.renderPostProcessing=true;Camera.transform.SetPositionAndRotation(position,rotation);Camera.useOcclusionCulling=true;
            for(var i=0;i<features.Length;i++)if(features[i])features[i].SetActive(states[i]);
        }
    }
}
