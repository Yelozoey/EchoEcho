using System.Collections.Generic;
using System.Linq;
using EchoStation.Echo;
using EchoStation.Player;
using UnityEngine;
namespace EchoStation.Salvage
{
    [RequireComponent(typeof(SalvageCommandHub))]
    public sealed class SalvageDiscoveryMarkers:MonoBehaviour
    {
        readonly Dictionary<string,TextMesh> markers=new Dictionary<string,TextMesh>();SalvageCommandHub hub;Transform view;float timer;
        public int VisibleCount=>markers.Values.Count(m=>m&&m.gameObject.activeInHierarchy);
        void Start(){hub=GetComponent<SalvageCommandHub>();view=GetComponent<SalvageMotor>().View;hub.Changed+=Refresh;Refresh();}
        public void Refresh()
        {
            if(!hub)return;foreach(var target in hub.Targets)
            {
                bool visible=hub.Known.Contains(target.Id)&&target.Allows(IntentKind.Investigate);
                if(!markers.TryGetValue(target.Id,out var marker)&&visible)
                {marker=new GameObject("Discovered_"+target.Id).AddComponent<TextMesh>();marker.transform.SetParent(target.transform,false);marker.transform.localPosition=Vector3.up*(target.Definition.Kind==SalvageTargetKind.Cargo?.9f:1.45f);marker.font=RuntimeUiFont.Get();marker.GetComponent<Renderer>().sharedMaterial=marker.font.material;marker.fontSize=32;marker.characterSize=.025f;marker.anchor=TextAnchor.MiddleCenter;marker.color=target.Definition.Kind==SalvageTargetKind.Danger?new Color(1,.5f,.18f):new Color(.1f,.8f,1);marker.text="◇ 已扫描\n"+target.Definition.Name;markers.Add(target.Id,marker);}
                if(marker)marker.gameObject.SetActive(visible);
            }
        }
        void LateUpdate(){if(!view)return;timer-=Time.unscaledDeltaTime;if(timer<=0){timer=.2f;Refresh();}foreach(var marker in markers.Values)if(marker&&marker.gameObject.activeInHierarchy)marker.transform.rotation=view.rotation;}
        void OnDestroy(){if(hub)hub.Changed-=Refresh;}
    }
}
