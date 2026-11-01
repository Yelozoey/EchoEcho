using System;
using System.Collections.Generic;

namespace EchoStation.Dialogue
{
    public enum DialogueSpeaker { Player, Echo }
    public enum DialogueWriteResult { Added, Empty, TooLong, Invalid, SequenceExhausted }

    public sealed class DialogueMessage
    {
        public long Number { get; }
        public DialogueSpeaker Speaker { get; }
        public string Text { get; }
        public bool IsRefusal { get; }
        public DialogueMessage(long number, DialogueSpeaker speaker, string text, bool refusal = false)
        {
            if (number <= 0 || (uint)speaker > (uint)DialogueSpeaker.Echo || refusal && speaker != DialogueSpeaker.Echo || string.IsNullOrWhiteSpace(text) || text.Length > (speaker == DialogueSpeaker.Player ? DialogueSession.MaximumPlayerCharacters : DialogueSession.MaximumEchoCharacters)) throw new ArgumentException("Invalid dialogue message.");
            Number = number; Speaker = speaker; Text = text; IsRefusal = refusal;
        }
    }

    public sealed class DialogueSession
    {
        public const int MaximumPlayerCharacters = 200;
        public const int MaximumEchoCharacters = 280;
        public const int MaximumVisibleMessages = 100;
        public const int MaximumSavedMessages = 50;
        private readonly List<DialogueMessage> messages = new List<DialogueMessage>();
        private long lastNumber;
        public IReadOnlyList<DialogueMessage> Messages { get; }
        public ulong Version { get; private set; } = 1;
        public event Action<DialogueMessage> Added;
        public event Action Replaced;
        public DialogueSession() { Messages = messages.AsReadOnly(); }

        public DialogueWriteResult TryAdd(DialogueSpeaker speaker, string text, bool refusal, out DialogueMessage message)
        {
            message = null;
            if ((uint)speaker > (uint)DialogueSpeaker.Echo || refusal && speaker != DialogueSpeaker.Echo) return DialogueWriteResult.Invalid;
            if (string.IsNullOrWhiteSpace(text)) return DialogueWriteResult.Empty;
            if (text.Length > (speaker == DialogueSpeaker.Player ? MaximumPlayerCharacters : MaximumEchoCharacters)) return DialogueWriteResult.TooLong;
            if (lastNumber == long.MaxValue) return DialogueWriteResult.SequenceExhausted;
            message = new DialogueMessage(++lastNumber, speaker, text.Trim(), refusal);
            if (messages.Count == MaximumVisibleMessages) messages.RemoveAt(0);
            messages.Add(message); Added?.Invoke(message); return DialogueWriteResult.Added;
        }

        public IReadOnlyList<DialogueMessage> CaptureRecent()
        {
            var count = Math.Min(MaximumSavedMessages, messages.Count);
            return Array.AsReadOnly(messages.GetRange(messages.Count - count, count).ToArray());
        }

        public bool TryRestoreRecent(IReadOnlyList<DialogueMessage> recent)
            => TryRestore(recent, MaximumSavedMessages);
        internal bool TryRestoreVisibleForRollback(IReadOnlyList<DialogueMessage> recent)
            => TryRestore(recent, MaximumVisibleMessages);
        private bool TryRestore(IReadOnlyList<DialogueMessage> recent, int maximum)
        {
            if (recent == null || recent.Count > maximum || Version == ulong.MaxValue) return false;
            var copy = new DialogueMessage[recent.Count]; var previous = 0L;
            for (var i = 0; i < copy.Length; i++)
            {
                var item = recent[i];
                if (item == null || item.Number <= previous || item.Number == long.MaxValue) return false;
                copy[i] = item; previous = item.Number;
            }
            messages.Clear(); messages.AddRange(copy); lastNumber = previous; Version++;
            Replaced?.Invoke(); return true;
        }

        public void Reset()
        {
            var version = checked(Version + 1);
            messages.Clear(); lastNumber = 0; Version = version; Replaced?.Invoke();
        }
    }
}
