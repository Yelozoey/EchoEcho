using System;
using UnityEngine;

namespace EchoStation.InfiltrationLevel
{
    public sealed class LevelFogField:MonoBehaviour
    {
        public const int Steps=16;
        [SerializeField] Bounds[] boxes={new Bounds(new Vector3(0,2.75f,-2),new Vector3(10,5.4f,10)),new Bounds(new Vector3(12,1.6f,8),new Vector3(5,3.2f,8)),new Bounds(new Vector3(-12,1.6f,8),new Vector3(5,3.2f,8))};
        [SerializeField] Vector3 densities=new Vector3(.018f,.008f,.008f);
        [SerializeField,Range(0,2)] float densityScale=1;
        readonly Plane[] planes=new Plane[6];
        internal readonly Vector4[] Minima=new Vector4[3],Maxima=new Vector4[3];
        public float DensityScale{get=>densityScale;set{if(!float.IsFinite(value)||value<0||value>2)throw new ArgumentOutOfRangeException();densityScale=value;}}
        // Diagnostic switches never saved by tests; production defaults use shadows and the scene color.
        public bool ScatterOnly{get;set;}
        public bool SampleShadows{get;set;}=true;
        public int LastWidth{get;internal set;}public int LastHeight{get;internal set;}
        public int LastRecordedFrame{get;internal set;}=-1;
        public bool LastShadowDependency{get;internal set;}
        public Bounds Box(int i){if(i<0||i>=3)throw new ArgumentOutOfRangeException();return boxes[i];}
        public bool ShouldRender(Camera camera)
        {
            if(!isActiveAndEnabled||densityScale==0||!camera)return false;
            GeometryUtility.CalculateFrustumPlanes(camera,planes);
            foreach(var box in boxes)if(box.SqrDistance(camera.transform.position)<=400&&GeometryUtility.TestPlanesAABB(planes,box))return true;
            return false;
        }
        internal Vector4 Pack()
        {
            for(int i=0;i<3;i++){Minima[i]=boxes[i].min;Maxima[i]=boxes[i].max;}
            return new Vector4(densities.x*densityScale,densities.y*densityScale,densities.z*densityScale,.55f);
        }
    }
}
