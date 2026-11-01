using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoStation.Player
{
    [DefaultExecutionOrder(100),RequireComponent(typeof(PlayerInteraction)),DisallowMultipleComponent]
    public sealed class InteractionPrompt:MonoBehaviour
    {
        private GameObject root;private Canvas canvas;private Image panel,crosshair;private Text label;private PlayerInteraction interaction;private PlayerInputReader input;private string last="";
        public bool IsVisible=>label&&label.enabled;
        public string CurrentText=>label?label.text:"";
        public Canvas Canvas=>canvas;
        private void Awake(){if(Application.isPlaying)Build();}
        private void Build()
        {
            if(root)return;interaction=GetComponent<PlayerInteraction>();input=GetComponent<PlayerInputReader>();root=new GameObject("ES_InteractionOverlay",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));root.layer=5;SceneManager.MoveGameObjectToScene(root,gameObject.scene);canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=20;var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            panel=Element("PromptBackground",root.transform,new Vector2(.5f,0),new Vector2(940,48),new Vector2(0,80)).AddComponent<Image>();panel.color=new Color(.025f,.04f,.065f,.9f);panel.raycastTarget=false;
            label=Element("PromptText",root.transform,new Vector2(.5f,0),new Vector2(920,48),new Vector2(0,80)).AddComponent<Text>();label.font=RuntimeUiFont.Get();label.fontSize=22;label.alignment=TextAnchor.MiddleCenter;label.color=new Color(.82f,.9f,1);label.supportRichText=false;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
            crosshair=Element("Crosshair",root.transform,new Vector2(.5f,.5f),new Vector2(3,3),Vector2.zero).AddComponent<Image>();crosshair.color=new Color(.65f,.82f,1,.85f);crosshair.raycastTarget=false;panel.enabled=label.enabled=false;
        }
        private static GameObject Element(string name,Transform parent,Vector2 anchor,Vector2 size,Vector2 position)
        {
            var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;return go;
        }
        private void LateUpdate()
        {
            Build();var text=interaction.Prompt;if(last!=text){label.text=text;last=text;}var visible=!string.IsNullOrEmpty(text)&&input.CanPlay;panel.enabled=label.enabled=visible;crosshair.enabled=input.CanPlay;
        }
        private void OnEnable(){if(Application.isPlaying){Build();canvas.enabled=true;}}
        private void OnDisable(){if(canvas)canvas.enabled=false;}
        private void OnDestroy(){if(root)Destroy(root);}
    }
}
