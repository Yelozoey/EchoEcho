using System;
using EchoStation.Echo;
using EchoStation.Player;
using EchoStation.Salvage;
using UnityEngine;
namespace EchoStation.SalvageNext
{
    [DefaultExecutionOrder(100),DisallowMultipleComponent]
    public sealed class NextWorldLabels:MonoBehaviour
    {
        sealed class Entry
        {
            public TextMesh Text;public Renderer Renderer;public Material OriginalMaterial;public Font OriginalFont;public string OriginalText;public Color OriginalColor;public float OriginalSize;public int OriginalFontSize;public TextAnchor Anchor;public TextAlignment Alignment;public Quaternion Rotation;public Vector3 Position;public bool Enabled,Known;public CargoActor Cargo;public SalvageTargetView Target;
        }
        [SerializeField]Camera view;[SerializeField]Shader shader;[SerializeField]TextMesh[] labels=Array.Empty<TextMesh>();[SerializeField]SalvageCommandHub hub;
        Entry[] entries=Array.Empty<Entry>();Font font;Material material;SalvageCarry carry;
        public Material RenderingMaterial=>material;public int Rebuilds{get;private set;}public int LabelCount=>entries.Length;
        public void Configure(Camera camera,Shader textShader,TextMesh[] texts,SalvageCommandHub owner=null)
        {if(!camera||!textShader||texts==null||texts.Length==0||Array.Exists(texts,t=>!t||t.gameObject.scene!=gameObject.scene))throw new ArgumentException("Owned labels, camera and depth shader required.");Release();view=camera;shader=textShader;labels=(TextMesh[])texts.Clone();hub=owner;if(Application.isPlaying&&isActiveAndEnabled)Bind();}
        void OnEnable(){if(Application.isPlaying&&view&&shader&&labels.Length>0)Bind();}
        void Bind()
        {
            if(material)return;font=RuntimeUiFont.Get();material=new Material(shader){name="ES_Next_WorldText_Runtime",hideFlags=HideFlags.DontSave};material.mainTexture=font.material.mainTexture;Font.textureRebuilt+=FontRebuilt;carry=hub?hub.GetComponent<SalvageCarry>():null;entries=new Entry[labels.Length];
            for(int i=0;i<labels.Length;i++)
            {
                var text=labels[i];var renderer=text.GetComponent<Renderer>();var e=new Entry{Text=text,Renderer=renderer,OriginalMaterial=renderer.sharedMaterial,OriginalFont=text.font,OriginalText=text.text,OriginalColor=text.color,OriginalSize=text.characterSize,OriginalFontSize=text.fontSize,Anchor=text.anchor,Alignment=text.alignment,Rotation=text.transform.localRotation,Position=text.transform.localPosition,Enabled=renderer.enabled,Cargo=text.GetComponentInParent<CargoActor>(),Target=text.GetComponentInParent<SalvageTargetView>()};entries[i]=e;text.font=font;text.fontSize=e.OriginalFontSize>0?e.OriginalFontSize:32;text.characterSize=((e.Cargo||e.Target) ? .08f : .14f)*10/text.fontSize;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;if(e.Cargo)text.transform.position=new Vector3(e.Cargo.transform.position.x,e.Cargo.Body.bounds.max.y+.35f,e.Cargo.transform.position.z);renderer.sharedMaterial=material;
            }
            EvaluateNow();
        }
        void FontRebuilt(Font changed){if(changed!=font||!material)return;Rebuilds++;material.mainTexture=font.material.mainTexture;}
        public void EvaluateNow()
        {
            if(!material||!view)return;if(material.mainTexture!=font.material.mainTexture)material.mainTexture=font.material.mainTexture;
            foreach(var e in entries)
            {
                if(!e.Text||!e.Renderer)continue;bool known=hub&&e.Target&&hub.Known.Contains(e.Target.Id);
                if(known!=e.Known){e.Known=known;e.Text.text=(known?"◇ 已扫描\n":"")+e.OriginalText;e.Text.color=known?e.Target.Definition.Kind==SalvageTargetKind.Danger?new Color(1,.5f,.18f):new Color(.2f,.8f,1):e.OriginalColor;}
                bool world=!e.Cargo||!carry||carry.Registry.Get(e.Cargo.Id).State==CargoState.World;float range=e.Cargo||e.Target?7:12;e.Renderer.enabled=e.Enabled&&world&&e.Text.gameObject.activeInHierarchy&&(e.Text.transform.position-view.transform.position).sqrMagnitude<=range*range;e.Text.transform.rotation=view.transform.rotation;if(e.Renderer.sharedMaterial!=material)e.Renderer.sharedMaterial=material;
            }
        }
        void LateUpdate()=>EvaluateNow();
        void Release()
        {
            Font.textureRebuilt-=FontRebuilt;foreach(var e in entries)if(e!=null&&e.Text&&e.Renderer){e.Text.font=e.OriginalFont;e.Text.text=e.OriginalText;e.Text.color=e.OriginalColor;e.Text.characterSize=e.OriginalSize;e.Text.fontSize=e.OriginalFontSize;e.Text.anchor=e.Anchor;e.Text.alignment=e.Alignment;e.Text.transform.localRotation=e.Rotation;e.Text.transform.localPosition=e.Position;e.Renderer.sharedMaterial=e.OriginalMaterial;e.Renderer.enabled=e.Enabled;}entries=Array.Empty<Entry>();if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}material=null;font=null;carry=null;
        }
        void OnDisable()=>Release();
    }
}
