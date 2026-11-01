using System;
using System.Collections.Generic;
using System.Globalization;

namespace EchoStation.Memory
{
    public enum MemoryPriority { Low, Normal, Critical }
    public enum MemoryClueKind { Note, Data, Scan }
    public enum CharacterMemoryKind { KnownDanger, Agreement, PlayerKnowledge }
    public enum MemoryCategory { Event, Clue, Character }
    public enum MemoryWriteResult { Added, AlreadyKnown, Conflict, CapacityReached, Invalid, Updated, NotFound }

    public abstract class MemoryRecord
    {
        public string Id { get; }
        public string Summary { get; }

        private protected MemoryRecord(string id, string summary)
        {
            Id = MemoryLimits.Id(id, nameof(id));
            Summary = MemoryLimits.Text(summary, MemoryLimits.MaximumSummaryCharacters, nameof(summary));
        }
    }

    public sealed class EventMemory : MemoryRecord
    {
        public long Number { get; }
        public double ElapsedGameSeconds { get; }
        public MemoryPriority Priority { get; }
        public string DisplayText => "[" + Number.ToString(CultureInfo.InvariantCulture) + "] " + Summary;

        internal EventMemory(string id, long number, double seconds, string summary, MemoryPriority priority) : base(id, summary)
        {
            if (number <= 0) throw new ArgumentException("Event numbers must be positive.", nameof(number));
            MemoryLimits.Time(seconds);
            if ((uint)priority > (uint)MemoryPriority.Critical) throw new ArgumentException("Invalid memory priority.", nameof(priority));
            Number = number;
            ElapsedGameSeconds = seconds;
            Priority = priority;
        }

        internal bool Matches(EventMemory other)
            => Id == other.Id && Summary == other.Summary && ElapsedGameSeconds == other.ElapsedGameSeconds && Priority == other.Priority;
    }

    public sealed class ClueMemory : MemoryRecord
    {
        public string Title { get; }
        public MemoryClueKind Kind { get; }
        public string Password { get; }
        public string KeyMarker { get; }

        public ClueMemory(string id, string title, string summary, MemoryClueKind kind, string password = null, string keyMarker = null) : base(id, summary)
        {
            Title = MemoryLimits.Text(title, MemoryLimits.MaximumTitleCharacters, nameof(title));
            if ((uint)kind > (uint)MemoryClueKind.Scan) throw new ArgumentException("Invalid clue kind.", nameof(kind));
            Kind = kind;
            Password = MemoryLimits.OptionalText(password, nameof(password));
            KeyMarker = MemoryLimits.OptionalText(keyMarker, nameof(keyMarker));
        }

        internal bool Matches(ClueMemory other)
            => Id == other.Id && Title == other.Title && Summary == other.Summary && Kind == other.Kind && Password == other.Password && KeyMarker == other.KeyMarker;
    }

    public sealed class CharacterMemory : MemoryRecord
    {
        public CharacterMemoryKind Kind { get; }
        public string TargetId { get; }

        public CharacterMemory(string id, CharacterMemoryKind kind, string summary, string targetId = null) : base(id, summary)
        {
            if ((uint)kind > (uint)CharacterMemoryKind.PlayerKnowledge) throw new ArgumentException("Invalid character memory kind.", nameof(kind));
            Kind = kind;
            TargetId = MemoryLimits.OptionalId(targetId, nameof(targetId));
        }

        internal bool Matches(CharacterMemory other)
            => Id == other.Id && Summary == other.Summary && Kind == other.Kind && TargetId == other.TargetId;
    }

    public readonly struct MemoryChange
    {
        public MemoryCategory Category { get; }
        public MemoryWriteResult Result { get; }
        public string Id { get; }

        internal MemoryChange(MemoryCategory category, MemoryWriteResult result, string id)
        { Category = category; Result = result; Id = id; }
    }

    public sealed class MemorySnapshot
    {
        public IReadOnlyList<EventMemory> Events { get; }
        public IReadOnlyList<ClueMemory> Clues { get; }
        public IReadOnlyList<CharacterMemory> Characters { get; }

        internal MemorySnapshot(EventMemory[] events, ClueMemory[] clues, CharacterMemory[] characters)
        {
            Events = Array.AsReadOnly(events);
            Clues = Array.AsReadOnly(clues);
            Characters = Array.AsReadOnly(characters);
        }
    }
}
