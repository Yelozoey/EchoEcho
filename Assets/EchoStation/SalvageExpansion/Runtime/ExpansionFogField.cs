using System;
using UnityEngine;
namespace EchoStation.SalvageExpansion
{
    public sealed class ExpansionFogField:MonoBehaviour
    {
        [SerializeField]EnvironmentHazards hazards;[SerializeField]ExpansionGraphics graphics;[SerializeField]Light[] sources;
        readonly Plane[] planes=new Plane[6];internal readonly Vector4[] Minima=new Vector4[3],Maxima=new Vector4[3],LightPositions=new Vector4[4],LightColors=new Vector4[4],LightDirections=new Vector4[4];
        public int LastWidth{get;internal set;}public int LastHeight{get;internal set;}public int LastRecordedFrame{get;internal set;}=-1;HazardZone steam;
        public void Configure(EnvironmentHazards value,ExpansionGraphics settings,Light[] lights){hazards=value;graphics=settings;sources=lights;if(lights==null||lights.Length!=4)throw new ArgumentException("Four local lights required.");}
        HazardZone Steam{get{if(!steam&&hazards)foreach(var zone in hazards.Zones)if(zone.Kind==EnvironmentHazardKind.Steam)steam=zone;return steam;}}
        public bool ShouldRender(Camera camera){if(!camera||!hazards||!Steam||!graphics||graphics.Quality==EchoStation.SalvageNext.RecoveryQuality.Low||hazards.Signal(Steam)==HazardSignal.Safe)return false;GeometryUtility.CalculateFrustumPlanes(camera,planes);return Steam.Volume.bounds.SqrDistance(camera.transform.position)<100&&GeometryUtility.TestPlanesAABB(planes,Steam.Volume.bounds);}
        internal Vector4 Pack()
        {
            Minima[0]=Steam.Volume.bounds.min;Maxima[0]=Steam.Volume.bounds.max;for(int i=1;i<3;i++){Minima[i]=new Vector4(0,-1000,0,0);Maxima[i]=new Vector4(0,-1000,0,0);}for(int i=0;i<4;i++){var light=sources[i];if(!light||!light.isActiveAndEnabled){LightPositions[i]=LightColors[i]=LightDirections[i]=Vector4.zero;continue;}var p=light.transform.position;var d=light.transform.forward;var color=light.color.linear*light.intensity;LightPositions[i]=new Vector4(p.x,p.y,p.z,light.range);LightColors[i]=new Vector4(color.r,color.g,color.b,light.type==LightType.Spot?Mathf.Cos(light.spotAngle*.5f*Mathf.Deg2Rad):-2);LightDirections[i]=new Vector4(d.x,d.y,d.z,Mathf.Cos(light.innerSpotAngle*.5f*Mathf.Deg2Rad));}return new Vector4(hazards.Signal(Steam)==HazardSignal.Active?.10f:.025f,0,0,1);
        }
    }
}
