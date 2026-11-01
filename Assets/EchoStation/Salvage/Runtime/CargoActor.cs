using UnityEngine;

namespace EchoStation.Salvage
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CargoActor:MonoBehaviour
    {
        [SerializeField]string id;
        public string Id=>id;
        public BoxCollider Body=>GetComponent<BoxCollider>();
        public void Configure(string value){SalvageCatalog.Get(value);id=value;}
    }
}
