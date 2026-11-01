using EchoStation.Player;
using UnityEngine;
using UnityEngine.UI;

namespace EchoStation.Salvage
{
    public static class SalvageUi
    {
        public static Text Panel(Transform owner,string name,int width,int height,int order)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));root.transform.SetParent(owner,false);
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=order;
            var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);
            var panel=new GameObject("Panel",typeof(RectTransform),typeof(Image));panel.transform.SetParent(root.transform,false);
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(width,height);panel.GetComponent<Image>().color=new Color(.025f,.05f,.08f,.96f);
            var label=new GameObject("Text",typeof(RectTransform),typeof(Text));label.transform.SetParent(panel.transform,false);var textRect=label.GetComponent<RectTransform>();textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=new Vector2(24,20);textRect.offsetMax=new Vector2(-24,-20);
            var text=label.GetComponent<Text>();text.font=RuntimeUiFont.Get();text.fontSize=25;text.color=new Color(.78f,.9f,1);text.supportRichText=false;text.alignment=TextAnchor.MiddleLeft;text.raycastTarget=false;return text;
        }
        public static GameObject CanvasOf(Text text)=>text.GetComponentInParent<Canvas>(true).gameObject;
    }
}
