using EchoStation.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace EchoStation.InfiltrationLevel
{
    public sealed class LevelPreview:MonoBehaviour
    {
        [SerializeField] RenderPipelineAsset pipeline;
        [SerializeField] PlayerInputReader input;
        [SerializeField] Font font;
        RenderPipelineAsset originalDefault,originalQuality;int originalAA;bool bound,lines=true;
        GUIStyle title,body;
        public bool AiEnabled=>false;
        public void Configure(RenderPipelineAsset value,PlayerInputReader reader,Font textFont){pipeline=value;input=reader;font=textFont;}
        void OnEnable()
        {
            if(!Application.isPlaying||!pipeline)return;
            originalDefault=GraphicsSettings.defaultRenderPipeline;originalQuality=QualitySettings.renderPipeline;originalAA=QualitySettings.antiAliasing;bound=true;
            GraphicsSettings.defaultRenderPipeline=pipeline;QualitySettings.renderPipeline=pipeline;
            Font.textureRebuilt+=RefreshText;RefreshText(font);
        }
        void RefreshText(Font changed)
        {
            if(changed!=font||!font||!transform.parent)return;
            foreach(var t in transform.parent.GetComponentsInChildren<TextMesh>())if(t.GetComponent<Renderer>().sharedMaterial.shader.name=="EchoStation/InfiltrationLevel/WorldLabel")t.GetComponent<Renderer>().sharedMaterial.mainTexture=font.material.mainTexture;
        }
        void OnDisable()
        {
            Font.textureRebuilt-=RefreshText;if(!bound)return;
            if(GraphicsSettings.defaultRenderPipeline==pipeline)GraphicsSettings.defaultRenderPipeline=originalDefault;
            if(QualitySettings.renderPipeline==pipeline)QualitySettings.renderPipeline=originalQuality;
            QualitySettings.antiAliasing=originalAA;bound=false;
        }
        void Update()
        {
            if(Keyboard.current!=null&&Keyboard.current.tabKey.wasPressedThisFrame)
            {lines=!lines;foreach(var line in transform.parent.GetComponentsInChildren<LineRenderer>())line.enabled=lines;}
        }
        void OnGUI()
        {
            if(!Application.isPlaying)return;
            if(title==null){title=new GUIStyle(GUI.skin.label){font=font,fontSize=19,fontStyle=FontStyle.Bold,normal={textColor=new Color(.88f,.95f,1)}};body=new GUIStyle(GUI.skin.label){font=font,fontSize=15,normal={textColor=new Color(.85f,.9f,.94f)}};}
            var old=GUI.color;GUI.color=new Color(.015f,.025f,.04f,.88f);GUI.DrawTexture(new Rect(12,10,690,92),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(12,Screen.height-66,850,54),Texture2D.whiteTexture);GUI.color=old;
            int x=Mathf.Clamp(Mathf.FloorToInt((transform.position.x+18)/4),0,8),z=Mathf.Clamp(Mathf.FloorToInt((transform.position.z+16)/4),0,7);
            GUI.Label(new Rect(24,16,660,28),"ECHO STATION / 路线与光影样板 · "+LevelLayout.Names[LevelLayout.Zone(x,z)],title);
            GUI.Label(new Rect(24,46,660,25),"守卫仅展示岗位；巡逻、证件、任务与 LLM 尚未接入。",body);
            GUI.Label(new Rect(24,71,660,25),"蓝：正面检查预留   橙：行政侧路   青：物流 / 检修绕行",body);
            GUI.Label(new Rect(24,Screen.height-62,820,25),input&&input.PointerCaptured?"WASD 移动   鼠标观察   空格跳跃   Esc 释放鼠标   Tab 路线示意":"点击 Game 捕获鼠标；Esc 释放。Tab 切换路线示意。",body);
            GUI.Label(new Rect(24,Screen.height-38,820,25),"从大厅选择三路入口；在办公 / 仓区换线，绕到后侧检修廊再返回。",body);
        }
    }
}
