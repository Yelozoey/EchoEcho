using EchoStation.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace EchoStation.InfiltrationArt
{
    /// <summary>Play-only rendering scope and explicit art-preview instructions. No AI or mission logic.</summary>
    public sealed class ArtSampleRuntime : MonoBehaviour
    {
        [SerializeField] RenderPipelineAsset pipeline;
        [SerializeField] PlayerInputReader input;
        [SerializeField] Font font;
        RenderPipelineAsset previousDefault, previousQuality;
        int previousAA;
        bool bound;
        GUIStyle title, hint;
        public bool IsArtPreview => true;
        public void Configure(RenderPipelineAsset value, PlayerInputReader reader, Font textFont)
        { pipeline = value; input = reader; font = textFont; }
        void OnEnable()
        {
            if (!Application.isPlaying || !pipeline) return;
            previousDefault = GraphicsSettings.defaultRenderPipeline;
            previousQuality = QualitySettings.renderPipeline;
            previousAA = QualitySettings.antiAliasing;
            bound = true;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            Font.textureRebuilt += RefreshLabels;
            RefreshLabels(font);
        }
        void RefreshLabels(Font changed)
        {
            if(changed!=font||!font||!transform.parent)return;
            foreach(var text in transform.parent.GetComponentsInChildren<TextMesh>(true))
            {
                var material=text.GetComponent<Renderer>().sharedMaterial;
                if(material&&material.shader.name=="EchoStation/InfiltrationWorldLabel")material.mainTexture=font.material.mainTexture;
            }
        }
        void OnDisable()
        {
            Font.textureRebuilt -= RefreshLabels;
            if (!bound) return;
            if (GraphicsSettings.defaultRenderPipeline == pipeline) GraphicsSettings.defaultRenderPipeline = previousDefault;
            if (QualitySettings.renderPipeline == pipeline) QualitySettings.renderPipeline = previousQuality;
            QualitySettings.antiAliasing = previousAA;
            bound = false;
        }
        void OnGUI()
        {
            if (!Application.isPlaying) return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.88f, .95f, 1) } };
                hint = new GUIStyle(GUI.skin.label) { font = font, fontSize = 15, normal = { textColor = new Color(.78f, .85f, .92f) } };
            }
            var oldColor=GUI.color;GUI.color=new Color(.025f,.04f,.065f,.86f);
            GUI.DrawTexture(new Rect(12,10,Mathf.Min(Screen.width-24,700),72),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(12,Screen.height-42,Mathf.Min(Screen.width-24,650),34),Texture2D.whiteTexture);
            GUI.color=oldColor;
            GUI.Label(new Rect(22, 16, Screen.width - 44, 30), "ECHO STATION / 接待检查区 · 美术样板", title);
            GUI.Label(new Rect(22, 44, Screen.width - 44, 28), "本项仅看美术与漫游：门卫、Echo 均未接入 AI 或任务。", hint);
            GUI.Label(new Rect(22, Screen.height - 38, Screen.width - 44, 30),
                input && input.PointerCaptured ? "WASD 移动   鼠标观察   空格跳跃   Esc 释放鼠标" : "点击 Game 画面开始观察；Esc 可随时释放鼠标", hint);
        }
    }
}
