using System;

namespace EchoStation.Memory
{
    public static class MemoryLimits
    {
        public const int MaximumEvents = 512;
        public const int MaximumClues = 128;
        public const int MaximumCharacterStates = 64;
        public const int MaximumIdCharacters = 64;
        public const int MaximumSummaryCharacters = 120;
        public const int MaximumTitleCharacters = 80;

        // Same stable-ID grammar as the world registry, without a Unity dependency.
        public static bool IsValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > MaximumIdCharacters || id[0] < 'a' || id[0] > 'z') return false;
            for (var i = 1; i < id.Length; i++)
            {
                var c = id[i];
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_')) return false;
            }
            return true;
        }

        internal static string Id(string value, string name)
        {
            if (!IsValidId(value)) throw new ArgumentException("Invalid stable memory ID.", name);
            return value;
        }

        internal static string Text(string value, int maximum, string name)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > maximum) throw new ArgumentException("Invalid bounded memory text.", name);
            return value;
        }

        internal static string OptionalText(string value, string name)
            => string.IsNullOrEmpty(value) ? null : Text(value, MaximumSummaryCharacters, name);

        internal static string OptionalId(string value, string name)
            => string.IsNullOrEmpty(value) ? null : Id(value, name);

        internal static void Time(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentException("Invalid elapsed game time.", nameof(seconds));
        }
    }
}
