using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoStation.Player
{
    internal static class RuntimeUi
    {
        internal static readonly Color TextColor=new Color(.82f,.9f,1);
        internal static readonly Color ButtonColor=new Color(.04f,.075f,.11f,1);
        internal static readonly Color SelectedColor=new Color(.1f,.23f,.32f,1);
        internal static Canvas CreateCanvas(string name,Scene scene,int order)
        {
            var root=new GameObject(name,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.layer=5;SceneManager.MoveGameObjectToScene(root,scene);var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=order;var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;return canvas;
        }
        internal static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position,Vector2 anchor,Vector2 pivot)
        {
            var go=new GameObject(name,typeof(RectTransform));go.layer=5;go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
        }
        internal static RectTransform Fill(string name,Transform parent,Vector2 inset)
        {
            var rect=Rect(name,parent,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(.5f,.5f));rect.anchorMax=Vector2.one;rect.offsetMin=inset;rect.offsetMax=-inset;return rect;
        }
        internal static Image Image(RectTransform rect,Color color,bool raycast=true){var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=raycast;return image;}
        internal static Text Label(RectTransform rect,int size,TextAnchor anchor=TextAnchor.MiddleLeft)
        {
            var label=rect.gameObject.AddComponent<Text>();label.font=RuntimeUiFont.Get();label.fontSize=size;label.color=TextColor;label.alignment=anchor;label.supportRichText=false;label.raycastTarget=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Overflow;return label;
        }
        internal static Button Button(RectTransform rect,string caption,Action clicked,int size=18)
        {
            var image=Image(rect,ButtonColor);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;button.navigation=new Navigation{mode=Navigation.Mode.None};var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.25f,1.25f,1.25f);colors.pressedColor=new Color(.75f,.8f,.85f);colors.selectedColor=Color.white;button.colors=colors;var label=Label(Fill("Caption",rect,new Vector2(10,2)),size,TextAnchor.MiddleCenter);label.text=caption;button.onClick.AddListener(()=>clicked());return button;
        }
        internal static ScrollRect ScrollViewport(string name,Transform parent,Vector2 position,Vector2 size,out RectTransform content)
        {
            var viewport=Rect(name,parent,size,position,new Vector2(0,1),new Vector2(0,1));Image(viewport,new Color(.015f,.03f,.047f,.8f));viewport.gameObject.AddComponent<RectMask2D>();content=Rect("Content",viewport,new Vector2(0,size.y),Vector2.zero,new Vector2(0,1),new Vector2(.5f,1));content.anchorMax=new Vector2(1,1);var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=30;scroll.inertia=true;return scroll;
        }
    }
}
