using System;
using UnityEngine;

namespace EchoStation.Rendering
{
    public static class ParticleBudget
    {
        public const int MaximumConcurrentSystems=4,MaximumParticlesPerSystem=64;
        public static int ActiveSystems{get;private set;}
        internal static bool Reserve(){if(ActiveSystems>=MaximumConcurrentSystems)return false;ActiveSystems++;return true;}
        internal static void Release(){if(ActiveSystems<=0)throw new InvalidOperationException("Particle budget underflow.");ActiveSystems--;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void Reset()=>ActiveSystems=0;
    }
    public readonly struct ParticleLease
    {
        internal readonly ParticleEffectPool owner;internal readonly int slot;internal readonly uint generation;
        internal ParticleLease(ParticleEffectPool owner,int slot,uint generation){this.owner=owner;this.slot=slot;this.generation=generation;}
        public bool Stop()=>owner&&owner.Release(this);
    }
    [DisallowMultipleComponent]
    public sealed class ParticleEffectPool:MonoBehaviour
    {
        [SerializeField]private ParticleSystem source;
        [SerializeField,Range(1,4)]private int capacity=4;
        private sealed class Slot{public ParticleSystem system;public bool active;public uint generation;}
        private Slot[] slots;
        public int Capacity=>slots?.Length??0;
        public int ActiveCount{get;private set;}
        public int LiveParticleCount{get{var count=0;if(slots!=null)foreach(var slot in slots)if(slot.active&&slot.system)count+=slot.system.particleCount;return count;}}
        private void Awake(){if(source)Initialize(source,capacity);}
        public static void Validate(ParticleSystem prefab,int count)
        {
            if(!prefab||count<1||count>ParticleBudget.MaximumConcurrentSystems)throw new ArgumentException("A particle source and capacity 1..4 are required.");
            var main=prefab.main;
            if(prefab.GetComponentsInChildren<ParticleSystem>(true).Length!=1||main.loop||main.playOnAwake||main.stopAction!=ParticleSystemStopAction.None||main.maxParticles>ParticleBudget.MaximumParticlesPerSystem||main.maxParticles<1||prefab.subEmitters.enabled||prefab.collision.enabled||prefab.trigger.enabled)
                throw new ArgumentException("Pool requires one nonlooping, manually played system with 1..64 particles, no subemitters/collision/triggers and no automatic stop action.");
        }
        public void Initialize(ParticleSystem prefab,int count)
        {
            if(slots!=null)throw new InvalidOperationException("Pool is already initialized.");Validate(prefab,count);source=prefab;capacity=count;slots=new Slot[count];
            for(var i=0;i<count;i++){var copy=Instantiate(source,transform);copy.name=source.name+"_Pooled_"+i;copy.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);copy.gameObject.SetActive(false);slots[i]=new Slot{system=copy};}
        }
        public bool TryPlay(Vector3 position,Quaternion rotation,out ParticleLease lease)
        {
            lease=default;if(!isActiveAndEnabled||slots==null||!Finite(position)||!Finite(rotation))return false;
            for(var i=0;i<slots.Length;i++)
            {
                var slot=slots[i];if(slot.active)continue;if(!ParticleBudget.Reserve())return false;slot.active=true;slot.generation++;ActiveCount++;
                slot.system.transform.SetPositionAndRotation(position,rotation);slot.system.gameObject.SetActive(true);slot.system.Play(false);lease=new ParticleLease(this,i,slot.generation);return true;
            }
            return false;
        }
        internal bool Release(ParticleLease lease)
        {
            if(lease.owner!=this||slots==null||lease.slot<0||lease.slot>=slots.Length)return false;var slot=slots[lease.slot];if(!slot.active||slot.generation!=lease.generation)return false;Return(slot);return true;
        }
        private void Return(Slot slot){slot.system.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);slot.system.gameObject.SetActive(false);slot.active=false;ActiveCount--;ParticleBudget.Release();}
        private void Update(){if(slots==null)return;foreach(var slot in slots)if(slot.active&&!slot.system.IsAlive(false))Return(slot);}
        private void OnDisable(){if(slots!=null)foreach(var slot in slots)if(slot.active)Return(slot);}
        private void OnDestroy(){if(slots!=null)foreach(var slot in slots)if(slot.system){if(Application.isPlaying)Destroy(slot.system.gameObject);else DestroyImmediate(slot.system.gameObject);}}
        private static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
        private static bool Finite(Quaternion q)=>float.IsFinite(q.x)&&float.IsFinite(q.y)&&float.IsFinite(q.z)&&float.IsFinite(q.w)&&q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w>.0001f;
    }
}
