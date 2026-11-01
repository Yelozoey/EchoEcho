using EchoStation.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoStation.ParkourArt
{
    [RequireComponent(typeof(CharacterController),typeof(PlayerInputReader)),DisallowMultipleComponent]
    public sealed class VectorCityFirstPerson : MonoBehaviour
    {
        [SerializeField] Camera view;
        [SerializeField] Vector3 spawnPosition=new Vector3(-22,12.10f,-25.5f);
        [SerializeField] float spawnYaw;
        [SerializeField] float walkSpeed=4.5f,sprintSpeed=8,jumpHeight=1.6f;
        const float Gravity=18;
        CharacterController controller;PlayerInputReader input;
        Vector2 yawPitch;float verticalSpeed;
        public Camera View=>view;
        public Vector3 SpawnPosition=>spawnPosition;
        public float Yaw=>yawPitch.x;
        public float Pitch=>yawPitch.y;
        public bool Grounded=>controller&&controller.isGrounded;
        public CollisionFlags LastCollision {get;private set;}
        public void Configure(Camera camera,Vector3 spawn,float yaw=0)
        {view=camera;spawnPosition=spawn;spawnYaw=yaw;}
        void Awake()
        {
            controller=GetComponent<CharacterController>();input=GetComponent<PlayerInputReader>();
            if(!view){enabled=false;Debug.LogError("VectorCity player camera is missing.",this);return;}
            yawPitch=new Vector2(transform.eulerAngles.y,0);ResetToSpawn();
        }
        void Update()
        {
            if(transform.position.y<6){ResetToSpawn();return;}
            var keyboard=Keyboard.current;
            if(input.CanPlay&&keyboard!=null&&keyboard.rKey.wasPressedThisFrame){ResetToSpawn();return;}
            var dt=Mathf.Min(Time.deltaTime,.05f);
            yawPitch=FirstPersonMotion.LookAngles(input.Look,yawPitch);ApplyLook();
            bool sprint=input.CanPlay&&keyboard!=null&&(keyboard.leftShiftKey.isPressed||keyboard.rightShiftKey.isPressed);
            var movement=FirstPersonMotion.HorizontalDelta(input.Move,yawPitch.x,dt)*((sprint?sprintSpeed:walkSpeed)/FirstPersonMotion.Speed);
            if(controller.isGrounded&&verticalSpeed<=0)verticalSpeed=input.JumpPressed?Mathf.Sqrt(2*Gravity*jumpHeight):-2;
            verticalSpeed=Mathf.Max(verticalSpeed-Gravity*dt,-30);movement.y=verticalSpeed*dt;
            LastCollision=controller.Move(movement);
            if((LastCollision&CollisionFlags.Above)!=0&&verticalSpeed>0)verticalSpeed=0;
            if((LastCollision&CollisionFlags.Below)!=0&&verticalSpeed<0)verticalSpeed=-2;
        }
        public void SetLookAngles(float yaw,float pitch)
        {yawPitch=new Vector2(Mathf.Repeat(yaw,360),Mathf.Clamp(pitch,-FirstPersonMotion.PitchLimit,FirstPersonMotion.PitchLimit));ApplyLook();}
        void ApplyLook(){transform.rotation=Quaternion.Euler(0,yawPitch.x,0);if(view)view.transform.localRotation=Quaternion.Euler(yawPitch.y,0,0);}
        public void ResetToSpawn()
        {
            if(!controller)controller=GetComponent<CharacterController>();
            bool wasEnabled=controller.enabled;controller.enabled=false;transform.position=spawnPosition;controller.enabled=wasEnabled;
            verticalSpeed=0;SetLookAngles(spawnYaw,0);
        }
    }
}
