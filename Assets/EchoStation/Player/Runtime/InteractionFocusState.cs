using System;
using UnityEngine;

namespace EchoStation.Player
{
    public sealed class InteractionFocusState
    {
        public const float FadeSeconds=.15f;
        public float Distance{get;private set;}=1;
        public float Weight{get;private set;}
        public bool Active=>Weight>=.001f;
        public void Step(float? targetDistance,float deltaTime)
        {
            if(float.IsNaN(deltaTime)||float.IsInfinity(deltaTime)||deltaTime<0)throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if(targetDistance.HasValue&&(float.IsNaN(targetDistance.Value)||float.IsInfinity(targetDistance.Value)||targetDistance.Value<=0))throw new ArgumentOutOfRangeException(nameof(targetDistance));
            if(targetDistance.HasValue)
            {
                var target=Mathf.Clamp(targetDistance.Value,.35f,2.5f);
                Distance=Active?Mathf.Lerp(Distance,target,1-Mathf.Exp(-10*deltaTime)):target;
            }
            Weight=Mathf.MoveTowards(Weight,targetDistance.HasValue?1:0,deltaTime/FadeSeconds);
        }
        public void Reset(){Weight=0;Distance=1;}
    }
}
