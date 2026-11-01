using System;
using EchoStation.Player;
using UnityEngine;
using UnityEngine.UI;

namespace EchoStation.Salvage
{
    public static class SalvageWidgets
    {
        public static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;}
        public static Text Label(RectTransform rect,string value,int size=21)
        {var text=rect.gameObject.AddComponent<Text>();text.font=RuntimeUiFont.Get();text.fontSize=size;text.text=value;text.color=new Color(.8f,.92f,1);text.alignment=TextAnchor.UpperLeft;text.supportRichText=false;text.raycastTarget=false;return text;}
        public static Button Button(Transform parent,string name,string title,float x,float y,float width,Action clicked)
        {var rect=Rect(name,parent,x,y,width,44);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.08f,.2f,.28f);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;var label=Label(Rect("Label",rect,8,8,width-16,32),title,20);label.alignment=TextAnchor.MiddleCenter;button.onClick.AddListener(()=>clicked());return button;}
        public static ScrollRect Scroll(Transform parent,string name,float x,float y,float width,float height,out RectTransform content)
        {var rect=Rect(name,parent,x,y,width,height);var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.015f,.035f,.05f,.9f);rect.gameObject.AddComponent<RectMask2D>();var scroll=rect.gameObject.AddComponent<ScrollRect>();scroll.viewport=rect;scroll.horizontal=false;content=Rect("Content",rect,0,0,width,0);content.anchorMax=new Vector2(1,1);content.sizeDelta=Vector2.zero;scroll.content=content;scroll.movementType=ScrollRect.MovementType.Clamped;return scroll;}
    }
}
