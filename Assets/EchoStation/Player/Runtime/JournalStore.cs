using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EchoStation.World;

namespace EchoStation.Player
{
    public enum JournalAddResult{Added,AlreadyKnown,Conflict,CapacityReached,Invalid}
    public sealed class JournalEntry
    {
        public string Id{get;}public string Title{get;}public string FullText{get;}public string Summary{get;}public ClueKind Kind{get;}
        public JournalEntry(string id,string title,string fullText,string summary,ClueKind kind)
        {
            if(!TargetRegistry.IsValidId(id)||string.IsNullOrWhiteSpace(title)||title.Length>80||string.IsNullOrWhiteSpace(fullText)||fullText.Length>4000||string.IsNullOrWhiteSpace(summary)||summary.Length>120||summary==fullText||kind<ClueKind.Note||kind>ClueKind.Scan)throw new ArgumentException("Invalid journal record or non-distinct summary.");Id=id;Title=title;FullText=fullText;Summary=summary;Kind=kind;
        }
        public static JournalEntry FromClue(ClueRecord record){if(record==null)throw new ArgumentNullException(nameof(record));return new JournalEntry(record.Id,record.Title,record.FullText,record.Summary,record.Kind);}
        internal bool Matches(JournalEntry other)=>other!=null&&Id==other.Id&&Title==other.Title&&FullText==other.FullText&&Summary==other.Summary&&Kind==other.Kind;
    }
    [Serializable]public sealed class JournalSummary
    {
        public string id,title,summary;public ClueKind kind;
        internal JournalSummary(JournalEntry entry){id=entry.Id;title=entry.Title;summary=entry.Summary;kind=entry.Kind;}
    }
    public sealed class JournalStore
    {
        public const int MaximumEntries=128;
        private readonly Dictionary<string,JournalEntry> byId=new Dictionary<string,JournalEntry>(StringComparer.Ordinal);
        private readonly List<JournalEntry> entries=new List<JournalEntry>();private readonly ReadOnlyCollection<JournalEntry> view;
        public IReadOnlyList<JournalEntry> Entries=>view;
        public event Action<JournalEntry> Added;
        public event Action Replaced;
        // Future memory ingestion only. The prompt retrieval layer is a separate S2 responsibility.
        public event Action<JournalSummary> SummaryAdded;
        public JournalStore(){view=entries.AsReadOnly();}
        public bool TryGet(string id,out JournalEntry entry){entry=null;return id!=null&&byId.TryGetValue(id,out entry);}
        public JournalAddResult Add(JournalEntry entry)
        {
            if(entry==null)return JournalAddResult.Invalid;if(byId.TryGetValue(entry.Id,out var known))return known.Matches(entry)?JournalAddResult.AlreadyKnown:JournalAddResult.Conflict;if(entries.Count>=MaximumEntries)return JournalAddResult.CapacityReached;
            byId.Add(entry.Id,entry);entries.Add(entry);Added?.Invoke(entry);SummaryAdded?.Invoke(new JournalSummary(entry));return JournalAddResult.Added;
        }
        public JournalSummary[] ExportSummaries(){var result=new JournalSummary[entries.Count];for(var i=0;i<result.Length;i++)result[i]=new JournalSummary(entries[i]);return result;}
        internal void Replace(JournalEntry[] replacement)
        {
            if(replacement==null||replacement.Length>MaximumEntries)throw new ArgumentException("Invalid replacement journal.");var checkedIds=new HashSet<string>(StringComparer.Ordinal);foreach(var entry in replacement)if(entry==null||!checkedIds.Add(entry.Id))throw new ArgumentException("Invalid replacement journal entry.");byId.Clear();entries.Clear();foreach(var entry in replacement){byId.Add(entry.Id,entry);entries.Add(entry);}Replaced?.Invoke();
        }
    }
}
