using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EchoStation.Echo;

namespace EchoStation.Salvage
{
    public enum SalvageTargetKind{Cargo,Danger}
    public sealed class SalvageTargetDefinition
    {
        public string Id{get;}public string Name{get;}public string Category{get;}public string Summary{get;}public SalvageTargetKind Kind{get;}
        public SalvageTargetDefinition(string id,string name,string category,string summary,SalvageTargetKind kind)
        {
            if(string.IsNullOrWhiteSpace(id)||id.Length>64||id.Any(c=>!(c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_'))||string.IsNullOrWhiteSpace(name)||string.IsNullOrWhiteSpace(category)||string.IsNullOrWhiteSpace(summary)||summary.Length>120||!Enum.IsDefined(typeof(SalvageTargetKind),kind))throw new ArgumentException("Invalid information target.");
            Id=id;Name=name;Category=category;Summary=summary;Kind=kind;
        }
    }
    public static class SalvageTargetCatalog
    {
        public static readonly ReadOnlyCollection<SalvageTargetDefinition> Items=Array.AsReadOnly(new[]{
            Cargo(0,"tool","常规维修工具，适合基础维护。"),Cargo(1,"parts","标准接口备件，需人工回收。"),Cargo(2,"sample","密封样本容器，避免碰撞。"),
            Cargo(3,"instrument","精密测量组件，携带时保持稳定。"),Cargo(4,"power","独立电源组件，Echo不能直接接线。"),Cargo(5,"record","离线记录载体，内容需回收后由人工处理。"),
            new SalvageTargetDefinition("danger_power_box","故障配电箱","electrical","配电箱外壳存在故障信号，建议保持距离。",SalvageTargetKind.Danger),
            new SalvageTargetDefinition("danger_leak_pipe","泄漏管段","leak","管段存在泄漏迹象，建议绕开近处。",SalvageTargetKind.Danger)});
        static SalvageTargetDefinition Cargo(int i,string category,string summary){var c=SalvageCatalog.Items[i];return new SalvageTargetDefinition(c.Id,c.Name,category,summary,SalvageTargetKind.Cargo);}
        public static bool TryGet(string id,out SalvageTargetDefinition definition){definition=Items.FirstOrDefault(d=>d.Id==id);return definition!=null;}
        public static bool Allows(string id,IntentKind intent,CargoState? state)
        {
            if(!TryGet(id,out var d))return false;
            if(d.Kind==SalvageTargetKind.Cargo&&state!=CargoState.World)return false;
            return intent==IntentKind.Investigate||intent==IntentKind.Warn&&d.Kind==SalvageTargetKind.Danger;
        }
    }
    // Owned by the traditional sensor layer. Only canonical summaries cross this boundary.
    public sealed class SalvageDiscoveryIndex
    {
        readonly HashSet<string> ids=new HashSet<string>(StringComparer.Ordinal);
        public int Count=>ids.Count;
        public bool Contains(string id)=>id!=null&&ids.Contains(id);
        public string Summary(string id)=>Contains(id)&&SalvageTargetCatalog.TryGet(id,out var definition)?definition.Summary:null;
        public string[] Snapshot()=>ids.OrderBy(i=>i,StringComparer.Ordinal).ToArray();
        internal bool CommitValidatedSensor(string id){if(!SalvageTargetCatalog.TryGet(id,out _))throw new ArgumentException("Unknown discovery ID.");return ids.Add(id);}
        internal void RestoreValidatedIds(IEnumerable<string> source)
        {
            var copy=source?.ToArray()??throw new ArgumentNullException(nameof(source));if(copy.Length>8||copy.Any(id=>!SalvageTargetCatalog.TryGet(id,out _))||copy.Distinct(StringComparer.Ordinal).Count()!=copy.Length)throw new ArgumentException("Invalid discovery snapshot.");
            ids.Clear();foreach(var id in copy)ids.Add(id);
        }
    }
}
