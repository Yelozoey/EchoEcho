using System;
using EchoStation.Echo;
using EchoStation.World;
using UnityEngine;

namespace EchoStation.Salvage
{
    [RequireComponent(typeof(WorldTarget),typeof(EchoTargetPermissions))]
    public sealed class SalvageTargetView:MonoBehaviour
    {
        [SerializeField]string id;[SerializeField]SalvageCarry owner;
        public string Id=>id;
        public WorldTarget World=>GetComponent<WorldTarget>();
        public SalvageTargetDefinition Definition=>SalvageTargetCatalog.TryGet(id,out var d)?d:throw new InvalidOperationException("Unknown target metadata.");
        public void Configure(string value,SalvageCarry player,Transform approach)
        {
            if(gameObject.activeInHierarchy||!player||player.gameObject.scene!=gameObject.scene||!approach||!approach.IsChildOf(transform)||!SalvageTargetCatalog.TryGet(value,out var d))throw new ArgumentException("Configure an inactive owned target and approach.");
            id=value;owner=player;World.Configure(value,d.Name);var p=GetComponent<EchoTargetPermissions>();p.Configure(true,d.Kind==SalvageTargetKind.Danger);p.SetApproachPoint(approach);p.SetScanFact(d.Summary);
        }
        public bool Allows(IntentKind intent)
        {
            if(!isActiveAndEnabled||!owner||owner.gameObject.scene!=gameObject.scene||!SalvageTargetCatalog.TryGet(id,out var d)||World.Id!=id)return false;
            CargoState? state=null;
            if(d.Kind==SalvageTargetKind.Cargo)
            {var actor=GetComponent<CargoActor>();if(!actor||actor.Id!=id)return false;try{if(owner.Actor(id)!=actor)return false;state=owner.Registry.Get(id).State;}catch(ArgumentException){return false;}}
            var permission=GetComponent<EchoTargetPermissions>();return permission.CanInvestigate&&permission.ScanFact==d.Summary&&(intent!=IntentKind.Warn||permission.IsDanger)&&SalvageTargetCatalog.Allows(id,intent,state);
        }
    }
}
