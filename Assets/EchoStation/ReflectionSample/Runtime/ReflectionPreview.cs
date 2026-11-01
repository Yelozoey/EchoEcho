using System.Collections.Generic;
using System.Linq;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace EchoStation.ReflectionSample
{
    public sealed class ReflectionPreview:MonoBehaviour
    {
        [SerializeField] RenderPipelineAsset pipeline;
        [SerializeField] Font font;
        [SerializeField] MonoBehaviour planar;
        [SerializeField] PlayerInputReader reader;
        RenderPipelineAsset beforeDefault,beforeQuality;int beforeAA;bool bound;
        readonly Dictionary<Renderer,Material[]> originals=new();
        readonly List<Material> copies=new();
        GUIStyle title,body;
        public bool EnvironmentOn{get;private set;}=true;
        public bool HighlightsOn{get;private set;}=true;
        public bool DynamicOn=>planar&&planar.enabled;
        public RenderPipelineAsset Pipeline=>pipeline;
        public void Configure(RenderPipelineAsset asset,Font text,PlayerInputReader input){pipeline=asset;font=text;reader=input;}
        public void ConfigureDynamic(MonoBehaviour value){planar=value;}
        void OnEnable()
        {
            if(!Application.isPlaying||!pipeline)return;
            beforeDefault=GraphicsSettings.defaultRenderPipeline;beforeQuality=QualitySettings.renderPipeline;beforeAA=QualitySettings.antiAliasing;bound=true;
            GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            foreach(var r in GetComponentsInChildren<Renderer>())
            {
                var source=r.sharedMaterials;if(!source.Any(m=>m&&m.shader.name=="Universal Render Pipeline/Lit"))continue;
                originals[r]=source;var own=source.Select(m=>m?new Material(m):null).ToArray();copies.AddRange(own.Where(m=>m));r.sharedMaterials=own;
            }
            Font.textureRebuilt+=RefreshText;RefreshText(font);
        }
        void RefreshText(Font changed){if(changed!=font||!font)return;foreach(var t in GetComponentsInChildren<TextMesh>())t.GetComponent<Renderer>().sharedMaterial.mainTexture=font.material.mainTexture;}
        public void SetDynamic(bool value){if(planar)planar.enabled=value;}
        public void SetEnvironment(bool value)
        {
            EnvironmentOn=value;
            foreach(var m in copies){if(!m.HasProperty("_EnvironmentReflections"))continue;m.SetFloat("_EnvironmentReflections",value?1:0);if(value)m.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");else m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");}
        }
        public void SetHighlights(bool value)
        {
            HighlightsOn=value;
            foreach(var m in copies){if(!m.HasProperty("_SpecularHighlights"))continue;m.SetFloat("_SpecularHighlights",value?1:0);if(value)m.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");else m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");}
        }
        void Update()
        {
            var k=Keyboard.current;if(k==null||!reader||!reader.HasApplicationFocus)return;
            if(k.digit1Key.wasPressedThisFrame)SetDynamic(!DynamicOn);
            if(k.digit2Key.wasPressedThisFrame)SetEnvironment(!EnvironmentOn);
            if(k.digit3Key.wasPressedThisFrame)SetHighlights(!HighlightsOn);
        }
        void OnDisable()
        {
            Font.textureRebuilt-=RefreshText;
            foreach(var pair in originals)if(pair.Key)pair.Key.sharedMaterials=pair.Value;
            foreach(var m in copies)if(m)Destroy(m);copies.Clear();originals.Clear();
            if(!bound)return;
            if(GraphicsSettings.defaultRenderPipeline==pipeline)GraphicsSettings.defaultRenderPipeline=beforeDefault;
            if(QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=beforeQuality;
            QualitySettings.antiAliasing=beforeAA;bound=false;
        }
        void OnGUI()
        {
            if(!Application.isPlaying)return;
            title??=new GUIStyle(GUI.skin.label){font=font,fontSize=20,fontStyle=FontStyle.Bold,normal={textColor=Color.white}};
            body??=new GUIStyle(GUI.skin.label){font=font,fontSize=16,normal={textColor=new Color(.86f,.93f,1)}};
            var color=GUI.color;GUI.color=new Color(.012f,.02f,.03f,.9f);GUI.DrawTexture(new Rect(12,10,900,104),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(12,Screen.height-68,1040,56),Texture2D.whiteTexture);GUI.color=color;
            GUI.Label(new Rect(24,16,870,30),"金属与反射小样 / PBR ＋ 探针 ＋ 实时平面倒影",title);
            GUI.Label(new Rect(24,50,870,25),"独立技术展示 · 非硬件光追 · 球面使用探针，只有中央地面使用平面反射",body);
            GUI.Label(new Rect(24,78,870,25),"1 地面倒影 "+(planar?(DynamicOn?"开":"关"):"待接入")+"   2 球面环境反射 "+(EnvironmentOn?"开":"关")+"   3 直接高光 "+(HighlightsOn?"开":"关"),body);
            GUI.Label(new Rect(24,Screen.height-64,1010,25),"点击Game；WASD/鼠标/空格走看，Esc释放。对比左粗糙金属、中抛光金属、右原涂装。",body);
            GUI.Label(new Rect(24,Screen.height-38,1010,25),"靠近中央金属地面看机器人倒影；1切换实时倒影。P暂停/恢复演示移动。",body);
        }
    }
}
