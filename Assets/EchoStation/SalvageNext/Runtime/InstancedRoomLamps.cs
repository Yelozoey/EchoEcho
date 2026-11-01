using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace EchoStation.SalvageNext
{
    public sealed class InstancedRoomLamps:MonoBehaviour
    {
        [SerializeField]Camera view;
        [SerializeField]Mesh mesh;
        [SerializeField]Material[] materials=Array.Empty<Material>();
        [SerializeField]Vector3[] centers=Array.Empty<Vector3>();
        readonly Matrix4x4[] matrices=new Matrix4x4[6];readonly Plane[] planes=new Plane[6];
        public Camera View=>view;public Mesh SharedMesh=>mesh;public int InstanceCount=>centers.Length;
        public Material MaterialAt(int index)=>materials[index];
        public int LastInstances{get;private set;}public int LastBatches{get;private set;}
        public void Configure(Mesh lamp,Material[] values,Vector3[] positions)
        {
            if(!lamp||values==null||values.Length!=lamp.subMeshCount||Array.Exists(values,m=>!m||!m.enableInstancing)||positions==null||positions.Length!=6||Array.Exists(positions,p=>!float.IsFinite(p.x)||!float.IsFinite(p.y)||!float.IsFinite(p.z)))throw new ArgumentException("Six finite lamps with instancing materials required.");mesh=lamp;materials=(Material[])values.Clone();centers=(Vector3[])positions.Clone();
        }
        public void BindView(Camera camera){view=camera;}
        public int Evaluate(Camera camera)
        {
            LastInstances=0;if(!isActiveAndEnabled||!camera||!mesh)return 0;GeometryUtility.CalculateFrustumPlanes(camera,planes);foreach(var center in centers){if((center-camera.transform.position).sqrMagnitude>36*36||!GeometryUtility.TestPlanesAABB(planes,new Bounds(center,mesh.bounds.size)))continue;matrices[LastInstances++]=Matrix4x4.TRS(center,Quaternion.identity,Vector3.one);}return LastInstances;
        }
        public void Submit(Camera camera)
        {
            LastBatches=0;if(Evaluate(camera)==0)return;for(int i=0;i<materials.Length;i++){var parameters=new RenderParams(materials[i]){camera=camera,layer=gameObject.layer,shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false,lightProbeUsage=LightProbeUsage.BlendProbes};Graphics.RenderMeshInstanced(parameters,mesh,i,matrices,LastInstances);LastBatches++;}
        }
        void LateUpdate(){if(view)Submit(view);}
        void OnDisable(){LastInstances=LastBatches=0;}
    }
}
