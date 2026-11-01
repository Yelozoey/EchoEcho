using System;
using EchoStation.Player;
using EchoStation.Rendering;
using UnityEngine;
namespace EchoStation.SalvageNext
{
    [RequireComponent(typeof(ParticleEffectPool))]public sealed class NextFaultSparks:MonoBehaviour
    {
        [SerializeField]ParticleEffectPool pool;
        [SerializeField]Camera view;
        [SerializeField]PlayerInputReader input;
        readonly Plane[] planes=new Plane[6];float next;
        public bool Automatic{get;set;}=true;
        public ParticleEffectPool Pool=>pool;
        public Vector3 Position(int index){if(index<0||index>1)throw new ArgumentOutOfRangeException();return index==0?new Vector3(22,1.5f,18.1f):new Vector3(34,1.5f,12.1f);}
        public void Configure(ParticleEffectPool owner,Camera camera,PlayerInputReader reader){if(!owner||!camera||!reader)throw new ArgumentException();pool=owner;view=camera;input=reader;}
        public bool TryBurst(int index,out ParticleLease lease)
        {
            lease=default;var position=Position(index);if(!isActiveAndEnabled||!pool||!pool.isActiveAndEnabled||!view||!input||!input.CanPlay||Time.timeScale==0||(view.transform.position-position).sqrMagnitude>144)return false;GeometryUtility.CalculateFrustumPlanes(view,planes);if(!GeometryUtility.TestPlanesAABB(planes,new Bounds(position,Vector3.one)))return false;return pool.TryPlay(position,Quaternion.Euler(-90,0,0),out lease);
        }
        void OnEnable(){if(pool)pool.enabled=true;next=Time.unscaledTime+3;}
        void OnDisable(){if(pool)pool.enabled=false;}
        void Update(){if(!Automatic||Time.unscaledTime<next)return;next=Time.unscaledTime+3;TryBurst(0,out _);TryBurst(1,out _);}
    }
}
