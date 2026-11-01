using System;

namespace EchoStation.Memory
{
    public sealed class MemorySession
    {
        public MemoryStore Store { get; private set; } = new MemoryStore();
        public ulong Version { get; private set; } = 1;
        public MemoryReadStatus LastRestoreStatus { get; private set; } = MemoryReadStatus.Missing;
        public event Action Replaced;
        public string CaptureJson() => MemoryJson.Encode(Store.CaptureSnapshot());

        public bool TryRestore(string json, out string error, Func<MemorySnapshot, bool> accept = null)
            => TryAdopt(MemoryJson.Decode(json), out error, accept);

        public bool TryAdopt(MemoryReadResult read, out string error, Func<MemorySnapshot, bool> accept = null)
        {
            error = "";
            if (read == null || !read.Success || read.Store == null)
            {
                LastRestoreStatus = read != null && !read.Success ? read.Status : MemoryReadStatus.Invalid;
                error = read?.Detail ?? "没有可恢复记忆。"; return false;
            }
            MemoryStore candidate; ulong next;
            try
            {
                candidate = Clone(read.Store.CaptureSnapshot());
                if (accept != null && !accept(candidate.CaptureSnapshot()))
                { LastRestoreStatus = MemoryReadStatus.Invalid; error = "记忆与待恢复进度不一致。"; return false; }
                next = checked(Version + 1);
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException || exception is OverflowException)
            { LastRestoreStatus = MemoryReadStatus.Invalid; error = exception.Message; return false; }
            Store = candidate; Version = next; LastRestoreStatus = read.Status;
            Replaced?.Invoke(); return true;
        }

        public void Reset()
        {
            var next = checked(Version + 1);
            Store = new MemoryStore(); Version = next; LastRestoreStatus = MemoryReadStatus.Missing;
            Replaced?.Invoke();
        }

        private static MemoryStore Clone(MemorySnapshot snapshot)
        {
            var store = new MemoryStore(); var number = 0L;
            foreach (var item in snapshot.Events)
            {
                if (item.Number != ++number) throw new ArgumentException("Invalid restored event order.");
                RequireAdded(store.AddEvent(item.Id, item.ElapsedGameSeconds, item.Summary, item.Priority));
            }
            foreach (var item in snapshot.Clues) RequireAdded(store.AddClue(item));
            foreach (var item in snapshot.Characters) RequireAdded(store.AddCharacter(item));
            return store;
        }
        private static void RequireAdded(MemoryWriteResult result)
        { if (result != MemoryWriteResult.Added) throw new ArgumentException("Invalid restored memory record."); }
    }
}
