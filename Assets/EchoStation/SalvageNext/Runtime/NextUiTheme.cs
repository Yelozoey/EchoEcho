using EchoStation.Salvage;
using UnityEngine;
using UnityEngine.UI;
namespace EchoStation.SalvageNext
{
    public sealed class NextUiTheme:ScriptableObject
    {
        public Sprite Panel,Button,Crosshair,E,G,R,Tab,F,T;
        public static readonly Color Ink=new Color(.92f,.97f,1),Muted=new Color(.66f,.79f,.85f),Cyan=new Color(.34f,.88f,.95f),Orange=new Color(1,.65f,.3f);
        public Image Box(RectTransform rect,bool button=false)
        {var image=rect.gameObject.AddComponent<Image>();image.sprite=Button;image.type=Image.Type.Sliced;image.color=button?new Color(.12f,.25f,.32f,.98f):new Color(.045f,.10f,.14f,.97f);image.raycastTarget=false;return image;}
        public Text Label(Transform parent,string name,float x,float y,float w,float h,string value,int size=26,Color? color=null)
        {var text=SalvageWidgets.Label(SalvageWidgets.Rect(name,parent,x,y,w,h),value,size);text.color=color??Ink;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;return text;}
        public Image Icon(Transform parent,string name,Sprite sprite,float x,float y,float w=40,float h=40)
        {var image=SalvageWidgets.Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;}
        public void StyleButton(UnityEngine.UI.Button button)
        {
            var image=button.GetComponent<Image>();if(image){image.sprite=Button;image.type=Image.Type.Sliced;image.color=new Color(.12f,.27f,.35f,1);}
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.55f,1,1);colors.pressedColor=new Color(.3f,.75f,.85f);colors.selectedColor=new Color(.65f,.94f,1);colors.disabledColor=new Color(.42f,.5f,.55f,.75f);button.colors=colors;
            var text=button.GetComponentInChildren<Text>();if(text){text.color=Ink;text.fontSize=22;var rect=text.rectTransform;rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,-4);rect.sizeDelta=new Vector2(rect.sizeDelta.x,36);}
        }
        public void StyleCanvas(Transform root,bool backdrop=true)
        {
            foreach(var image in root.GetComponentsInChildren<Image>(true))if(!image.GetComponent<UnityEngine.UI.Button>()){image.color=image.name=="Panel"?new Color(.035f,.075f,.11f,.98f):new Color(.025f,.06f,.09f,.98f);if(image.name=="Panel"){image.sprite=Button;image.type=Image.Type.Sliced;}}
            foreach(var text in root.GetComponentsInChildren<Text>(true))text.color=text.name=="Text"&&text.transform.parent.name=="Panel"?Cyan:Ink;
            foreach(var button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))StyleButton(button);
            if(backdrop&&!root.Find("ModalShade")){var rect=SalvageWidgets.Rect("ModalShade",root,0,0,0,0);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;rect.SetAsFirstSibling();rect.gameObject.AddComponent<Image>().color=new Color(0,.018f,.028f,.55f);}
        }
    }
}
