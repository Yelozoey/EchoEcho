using System;
using UnityEngine;
namespace EchoStation.SalvageNext
{
    public sealed class NextFogField:MonoBehaviour
    {
        public const int Steps=16;
        [SerializeField]NextSession session;
        [SerializeField]Light[] sources=Array.Empty<Light>();
        readonly Plane[] planes=new Plane[6];
        internal readonly Vector4[] Minima=new Vector4[3],Maxima=new Vector4[3],LightPositions=new Vector4[4],LightColors=new Vector4[4],LightDirections=new Vector4[4];
        float densityScale=1,lightStrength=1;
        public int LastWidth{get;internal set;}public int LastHeight{get;internal set;}public int LastRecordedFrame{get;internal set;}=-1;
        public float DensityScale{get=>densityScale;set{if(!float.IsFinite(value)||value<0||value>2)throw new ArgumentOutOfRangeException();densityScale=value;}}
        public float LightStrength{get=>lightStrength;set{if(!float.IsFinite(value)||value<0||value>4)throw new ArgumentOutOfRangeException();lightStrength=value;}}
        public Bounds Box(int index){if(index<0||index>2)throw new ArgumentOutOfRangeException();return new Bounds(index==0?new Vector3(12,1.3f,6):index==1?new Vector3(12,1.3f,18):new Vector3(24,1.3f,12),new Vector3(3,2.6f,3));}
        public void Configure(NextSession owner,Light[] lights){if(!owner||lights==null||lights.Length!=4||Array.Exists(lights,l=>!l||l.type==LightType.Directional))throw new ArgumentException("Session and four local lights required.");session=owner;sources=(Light[])lights.Clone();}
        public bool ShouldRender(Camera camera)
        {
            if(!isActiveAndEnabled||!session||session.Quality==RecoveryQuality.Low||densityScale==0||!camera)return false;GeometryUtility.CalculateFrustumPlanes(camera,planes);
            for(int i=0;i<3;i++){var box=Box(i);if(box.SqrDistance(camera.transform.position)<=64&&GeometryUtility.TestPlanesAABB(planes,box))return true;}return false;
        }
        internal Vector4 Pack()
        {
            for(int i=0;i<3;i++){var box=Box(i);Minima[i]=box.min;Maxima[i]=box.max;}
            for(int i=0;i<4;i++)
            {
                var light=sources[i];if(!light||!light.isActiveAndEnabled){LightPositions[i]=Vector4.zero;LightColors[i]=Vector4.zero;LightDirections[i]=Vector4.zero;continue;}var p=light.transform.position;var direction=light.transform.forward;var color=light.color.linear*light.intensity;LightPositions[i]=new Vector4(p.x,p.y,p.z,light.range);LightColors[i]=new Vector4(color.r,color.g,color.b,light.type==LightType.Spot?Mathf.Cos(light.spotAngle*.5f*Mathf.Deg2Rad):-2);LightDirections[i]=new Vector4(direction.x,direction.y,direction.z,Mathf.Cos(light.innerSpotAngle*.5f*Mathf.Deg2Rad));
            }
            return new Vector4(.06f*densityScale,.08f*densityScale,.06f*densityScale,lightStrength);
        }
    }
}
