using UnityEngine;
using UnityEngine.InputSystem;
namespace EchoStation.ReflectionSample
{
    // Controlled art demonstration, not guard AI, navigation, or an LLM command.
    public sealed class ReflectionDemoMotion:MonoBehaviour
    {
        [SerializeField]Transform robot;
        float elapsed;public bool Paused{get;private set;}
        public void Configure(Transform value){robot=value;}
        public void SetPaused(bool value){Paused=value;}
        public void Pose(float phase){if(robot)robot.position=new Vector3(Mathf.Sin(phase)*.85f,.045f,.35f);}
        void Update()
        {if(Keyboard.current!=null&&Keyboard.current.pKey.wasPressedThisFrame)Paused=!Paused;if(!Paused){elapsed+=Time.deltaTime;Pose(elapsed*.75f);}}
    }
}
