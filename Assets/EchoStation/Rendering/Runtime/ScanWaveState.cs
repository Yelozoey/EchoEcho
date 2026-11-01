using System;
using UnityEngine;

namespace EchoStation.Rendering
{
    public sealed class ScanWaveState
    {
        public const float Duration=1.2f,MaximumRadius=6,Width=.18f,Intensity=.35f;
        private float elapsed;
        public bool Active{get;private set;}
        public Vector3 Origin{get;private set;}
        public float Radius=>Mathf.Clamp01(elapsed/Duration)*MaximumRadius;
        public uint Generation{get;private set;}
        public void Begin(Vector3 origin)
        {
            if(!float.IsFinite(origin.x)||!float.IsFinite(origin.y)||!float.IsFinite(origin.z))throw new ArgumentException("Wave origin must be finite.");
            Origin=origin;elapsed=0;Active=true;Generation++;
        }
        public void Tick(float delta)
        {
            if(!float.IsFinite(delta)||delta<0)throw new ArgumentException("Wave delta must be finite and nonnegative.");
            if(!Active)return;elapsed+=delta;if(elapsed>=Duration)Active=false;
        }
        public void Cancel()=>Active=false;
    }
}
