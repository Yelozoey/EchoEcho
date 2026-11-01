using UnityEngine;
using UnityEngine.InputSystem;

namespace EchoStation.InfiltrationWallPreview
{
    public sealed class WallBrightnessPreview:MonoBehaviour
    {
        public Renderer[] surfaces;
        public Material original,brighter;
        public Light grazing;
        public Font font;
        public bool Coated;
        [SerializeField] int mode=2;
        GUIStyle style;
        public int Mode=>mode;
        public void SetMode(int value)
        {
            mode=Mathf.Clamp(value,0,2);
            foreach(var surface in surfaces)if(surface){var slots=surface.sharedMaterials;slots[0]=mode==0?original:brighter;surface.sharedMaterials=slots;}
            if(grazing)grazing.enabled=mode==2;
        }
        void OnEnable(){if(Application.isPlaying)SetMode(mode);}
        void OnDisable(){foreach(var surface in surfaces)if(surface){var slots=surface.sharedMaterials;slots[0]=original;surface.sharedMaterials=slots;}if(grazing)grazing.enabled=false;}
        void Update()
        {
            var keys=Keyboard.current;if(keys==null)return;
            if(keys.f5Key.wasPressedThisFrame)SetMode(0);
            if(keys.f6Key.wasPressedThisFrame)SetMode(1);
            if(keys.f7Key.wasPressedThisFrame)SetMode(2);
        }
        void OnGUI()
        {
            if(!Application.isPlaying)return;
            style??=new GUIStyle(GUI.skin.box){font=font,fontSize=17,alignment=TextAnchor.MiddleLeft,normal={textColor=new Color(.87f,.94f,1)}};
            string[] names=Coated?new[]{"原裸金属","浅灰半哑光涂层（无补灯）","浅灰涂层＋弱补光"}:new[]{"原效果","仅提亮底色","底色＋柔和掠射光"};
            GUI.Box(new Rect(12,148,840,60),(Coated?"入口背墙 · F5 原裸金属 / F6 浅灰涂层 / F7 涂层＋弱补光":"入口背墙 · F5 原效果 / F6 仅底色 / F7 底色＋掠射光")+"\n当前："+names[mode]+" · 曝光固定，可侧移观察凹凸与反光",style);
        }
    }
}
