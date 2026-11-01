using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoStation.InfiltrationCoated
{
    [DisallowMultipleComponent,RequireComponent(typeof(Renderer))]
    public sealed class CoatPlanar : MonoBehaviour
    {
        public Camera sourceCamera;
        public Vector3 localPlaneNormal=Vector3.up;
        public Vector3 localPlanePoint=Vector3.zero;
        [Range(256,2048)] public int textureWidth=1024;
        [Range(.005f,.1f)] public float clipOffset=.025f;
        public int RenderCount { get; private set; }
        public RenderTexture LastTexture { get; private set; }
        public Matrix4x4 LastViewProjection { get; private set; }
        static readonly List<CoatPlanar> active=new List<CoatPlanar>();
        static bool rendering;
        sealed class State {public Camera camera;public RenderTexture texture;public Matrix4x4 vp;public Vector3 normal;public bool valid;}
        readonly Dictionary<Camera,State> states=new Dictionary<Camera,State>();
        Renderer surface;MaterialPropertyBlock original,properties;
        void OnEnable()
        {
            surface=GetComponent<Renderer>();original=new MaterialPropertyBlock();surface.GetPropertyBlock(original);properties=new MaterialPropertyBlock();
            if(!active.Contains(this))active.Add(this);RenderPipelineManager.beginContextRendering+=PrepareContext;RenderPipelineManager.beginCameraRendering+=BeforeCamera;
        }
        void PrepareContext(ScriptableRenderContext context,List<Camera> cameras)
        {
            if(rendering)return;
            foreach(var source in cameras)PrepareCamera(source);
        }
        void PrepareCamera(Camera source)
        {
            if(!Application.isPlaying||source!=sourceCamera||rendering||!source||source.cameraType==CameraType.Reflection||source.cameraType==CameraType.Preview||!surface||!surface.enabled)return;
            var point=transform.TransformPoint(localPlanePoint);var normal=transform.TransformDirection(localPlaneNormal).normalized;
            if(Vector3.Dot(source.transform.position-point,normal)<=.04f)return;
            if(!GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(source),surface.bounds))return;
            RenderReflection(source,point,normal);
        }
        void RenderReflection(Camera source,Vector3 point,Vector3 normal)
        {
            int width=Mathf.Clamp(textureWidth,256,2048),height=Mathf.Max(128,Mathf.RoundToInt(width/source.aspect));
            if(!states.TryGetValue(source,out var state))
            {
                var go=new GameObject("CF_PlanarCamera_"+name+"_"+source.name){hideFlags=HideFlags.HideAndDontSave};
                var camera=go.AddComponent<Camera>();camera.enabled=false;camera.cameraType=CameraType.Reflection;
                state=new State{camera=camera};states.Add(source,state);
            }
            if(!state.texture||state.texture.width!=width||state.texture.height!=height)
            {
                if(state.texture){state.texture.Release();Release(state.texture);}
                state.texture=new RenderTexture(width,height,24,RenderTextureFormat.ARGBHalf,RenderTextureReadWrite.Linear){name="CF_PlanarHDR_"+name+"_"+source.name,hideFlags=HideFlags.HideAndDontSave,useMipMap=true,autoGenerateMips=false,filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};state.texture.Create();
            }
            var reflected=state.camera;reflected.CopyFrom(source);reflected.enabled=false;reflected.cameraType=CameraType.Reflection;
            reflected.allowMSAA=false;reflected.allowHDR=true;reflected.useOcclusionCulling=false;reflected.targetTexture=state.texture;
            var data=reflected.GetUniversalAdditionalCameraData();data.SetRenderer(1);data.renderPostProcessing=false;data.allowHDROutput=false;data.renderShadows=true;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
            var mirror=ReflectionMatrix(new Vector4(normal.x,normal.y,normal.z,-Vector3.Dot(normal,point)));
            reflected.transform.position=mirror.MultiplyPoint(source.transform.position);
            reflected.transform.rotation=Quaternion.LookRotation(Vector3.Reflect(source.transform.forward,normal),Vector3.Reflect(source.transform.up,normal));
            reflected.worldToCameraMatrix=source.worldToCameraMatrix*mirror;
            reflected.projectionMatrix=source.projectionMatrix;
            var cameraPoint=reflected.worldToCameraMatrix.MultiplyPoint(point+normal*clipOffset);
            var cameraNormal=reflected.worldToCameraMatrix.MultiplyVector(normal).normalized;
            reflected.projectionMatrix=reflected.CalculateObliqueMatrix(new Vector4(cameraNormal.x,cameraNormal.y,cameraNormal.z,-Vector3.Dot(cameraPoint,cameraNormal)));
            reflected.cullingMatrix=reflected.projectionMatrix*reflected.worldToCameraMatrix;
            var disabled=new List<KeyValuePair<Renderer,bool>>();bool inverted=GL.invertCulling;
            rendering=true;
            try
            {
                foreach(var item in active)if(item&&item.surface){disabled.Add(new KeyValuePair<Renderer,bool>(item.surface,item.surface.enabled));item.surface.enabled=false;}
                GL.invertCulling=!inverted;
                RenderPipeline.SubmitRenderRequest(reflected,new UniversalRenderPipeline.SingleCameraRequest{destination=state.texture});
                state.texture.GenerateMips();
                LastViewProjection=GL.GetGPUProjectionMatrix(reflected.projectionMatrix,true)*reflected.worldToCameraMatrix;
                state.vp=LastViewProjection;state.normal=normal;state.valid=true;
                LastTexture=state.texture;RenderCount++;
            }
            finally
            {
                GL.invertCulling=inverted;foreach(var pair in disabled)if(pair.Key)pair.Key.enabled=pair.Value;rendering=false;
            }
        }
        // Rendering nested requests in beginCameraRendering invalidates URP 17.6 camera-stack frame data.
        // Prepare at beginContextRendering; this callback only binds the already rendered source-specific state.
        void BeforeCamera(ScriptableRenderContext context,Camera source)
        {
            if(rendering||!surface||!states.TryGetValue(source,out var state)||!state.valid)return;
            surface.GetPropertyBlock(properties);properties.SetTexture("_PlanarTexture",state.texture);properties.SetMatrix("_PlanarVP",state.vp);properties.SetVector("_PlanarNormal",new Vector4(state.normal.x,state.normal.y,state.normal.z,0));properties.SetFloat("_PlanarValid",1);surface.SetPropertyBlock(properties);
        }
        public static Matrix4x4 ReflectionMatrix(Vector4 p)
        {
            var m=Matrix4x4.identity;
            m.m00=1-2*p.x*p.x;m.m01=-2*p.x*p.y;m.m02=-2*p.x*p.z;m.m03=-2*p.w*p.x;
            m.m10=-2*p.y*p.x;m.m11=1-2*p.y*p.y;m.m12=-2*p.y*p.z;m.m13=-2*p.w*p.y;
            m.m20=-2*p.z*p.x;m.m21=-2*p.z*p.y;m.m22=1-2*p.z*p.z;m.m23=-2*p.w*p.z;return m;
        }
        static void Release(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        void OnDisable()
        {
            RenderPipelineManager.beginContextRendering-=PrepareContext;RenderPipelineManager.beginCameraRendering-=BeforeCamera;active.Remove(this);
            if(surface)surface.SetPropertyBlock(original!=null&&!original.isEmpty?original:null);
            foreach(var state in states.Values){if(state.camera){state.camera.targetTexture=null;Release(state.camera.gameObject);}if(state.texture){state.texture.Release();Release(state.texture);}}
            states.Clear();LastTexture=null;
        }
    }
}
