using System;
using UnityEngine;
namespace EchoStation.SalvageExpansion
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ExpansionItemActor:MonoBehaviour
    {
        [SerializeField]string id;
        public string Id=>id;
        public BoxCollider Body=>GetComponent<BoxCollider>();
        public void Configure(string value){if(!InventoryItemDefinition.ValidId(value))throw new ArgumentException("Invalid owned item ID.");id=value;}
    }
}
