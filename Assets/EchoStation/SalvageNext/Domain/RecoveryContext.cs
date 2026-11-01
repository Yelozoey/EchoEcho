using System;
using System.Globalization;
using System.Linq;
using EchoStation.AI.Context;
using EchoStation.Memory;
using EchoStation.Salvage;

namespace EchoStation.SalvageNext
{
    public static class RecoveryContext
    {
        public static MemorySearch Search() => new MemorySearch(new[]{
            new MemoryKeywordGroup("回收","建议","推荐","物资","轻量","高分","就近"),
            new MemoryKeywordGroup("偏好","优先","约定","建议","推荐"),
            new MemoryKeywordGroup("危险","警告","故障","泄漏"),
            new MemoryKeywordGroup("进度","交付","完成","结算")},
            SalvageTargetCatalog.Items.Select(t=>new MemoryAlias(MemoryCategory.Clue,t.Id,new[]{t.Name,t.Kind==SalvageTargetKind.Cargo?t.Name.Substring(2):t.Name,t.Category,t.Id}.Distinct(StringComparer.Ordinal).ToArray())));

        public static InferenceSnapshot Snapshot(RecoveryKnowledge known,string input,double elapsed,string pointedId=null)
        {
            if(known==null)throw new ArgumentNullException(nameof(known));
            // Input is never copied into a memory record. Memory is rebuilt solely from trusted facts.
            var memory=new MemoryStore();
            memory.AddCharacter(new CharacterMemory("recovery_preference",CharacterMemoryKind.Agreement,"回收建议偏好："+PreferenceName(known.Preference)+"；仅建议，需玩家确认。"));
            memory.AddCharacter(new CharacterMemory("recovery_progress",CharacterMemoryKind.PlayerKnowledge,"交付进度："+known.DeliveredCount+"件／"+known.DeliveredPoints+"分；至少3件且12分，可主动结算。"));
            foreach(RecoveryPreference preference in Enum.GetValues(typeof(RecoveryPreference)))
            {
                var choice=known.Suggestions(preference).FirstOrDefault();
                var summary="回收建议（"+PreferenceName(preference)+"）："+(choice==null?"没有已发现且可搬运可达的目标。":"首选"+choice.Id+"（"+choice.Name+"，"+choice.Kilograms+"kg／"+choice.Points+"分）；需玩家确认后才调查。");
                memory.AddCharacter(new CharacterMemory("suggest_"+preference.ToString().ToLowerInvariant(),CharacterMemoryKind.Agreement,summary));
            }
            foreach(var fact in known.Facts)
            {
                string details=fact.Kind==SalvageTargetKind.Cargo ? "；回收物资 "+fact.Kilograms+"kg／"+fact.Points+"分；状态"+fact.State+
                    (fact.PathMeters.HasValue?"；路线"+fact.PathMeters.Value.ToString("0.0",CultureInfo.InvariantCulture)+"m":"；当前不可达或不在场") : "；危险对象";
                memory.AddClue(new ClueMemory(fact.Id,fact.Name,fact.Summary+details,MemoryClueKind.Scan));
            }
            var targets=known.Facts.Where(f=>f.PathMeters.HasValue&&(f.Kind==SalvageTargetKind.Danger||f.State==CargoState.World))
                .Select(f=>new TargetCandidate(f.Id,f.Name,"salvage",true,true,f.Kind==SalvageTargetKind.Danger,false,f.PathMeters.Value,true)).ToList();
            if(pointedId!=null&&!targets.Any(t=>t.Id==pointedId)&&!known.Facts.Any(f=>f.Id==pointedId))
            {
                if(!SalvageTargetCatalog.TryGet(pointedId,out var target))throw new ArgumentException("Invalid pointed target.");
                // Identity and allowed sensor action only; no unknown summary, location or cargo metadata.
                targets.Add(new TargetCandidate(target.Id,target.Name,"salvage",true,true,target.Kind==SalvageTargetKind.Danger,true,0));
            }
            return new InferenceSnapshot(memory,input,elapsed,"salvage",targets);
        }
        public static string PreferenceName(RecoveryPreference p)
        {
            switch(p){case RecoveryPreference.Nearby:return "就近";case RecoveryPreference.HighScore:return "高分";case RecoveryPreference.Lightweight:return "轻量";default:throw new ArgumentException("Unknown preference.");}
        }
    }
}
