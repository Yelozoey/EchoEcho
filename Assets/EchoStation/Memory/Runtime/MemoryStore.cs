using System;
using System.Collections.Generic;

namespace EchoStation.Memory
{
    // Owned by the game thread; give background readers an immutable snapshot.
    public sealed class MemoryStore
    {
        private readonly Dictionary<string, EventMemory> eventsById = new Dictionary<string, EventMemory>(StringComparer.Ordinal);
        private readonly Dictionary<string, ClueMemory> cluesById = new Dictionary<string, ClueMemory>(StringComparer.Ordinal);
        private readonly Dictionary<string, CharacterMemory> charactersById = new Dictionary<string, CharacterMemory>(StringComparer.Ordinal);
        private readonly List<EventMemory> events = new List<EventMemory>();
        private readonly List<ClueMemory> clues = new List<ClueMemory>();
        private readonly List<CharacterMemory> characters = new List<CharacterMemory>();
        public IReadOnlyList<EventMemory> Events { get; }
        public IReadOnlyList<ClueMemory> Clues { get; }
        public IReadOnlyList<CharacterMemory> Characters { get; }
        public event Action<MemoryChange> Changed;

        public MemoryStore()
        {
            Events = events.AsReadOnly();
            Clues = clues.AsReadOnly();
            Characters = characters.AsReadOnly();
        }

        public MemoryWriteResult AddEvent(string id, double elapsedGameSeconds, string summary, MemoryPriority priority = MemoryPriority.Normal)
        {
            EventMemory candidate;
            try { candidate = new EventMemory(id, events.Count + 1L, elapsedGameSeconds, summary, priority); }
            catch (ArgumentException) { return MemoryWriteResult.Invalid; }
            if (eventsById.TryGetValue(id, out var previous)) return previous.Matches(candidate) ? MemoryWriteResult.AlreadyKnown : MemoryWriteResult.Conflict;
            if (events.Count >= MemoryLimits.MaximumEvents) return MemoryWriteResult.CapacityReached;
            eventsById.Add(id, candidate);
            events.Add(candidate);
            Publish(MemoryCategory.Event, MemoryWriteResult.Added, id);
            return MemoryWriteResult.Added;
        }

        public MemoryWriteResult AddClue(ClueMemory record)
        {
            if (record == null) return MemoryWriteResult.Invalid;
            if (cluesById.TryGetValue(record.Id, out var previous)) return previous.Matches(record) ? MemoryWriteResult.AlreadyKnown : MemoryWriteResult.Conflict;
            if (clues.Count >= MemoryLimits.MaximumClues) return MemoryWriteResult.CapacityReached;
            cluesById.Add(record.Id, record);
            clues.Add(record);
            Publish(MemoryCategory.Clue, MemoryWriteResult.Added, record.Id);
            return MemoryWriteResult.Added;
        }

        public MemoryWriteResult AddCharacter(CharacterMemory record)
        {
            if (record == null) return MemoryWriteResult.Invalid;
            if (charactersById.TryGetValue(record.Id, out var previous)) return previous.Matches(record) ? MemoryWriteResult.AlreadyKnown : MemoryWriteResult.Conflict;
            if (characters.Count >= MemoryLimits.MaximumCharacterStates) return MemoryWriteResult.CapacityReached;
            charactersById.Add(record.Id, record);
            characters.Add(record);
            Publish(MemoryCategory.Character, MemoryWriteResult.Added, record.Id);
            return MemoryWriteResult.Added;
        }

        public MemoryWriteResult UpdateCharacter(CharacterMemory replacement)
        {
            if (replacement == null) return MemoryWriteResult.Invalid;
            if (!charactersById.TryGetValue(replacement.Id, out var previous)) return MemoryWriteResult.NotFound;
            if (previous.Matches(replacement)) return MemoryWriteResult.AlreadyKnown;
            var index = characters.IndexOf(previous);
            characters[index] = replacement;
            charactersById[replacement.Id] = replacement;
            Publish(MemoryCategory.Character, MemoryWriteResult.Updated, replacement.Id);
            return MemoryWriteResult.Updated;
        }

        public bool TryGetEvent(string id, out EventMemory record)
        { record = null; return id != null && eventsById.TryGetValue(id, out record); }
        public bool TryGetClue(string id, out ClueMemory record)
        { record = null; return id != null && cluesById.TryGetValue(id, out record); }
        public bool TryGetCharacter(string id, out CharacterMemory record)
        { record = null; return id != null && charactersById.TryGetValue(id, out record); }

        public MemorySnapshot CaptureSnapshot() => new MemorySnapshot(events.ToArray(), clues.ToArray(), characters.ToArray());

        private void Publish(MemoryCategory category, MemoryWriteResult result, string id)
            => Changed?.Invoke(new MemoryChange(category, result, id));
    }
}
