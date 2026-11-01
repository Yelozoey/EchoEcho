using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoStation.SalvageNext
{
    public sealed class InstancedRoomDecor:MonoBehaviour
    {
        [SerializeField]Camera view;
        [SerializeField]Mesh[] meshes=Array.Empty<Mesh>();
        [SerializeField]Material solid,impostor,strap;
        [SerializeField]Mesh strip;
        [SerializeField]Vector3[] centers=Array.Empty<Vector3>(),strips=Array.Empty<Vector3>();
        readonly Matrix4x4[][] matrices={new Matrix4x4[64],new Matrix4x4[64],new Matrix4x4[64],new Matrix4x4[64],new Matrix4x4[64]};
        readonly int[] counts=new int[5];readonly Plane[] planes=new Plane[6];
        public int LastBatches{get;private set;}public int LastInstances{get;private set;}public int SubmissionFrame{get;private set;}=-1;
        public int InstanceCount=>centers.Length;
        public Camera View=>view;
        public void BindView(Camera camera){view=camera;}
        public int CountAt(int lod)=>counts[lod];
        public static int LodAt(float distance)=>distance<8?0:distance<16?1:distance<24?2:distance<48?3:-1;
        public void Configure(Camera camera,Mesh[] levels,Material near,Material far,Mesh soft,Material fabric,Vector3[] positions,Vector3[] softPositions)
        {if(!camera||levels==null||levels.Length!=4||Array.Exists(levels,m=>!m)||!near||!far||!soft||!fabric||positions==null||positions.Length>64||softPositions==null||softPositions.Length>64)throw new ArgumentException("Invalid decor data.");view=camera;meshes=(Mesh[])levels.Clone();solid=near;impostor=far;strip=soft;strap=fabric;centers=(Vector3[])positions.Clone();strips=(Vector3[])softPositions.Clone();}
        public void Submit(Camera camera)
        {
            Array.Clear(counts,0,counts.Length);LastBatches=LastInstances=0;if(!isActiveAndEnabled||!camera||meshes.Length!=4)return;GeometryUtility.CalculateFrustumPlanes(camera,planes);
            foreach(var center in centers){float distance=Vector3.Distance(camera.transform.position,center);int lod=LodAt(distance);if(lod<0||!GeometryUtility.TestPlanesAABB(planes,new Bounds(center,new Vector3(.8f,.8f,.8f))))continue;var rotation=lod==3?Quaternion.Euler(0,Mathf.Atan2(camera.transform.position.x-center.x,camera.transform.position.z-center.z)*Mathf.Rad2Deg,0):Quaternion.identity;matrices[lod][counts[lod]++]=Matrix4x4.TRS(center,rotation,Vector3.one*.75f);}
            foreach(var center in strips)if((center-camera.transform.position).sqrMagnitude<24*24&&GeometryUtility.TestPlanesAABB(planes,new Bounds(center,new Vector3(.2f,.8f,.1f))))matrices[4][counts[4]++]=Matrix4x4.TRS(center,Quaternion.identity,new Vector3(.12f,.75f,1));
            for(int i=0;i<5;i++)if(counts[i]>0){var material=i<2?solid:i<4?impostor:strap;var parameters=new RenderParams(material){camera=camera,layer=gameObject.layer,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=true,lightProbeUsage=LightProbeUsage.BlendProbes};Graphics.RenderMeshInstanced(parameters,i<4?meshes[i]:strip,0,matrices[i],counts[i]);LastBatches++;LastInstances+=counts[i];}SubmissionFrame=Time.frameCount;
        }
        void LateUpdate(){if(view)Submit(view);}
        void OnDisable(){Array.Clear(counts,0,counts.Length);LastBatches=LastInstances=0;}
    }
}
