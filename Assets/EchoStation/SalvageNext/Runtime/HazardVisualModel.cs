using System;
using EchoStation.Salvage;
using UnityEngine;
namespace EchoStation.SalvageNext
{
    public sealed class HazardVisualModel:MonoBehaviour
    {
        [SerializeField]string id;
        public string Id=>id;
        public void Configure(string value){if(!SalvageTargetCatalog.TryGet(value,out var d)||d.Kind!=SalvageTargetKind.Danger)throw new ArgumentException("Readonly hazard metadata required.");id=value;}
    }
}
