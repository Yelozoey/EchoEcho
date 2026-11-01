using System;
using EchoStation.Player;
using UnityEngine;
namespace EchoStation.SalvageExpansion
{
    public enum EnvironmentHazardKind{Steam,Electrical}
    public enum HazardSignal{Safe,Warning,Active}
    [RequireComponent(typeof(BoxCollider))]
    public sealed class HazardZone:MonoBehaviour
    {
        [SerializeField]EnvironmentHazardKind kind;Material material;TextMesh label;HazardSignal? previous;
        public EnvironmentHazardKind Kind=>kind;
        public BoxCollider Volume=>GetComponent<BoxCollider>();
        public void Configure(EnvironmentHazardKind value){if(!Enum.IsDefined(typeof(EnvironmentHazardKind),value))throw new ArgumentException();kind=value;Volume.isTrigger=true;}
        public bool Contains(Vector3 position)=>Volume.bounds.Contains(position+Vector3.up*.8f);
        public void Present(HazardSignal signal)
        {
            if(previous==signal)return;previous=signal;var renderer=GetComponentInChildren<Renderer>();if(renderer){if(!material)material=renderer.material;material.color=signal==HazardSignal.Safe?new Color(.13f,.35f,.4f):signal==HazardSignal.Warning?new Color(1,.65f,.08f):new Color(.9f,.2f,.08f);}
            if(!label){var go=new GameObject("HazardLabel");go.transform.SetParent(transform,false);go.transform.localPosition=new Vector3(0,.65f,0);label=go.AddComponent<TextMesh>();label.font=RuntimeUiFont.Get();label.GetComponent<Renderer>().sharedMaterial=label.font.material;label.anchor=TextAnchor.MiddleCenter;label.fontSize=32;label.characterSize=.025f;label.color=Color.white;}
            label.text=(kind==EnvironmentHazardKind.Steam?"蒸汽":"漏电")+" · "+(signal==HazardSignal.Safe?"安全":signal==HazardSignal.Warning?"即将喷发":"危险");
        }
        void LateUpdate(){if(label&&Camera.main)label.transform.rotation=Camera.main.transform.rotation;}
        void OnDestroy(){if(material)Destroy(material);}
    }
}
