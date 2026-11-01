using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Memory;

namespace EchoStation.Dialogue.Knowledge
{
    public sealed class MemoryFactRequirement
    {
        public MemoryCategory Category { get; }
        public string Id { get; }
        public MemoryFactRequirement(MemoryCategory category, string id)
        {
            if ((uint)category > (uint)MemoryCategory.Character || !MemoryLimits.IsValidId(id)) throw new ArgumentException("Invalid required fact.");
            Category = category; Id = id;
        }
        internal bool Present(MemorySnapshot known)
            => Category == MemoryCategory.Event ? known.Events.Any(e => e.Id == Id) : Category == MemoryCategory.Clue ? known.Clues.Any(c => c.Id == Id) : known.Characters.Any(c => c.Id == Id);
    }

    public sealed class HintPlan
    {
        public string Id { get; }
        public string ZoneId { get; }
        public string CompletedEventId { get; }
        public string RetrievalQuery { get; }
        public IReadOnlyList<MemoryFactRequirement> Required { get; }
        public IReadOnlyList<string> Levels { get; }
        public HintPlan(string id, string zoneId, string completedEventId, string retrievalQuery, MemoryFactRequirement[] required, params string[] levels)
        {
            if (!MemoryLimits.IsValidId(id) || zoneId != null && !MemoryLimits.IsValidId(zoneId) || completedEventId != null && !MemoryLimits.IsValidId(completedEventId) || string.IsNullOrWhiteSpace(retrievalQuery) || retrievalQuery.Length > 200 || required == null || required.Length > 8 || required.Any(r => r == null) || levels == null || levels.Length != 3 || levels.Any(l => string.IsNullOrWhiteSpace(l) || l.Length > 280)) throw new ArgumentException("Invalid bounded hint plan.");
            Id = id; ZoneId = zoneId; CompletedEventId = completedEventId; RetrievalQuery = retrievalQuery;
            Required = Array.AsReadOnly((MemoryFactRequirement[])required.Clone()); Levels = Array.AsReadOnly((string[])levels.Clone());
        }
    }
}
