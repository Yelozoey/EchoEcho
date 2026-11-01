using System;
using UnityEngine;

namespace EchoStation.Player
{
    [RequireComponent(typeof(CharacterController),typeof(PlayerInputReader)),DisallowMultipleComponent]
    public sealed class FirstPersonMotor:MonoBehaviour
    {
        [SerializeField]private Transform view;
        private CharacterController controller;private PlayerInputReader input;private bool cached;private Vector2 yawPitch;
        private readonly PlayerVerticalMotion vertical=new PlayerVerticalMotion();
        public Transform View=>view;
        public float Yaw=>yawPitch.x;
        public float Pitch=>yawPitch.y;
        public float VerticalSpeed=>vertical.Velocity;
        public bool IsGrounded=>controller&&controller.isGrounded;
        public CollisionFlags LastCollision{get;private set;}
        public void Configure(Transform cameraView){if(!cameraView||!cameraView.IsChildOf(transform))throw new ArgumentException("View must belong to the player.");view=cameraView;cached=false;}
        private void Cache(){if(cached)return;if(!view)throw new InvalidOperationException("Player view is missing.");controller=GetComponent<CharacterController>();input=GetComponent<PlayerInputReader>();yawPitch=new Vector2(transform.eulerAngles.y,Mathf.DeltaAngle(0,view.localEulerAngles.x));cached=true;}
        private void Awake()=>Cache();
        private void Update()=>ApplyInput(input.Move,input.Look,Mathf.Min(Time.deltaTime,.1f),input.JumpPressed);
        public void ApplyInput(Vector2 move,Vector2 mouseDelta,float seconds,bool jumpPressed=false)
        {
            Cache();var next=FirstPersonMotion.LookAngles(mouseDelta,yawPitch);var delta=FirstPersonMotion.HorizontalDelta(move,next.x,seconds);delta.y=vertical.Step(controller.isGrounded,jumpPressed,seconds);yawPitch=next;ApplyAngles();LastCollision=controller.Move(delta);vertical.ResolveCollision(LastCollision);
        }
        public void ResetMotion()=>vertical.Reset();
        public void SetLookAngles(float yaw,float pitch)
        {
            Cache();if(!float.IsFinite(yaw)||!float.IsFinite(pitch))throw new ArgumentException("Look angles must be finite.");yawPitch=new Vector2(Mathf.Repeat(yaw,360),Mathf.Clamp(pitch,-FirstPersonMotion.PitchLimit,FirstPersonMotion.PitchLimit));ApplyAngles();
        }
        private void ApplyAngles(){transform.rotation=Quaternion.Euler(0,yawPitch.x,0);view.localRotation=Quaternion.Euler(yawPitch.y,0,0);}
    }
}
