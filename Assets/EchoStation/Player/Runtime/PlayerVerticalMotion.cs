using System;
using UnityEngine;

namespace EchoStation.Player
{
    public sealed class PlayerVerticalMotion
    {
        public const float Gravity=18,JumpHeight=.85f,TerminalSpeed=30,GroundSpeed=2;
        public static float JumpSpeed=>Mathf.Sqrt(2*Gravity*JumpHeight);
        public float Velocity{get;private set;}
        public float Step(bool grounded,bool jump,float seconds)
        {
            if(!float.IsFinite(seconds)||seconds<0)throw new ArgumentException("Vertical time must be finite and nonnegative.");
            if(grounded&&Velocity<=0)Velocity=jump?JumpSpeed:-GroundSpeed;
            Velocity=Mathf.Max(Velocity-Gravity*seconds,-TerminalSpeed);return Velocity*seconds;
        }
        public void ResolveCollision(CollisionFlags flags)
        {
            if((flags&CollisionFlags.Above)!=0&&Velocity>0)Velocity=0;
            if((flags&CollisionFlags.Below)!=0&&Velocity<0)Velocity=-GroundSpeed;
        }
        public void Reset()=>Velocity=0;
    }
}
