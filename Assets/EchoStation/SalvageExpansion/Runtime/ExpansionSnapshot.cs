using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace EchoStation.SalvageExpansion
{
    public sealed class ExpansionSnapshot
    {
        readonly JObject sections;
        public IReadOnlyList<InventoryItemState> Inventory{get;}
        public IReadOnlyDictionary<string,Vector3> Positions{get;}
        public Vector3 Player{get;}
        public float Yaw{get;}
        public float Pitch{get;}
        public JObject Sections=>(JObject)sections.DeepClone();
        public ExpansionSnapshot(IEnumerable<InventoryItemState> inventory,IReadOnlyDictionary<string,Vector3> positions,Vector3 player,float yaw,float pitch,JObject sections)
        {
            var candidate=new InventoryStore(ExpansionCatalog.Items);if(!candidate.TryRestore(inventory,out var error))throw new ArgumentException(error);
            if(positions==null||positions.Count!=ExpansionCatalog.Items.Count||positions.Any(p=>!ExpansionCatalog.Items.Any(d=>d.Id==p.Key)||!Finite(p.Value)||p.Value.x<0||p.Value.x>36||p.Value.z<0||p.Value.z>24||p.Value.y<0||p.Value.y>5))throw new ArgumentException("Invalid world item positions.");
            if(!Finite(player)||player.x<.3f||player.x>35.7f||player.z<.3f||player.z>23.7f||player.y<-.05f||player.y>3||!float.IsFinite(yaw)||yaw<0||yaw>=360||!float.IsFinite(pitch)||pitch< -80||pitch>80||sections==null)throw new ArgumentException("Invalid player or section state.");
            Inventory=Array.AsReadOnly(candidate.Records.ToArray());Positions=new ReadOnlyDictionary<string,Vector3>(positions.ToDictionary(p=>p.Key,p=>p.Value,StringComparer.Ordinal));Player=player;Yaw=yaw;Pitch=pitch;this.sections=(JObject)sections.DeepClone();
        }
        static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
    }
    public interface IExpansionSaveSection
    {
        string Key{get;}
        JToken Capture();
        bool TryPrepare(ExpansionSnapshot snapshot,JToken value,out Action apply,out Action rollback,out string error);
    }
}
