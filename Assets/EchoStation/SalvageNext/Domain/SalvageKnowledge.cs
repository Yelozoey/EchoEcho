using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EchoStation.Salvage;

namespace EchoStation.SalvageNext
{
    public enum RecoveryPreference { Nearby, HighScore, Lightweight }

    // Only the main-thread trusted adapter constructs observations. No Unity objects cross this boundary.
    internal sealed class ObservedTarget
    {
        internal string Id { get; }
        internal bool Known { get; }
        internal CargoState? State { get; }
        internal bool Reachable { get; }
        internal double? PathMeters { get; }
        internal ObservedTarget(string id, bool known, CargoState? state, bool reachable, double? pathMeters)
        {
            if (!SalvageTargetCatalog.TryGet(id, out var definition)) throw new ArgumentException("Unknown information target.");
            if (definition.Kind == SalvageTargetKind.Cargo ? !state.HasValue : state.HasValue) throw new ArgumentException("Target kind/state mismatch.");
            if (state.HasValue && !Enum.IsDefined(typeof(CargoState), state.Value)) throw new ArgumentException("Invalid state.");
            if (pathMeters.HasValue && (double.IsNaN(pathMeters.Value) || double.IsInfinity(pathMeters.Value) || pathMeters < 0)) throw new ArgumentException("Invalid path length.");
            if (reachable != pathMeters.HasValue) throw new ArgumentException("Reachable path required.");
            if (!known && (reachable || pathMeters.HasValue)) throw new ArgumentException("Unknown locations cannot enter memory.");
            Id=id; Known=known; State=state; Reachable=reachable; PathMeters=pathMeters;
        }
    }

    public sealed class KnownRecoveryFact
    {
        public string Id { get; }
        public string Name { get; }
        public string Summary { get; }
        public SalvageTargetKind Kind { get; }
        public CargoState? State { get; }
        public int Kilograms { get; }
        public int Points { get; }
        public double? PathMeters { get; }
        public bool CanSuggest => Kind==SalvageTargetKind.Cargo && State==CargoState.World && Kilograms<=8 && PathMeters.HasValue;
        internal KnownRecoveryFact(ObservedTarget observed)
        {
            if (!observed.Known) throw new ArgumentException("Only sensor-known facts.");
            SalvageTargetCatalog.TryGet(observed.Id,out var definition);
            Id=definition.Id; Name=definition.Name; Summary=definition.Summary; Kind=definition.Kind;
            State=observed.State; PathMeters=observed.PathMeters;
            if (Kind==SalvageTargetKind.Cargo) { var cargo=SalvageCatalog.Get(Id); Kilograms=cargo.Kilograms; Points=cargo.Points; }
        }
    }

    public sealed class RecoveryKnowledge
    {
        public long Epoch { get; }
        public long Revision { get; }
        public RecoveryPreference Preference { get; }
        public int DeliveredCount { get; }
        public int DeliveredPoints { get; }
        public ReadOnlyCollection<KnownRecoveryFact> Facts { get; }
        internal RecoveryKnowledge(long epoch,long revision,RecoveryPreference preference,int deliveredCount,int deliveredPoints,IEnumerable<ObservedTarget> source)
        {
            if (epoch<=0 || revision<=0 || !Enum.IsDefined(typeof(RecoveryPreference),preference)) throw new ArgumentException("Invalid session metadata.");
            if (deliveredCount<0 || deliveredCount>6 || deliveredPoints<0 || deliveredPoints>28 || (deliveredCount==0)!=(deliveredPoints==0)) throw new ArgumentException("Invalid progress.");
            var rows=source?.ToArray()??throw new ArgumentNullException(nameof(source));
            if(rows.Length>8 || rows.Any(row=>row==null) || rows.Select(row=>row.Id).Distinct(StringComparer.Ordinal).Count()!=rows.Length) throw new ArgumentException("Duplicate or excessive targets.");
            Epoch=epoch; Revision=revision; Preference=preference; DeliveredCount=deliveredCount; DeliveredPoints=deliveredPoints;
            Facts=Array.AsReadOnly(rows.Where(row=>row.Known).OrderBy(row=>row.Id,StringComparer.Ordinal).Select(row=>new KnownRecoveryFact(row)).ToArray());
        }
        public ReadOnlyCollection<KnownRecoveryFact> Suggestions(RecoveryPreference preference)
        {
            if(!Enum.IsDefined(typeof(RecoveryPreference),preference)) throw new ArgumentException("Unknown preference.");
            var allowed=Facts.Where(f=>f.CanSuggest);
            IOrderedEnumerable<KnownRecoveryFact> ranked;
            switch(preference)
            {
                case RecoveryPreference.Nearby: ranked=allowed.OrderBy(f=>f.PathMeters).ThenByDescending(f=>f.Points).ThenBy(f=>f.Id,StringComparer.Ordinal); break;
                case RecoveryPreference.HighScore: ranked=allowed.OrderByDescending(f=>f.Points).ThenBy(f=>f.PathMeters).ThenBy(f=>f.Id,StringComparer.Ordinal); break;
                default: ranked=allowed.OrderBy(f=>f.Kilograms).ThenByDescending(f=>f.Points).ThenBy(f=>f.PathMeters).ThenBy(f=>f.Id,StringComparer.Ordinal); break;
            }
            return Array.AsReadOnly(ranked.ToArray());
        }
    }
}
