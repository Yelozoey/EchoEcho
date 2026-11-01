using System;
using EchoStation.Player;
using UnityEngine;
namespace EchoStation.SalvageExpansion
{
    public enum FacilityDeviceKind{FanSocket,DataSocket,Isolator,Valve,ClampA,ClampB,ClampC}
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FacilityDevice:MonoBehaviour
    {
        static readonly string[] Ids={"fan_socket","data_socket","isolator","valve","clamp_a","clamp_b","clamp_c"};
        static readonly string[] Names={"通风插口","数据插口","供电隔离器","蒸汽阀门","核心夹具A","核心夹具B","核心夹具C"};
        [SerializeField]FacilityDeviceKind kind;Material material;TextMesh label;string previous;
        public FacilityDeviceKind Kind=>kind;public string Id=>Ids[(int)kind];public string DisplayName=>Names[(int)kind];
        public void Configure(FacilityDeviceKind value){if(!Enum.IsDefined(typeof(FacilityDeviceKind),value))throw new ArgumentException("Invalid device kind.");kind=value;}
        public void Present(bool active,string state)
        {
            if(previous==state)return;previous=state;var renderer=GetComponent<Renderer>();if(renderer){if(!material)material=renderer.material;material.color=active?new Color(.15f,.65f,.55f):new Color(.35f,.32f,.25f);}
            if(!label){var go=new GameObject("DeviceLabel");go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*1.1f;label=go.AddComponent<TextMesh>();label.font=RuntimeUiFont.Get();label.GetComponent<Renderer>().sharedMaterial=label.font.material;label.anchor=TextAnchor.MiddleCenter;label.fontSize=32;label.characterSize=.03f;label.color=new Color(.75f,.95f,1);}
            label.text=DisplayName+"\n"+state;
        }
        void LateUpdate(){if(label&&Camera.main)label.transform.rotation=Camera.main.transform.rotation;}
        void OnDestroy(){if(material)Destroy(material);}
    }
}
