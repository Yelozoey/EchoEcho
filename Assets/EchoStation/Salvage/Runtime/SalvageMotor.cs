using System;
using EchoStation.Player;
using UnityEngine;

namespace EchoStation.Salvage
{
    [RequireComponent(typeof(CharacterController),typeof(PlayerInputReader))]
    public sealed class SalvageMotor:MonoBehaviour
    {
        [SerializeField]Transform view;
        CharacterController controller;PlayerInputReader input;Vector2 angles;bool cached;
        readonly PlayerVerticalMotion vertical=new PlayerVerticalMotion();
        float speed=3.5f;
        public float Speed{get=>speed;set{if(!float.IsFinite(value)||value<=0||value>3.5f)throw new ArgumentOutOfRangeException();speed=value;}}
        public Transform View=>view;
        public Vector3 DesiredHorizontalVelocity{get;private set;}
        public bool Grounded=>GetComponent<CharacterController>().isGrounded;
        public void Configure(Transform value){if(!value||!value.IsChildOf(transform))throw new ArgumentException("View must belong to player.");view=value;cached=false;}
        void Cache(){if(cached)return;if(!view)throw new InvalidOperationException("Missing salvage view.");controller=GetComponent<CharacterController>();input=GetComponent<PlayerInputReader>();angles=new Vector2(transform.eulerAngles.y,Mathf.DeltaAngle(0,view.localEulerAngles.x));cached=true;}
        void Awake()=>Cache();
        void Update(){Cache();ApplyInput(input.Move,input.Look,Mathf.Min(Time.deltaTime,.1f),input.JumpPressed);}
        public void ApplyInput(Vector2 move,Vector2 look,float seconds,bool jump=false)
        {
            Cache();angles=FirstPersonMotion.LookAngles(look,angles);transform.rotation=Quaternion.Euler(0,angles.x,0);view.localRotation=Quaternion.Euler(angles.y,0,0);
            var delta=FirstPersonMotion.HorizontalDelta(move,angles.x,seconds)*(speed/FirstPersonMotion.Speed);DesiredHorizontalVelocity=seconds>0?delta/seconds:Vector3.zero;delta.y=vertical.Step(controller.isGrounded,jump,seconds);vertical.ResolveCollision(controller.Move(delta));
        }
        public void RestorePose(Vector3 position,float yaw,float pitch)
        {
            if(!float.IsFinite(position.x)||!float.IsFinite(position.y)||!float.IsFinite(position.z)||!float.IsFinite(yaw)||!float.IsFinite(pitch))throw new ArgumentException("Nonfinite pose.");
            Cache();controller.enabled=false;transform.position=position;angles=new Vector2(Mathf.Repeat(yaw,360),Mathf.Clamp(pitch,-80,80));transform.rotation=Quaternion.Euler(0,angles.x,0);view.localRotation=Quaternion.Euler(angles.y,0,0);vertical.Reset();DesiredHorizontalVelocity=Vector3.zero;controller.enabled=true;
        }
    }
}
