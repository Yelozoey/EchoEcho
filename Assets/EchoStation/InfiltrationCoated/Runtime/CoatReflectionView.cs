using UnityEngine;
using UnityEngine.InputSystem;
namespace EchoStation.InfiltrationCoated
{
    public sealed class CoatReflectionView:MonoBehaviour
    {
        [SerializeField]CoatPlanar reflection;[SerializeField]Font font;GUIStyle style;
        public bool ReflectionOn=>reflection&&reflection.enabled;
        public void Configure(CoatPlanar value,Font text){reflection=value;font=text;}
        public void SetReflection(bool value){if(reflection)reflection.enabled=value;}
        void Update(){if(Keyboard.current!=null&&Keyboard.current.f8Key.wasPressedThisFrame)SetReflection(!ReflectionOn);}
        void OnGUI(){if(!Application.isPlaying)return;style??=new GUIStyle(GUI.skin.box){font=font,fontSize=16,alignment=TextAnchor.MiddleLeft,normal={textColor=new Color(.8f,.92f,1)}};GUI.Box(new Rect(12,108,690,32),"涂装完成版 · 沿蓝线进入中庭 · F8 地面实时倒影："+(ReflectionOn?"开":"关"),style);}
    }
}
