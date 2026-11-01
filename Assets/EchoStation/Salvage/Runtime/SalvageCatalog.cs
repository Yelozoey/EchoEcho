using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace EchoStation.Salvage
{
    public enum CargoState{World,Held,Delivered}
    public sealed class CargoDefinition
    {
        public string Id{get;}public string Name{get;}public int Kilograms{get;}public int Points{get;}
        public CargoDefinition(string id,string name,int kilograms,int points)
        {if(string.IsNullOrWhiteSpace(id)||id.Length>64||id.Any(c=>!(c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_'))||string.IsNullOrWhiteSpace(name)||kilograms<=0||kilograms>100||points<=0||points>100)throw new ArgumentException("Invalid cargo definition.");Id=id;Name=name;Kilograms=kilograms;Points=points;}
    }
    public static class SalvageCatalog
    {
        public static readonly ReadOnlyCollection<CargoDefinition> Items=Array.AsReadOnly(new[]{
            new CargoDefinition("cargo_a","A 工具箱",2,2),new CargoDefinition("cargo_b","B 备件盒",4,4),new CargoDefinition("cargo_c","C 样本盒",2,3),
            new CargoDefinition("cargo_d","D 仪器",6,6),new CargoDefinition("cargo_e","E 电源单元",8,8),new CargoDefinition("cargo_f","F 记录盒",4,5),new CargoDefinition("cargo_heavy","超限设备",12,1)});
        public static CargoDefinition Get(string id)=>Items.SingleOrDefault(i=>i.Id==id)??throw new ArgumentException("Unknown cargo ID.");
    }
    public sealed class CargoRecord
    {
        public CargoDefinition Definition{get;}public CargoState State{get;internal set;}public Vector3 Position{get;internal set;}
        internal CargoRecord(CargoDefinition definition,Vector3 position){Definition=definition;Position=position;State=CargoState.World;}
    }
    public sealed class CargoRegistry
    {
        readonly Dictionary<string,CargoRecord> records=new Dictionary<string,CargoRecord>(StringComparer.Ordinal);
        public IEnumerable<CargoRecord> Records=>records.Values;
        public int Count=>records.Count;
        public bool Register(CargoDefinition definition,Vector3 position)
        {
            if(definition==null||!Finite(position))throw new ArgumentException("Invalid registration.");
            if(records.ContainsKey(definition.Id))return false;records.Add(definition.Id,new CargoRecord(definition,position));return true;
        }
        public CargoRecord Get(string id)=>id!=null&&records.TryGetValue(id,out var result)?result:throw new ArgumentException("Unknown registered cargo.");
        public static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
    }
}
