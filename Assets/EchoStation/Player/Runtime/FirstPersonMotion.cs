using System;
using UnityEngine;

namespace EchoStation.Player
{
    public static class FirstPersonMotion
    {
        public const float Speed=3,Sensitivity=.1f,PitchLimit=80;
        public static Vector3 HorizontalDelta(Vector2 input,float yaw,float seconds)
        {
            if(!Finite(input)||!float.IsFinite(yaw)||!float.IsFinite(seconds)||seconds<0)throw new ArgumentException("Motion inputs must be finite and time nonnegative.");
            input=Vector2.ClampMagnitude(input,1);return Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y)*(Speed*seconds);
        }
        public static Vector2 LookAngles(Vector2 mouseDelta,Vector2 yawPitch)
        {
            if(!Finite(mouseDelta)||!Finite(yawPitch))throw new ArgumentException("Look values must be finite.");return new Vector2(Mathf.Repeat(yawPitch.x+mouseDelta.x*Sensitivity,360),Mathf.Clamp(yawPitch.y-mouseDelta.y*Sensitivity,-PitchLimit,PitchLimit));
        }
        private static bool Finite(Vector2 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y);
    }
}
