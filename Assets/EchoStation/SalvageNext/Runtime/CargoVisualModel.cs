using System;
using System.Linq;
using EchoStation.Salvage;
using UnityEngine;

namespace EchoStation.SalvageNext
{
    public sealed class CargoVisualModel:MonoBehaviour
    {
        [SerializeField]string id;
        [SerializeField]Bounds localBounds;
        [SerializeField]CargoActor owner;
        [SerializeField]Renderer[] labels=Array.Empty<Renderer>();
        bool? lastVisible;
        public string Id=>id;
        public Bounds LocalBounds=>localBounds;
        public void Configure(string item,Bounds bounds)
        {
            SalvageCatalog.Get(item);if(!CargoRegistry.Finite(bounds.center)||!CargoRegistry.Finite(bounds.size)||bounds.size.x<=0||bounds.size.y<=0||bounds.size.z<=0)throw new ArgumentException("Finite visual bounds required.");id=item;localBounds=bounds;
        }
        public void Bind(CargoActor actor,Renderer[] worldLabels)
        {
            if(!actor||actor.Id!=id||actor.gameObject.scene!=gameObject.scene||worldLabels==null||worldLabels.Any(r=>!r||!r.transform.IsChildOf(actor.transform)))throw new ArgumentException("Owned matching cargo and labels required.");
            owner=actor;labels=worldLabels.ToArray();lastVisible=null;RefreshLabels();
        }
        public void RefreshLabels()
        {
            if(!owner)return;bool visible=owner.Body.enabled&&owner.gameObject.activeInHierarchy;if(lastVisible==visible)return;lastVisible=visible;
            foreach(var renderer in labels)if(renderer)renderer.enabled=visible;
        }
        void OnEnable(){lastVisible=null;RefreshLabels();}
        void LateUpdate()=>RefreshLabels();
    }
}
