using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EchoStation.Memory
{
    public enum MemoryReadStatus { Loaded, Backup, Missing, Invalid, UnsupportedVersion, IoError }

    public sealed class MemoryReadResult
    {
        public MemoryReadStatus Status { get; }
        public MemoryStore Store { get; }
        public string Detail { get; }
        public bool Success => Status == MemoryReadStatus.Loaded || Status == MemoryReadStatus.Backup;
        public MemoryReadResult(MemoryReadStatus status, MemoryStore store = null, string detail = "")
        { Status = status; Store = store; Detail = detail; }
    }

    public static class MemoryJson
    {
        public const int MaximumBytes = 2 * 1024 * 1024;
        public const string Format = "echostation.memory";
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly string[] Priorities = { "low", "normal", "critical" };
        private static readonly string[] ClueKinds = { "note", "data", "scan" };
        private static readonly string[] CharacterKinds = { "danger", "agreement", "player_knowledge" };
        private static readonly Regex Number = new Regex(@"\A-?(?:0|[1-9][0-9]*)(?:\.[0-9]+)?(?:[eE][+-]?[0-9]+)?\z", RegexOptions.CultureInvariant);

        public static string Encode(MemorySnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var events = new JArray(); var clues = new JArray(); var characters = new JArray();
            foreach (var e in snapshot.Events)
                events.Add(new JObject { ["id"] = e.Id, ["number"] = e.Number, ["time"] = e.ElapsedGameSeconds, ["summary"] = e.Summary, ["priority"] = Priorities[(int)e.Priority] });
            foreach (var c in snapshot.Clues)
                clues.Add(new JObject { ["id"] = c.Id, ["title"] = c.Title, ["summary"] = c.Summary, ["kind"] = ClueKinds[(int)c.Kind], ["password"] = c.Password, ["marker"] = c.KeyMarker });
            foreach (var c in snapshot.Characters)
                characters.Add(new JObject { ["id"] = c.Id, ["kind"] = CharacterKinds[(int)c.Kind], ["summary"] = c.Summary, ["targetId"] = c.TargetId });
            var payload = new JObject { ["events"] = events, ["clues"] = clues, ["characters"] = characters };
            var envelope = new JObject { ["format"] = Format, ["version"] = 1, ["payload"] = payload, ["sha256"] = Hash(payload.ToString(Formatting.None)) };
            var json = envelope.ToString(Formatting.None);
            if (Utf8.GetByteCount(json) > MaximumBytes) throw new ArgumentException("Memory document exceeds byte budget.");
            return json;
        }

        public static MemoryReadResult Decode(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json) || Utf8.GetByteCount(json) > MaximumBytes) return Invalid("Memory document empty or too large.");
                ValidateLexemes(json);
                JObject root;
                using (var reader = new JsonTextReader(new StringReader(json)) { MaxDepth = 16, DateParseHandling = DateParseHandling.None })
                {
                    root = JToken.ReadFrom(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }) as JObject;
                    if (root == null || reader.Read()) return Invalid("Memory document must contain exactly one object.");
                }
                if (Text(root, "format") != Format) return Invalid("Unknown memory format.");
                var version = root["version"];
                if (version == null || version.Type != JTokenType.Integer) return Invalid("Invalid memory version.");
                var versionText = version.ToString(Formatting.None);
                if (versionText != "1")
                    return versionText != "0" && !versionText.StartsWith("-", StringComparison.Ordinal)
                        ? new MemoryReadResult(MemoryReadStatus.UnsupportedVersion, detail: "Future memory version; preserve source.")
                        : Invalid("Invalid memory version.");
                Fields(root, "format", "version", "payload", "sha256");
                var data = Object(root["payload"]);
                if (Text(root, "sha256") != Hash(data.ToString(Formatting.None))) return Invalid("Memory checksum mismatch.");
                Fields(data, "events", "clues", "characters");
                var events = Array(data, "events", MemoryLimits.MaximumEvents);
                var clues = Array(data, "clues", MemoryLimits.MaximumClues);
                var characters = Array(data, "characters", MemoryLimits.MaximumCharacterStates);
                var candidate = new MemoryStore();
                for (var i = 0; i < events.Count; i++)
                {
                    var e = Object(events[i]); Fields(e, "id", "number", "time", "summary", "priority");
                    if (e["number"].Type != JTokenType.Integer || e["number"].Value<long>() != i + 1L) return Invalid("Event sequence is not continuous.");
                    if (e["time"].Type != JTokenType.Integer && e["time"].Type != JTokenType.Float) return Invalid("Event time must be numeric.");
                    RequireAdded(candidate.AddEvent(Text(e, "id"), e["time"].Value<double>(), Text(e, "summary"), (MemoryPriority)EnumIndex(Priorities, Text(e, "priority"))));
                }
                foreach (var token in clues)
                {
                    var c = Object(token); Fields(c, "id", "title", "summary", "kind", "password", "marker");
                    RequireAdded(candidate.AddClue(new ClueMemory(Text(c, "id"), Text(c, "title"), Text(c, "summary"), (MemoryClueKind)EnumIndex(ClueKinds, Text(c, "kind")), Optional(c, "password"), Optional(c, "marker"))));
                }
                foreach (var token in characters)
                {
                    var c = Object(token); Fields(c, "id", "kind", "summary", "targetId");
                    RequireAdded(candidate.AddCharacter(new CharacterMemory(Text(c, "id"), (CharacterMemoryKind)EnumIndex(CharacterKinds, Text(c, "kind")), Text(c, "summary"), Optional(c, "targetId"))));
                }
                return new MemoryReadResult(MemoryReadStatus.Loaded, candidate);
            }
            catch (Exception error) when (error is JsonException || error is ArgumentException || error is OverflowException || error is FormatException)
            { return Invalid(error.Message); }
        }

        private static MemoryReadResult Invalid(string reason) => new MemoryReadResult(MemoryReadStatus.Invalid, detail: reason);
        private static JObject Object(JToken value) => value as JObject ?? throw new ArgumentException("Object required.");
        private static void Fields(JObject value, params string[] names)
        {
            if (value.Count != names.Length || names.Any(n => value.Property(n, StringComparison.Ordinal) == null)) throw new ArgumentException("Missing or unknown JSON field.");
        }
        private static JArray Array(JObject parent, string name, int maximum)
        {
            var value = parent[name] as JArray;
            if (value == null || value.Count > maximum) throw new ArgumentException("Invalid memory array or capacity.");
            return value;
        }
        private static string Text(JObject parent, string name)
        {
            var value = parent[name];
            if (value == null || value.Type != JTokenType.String) throw new ArgumentException("String required: " + name);
            return value.Value<string>();
        }
        private static string Optional(JObject parent, string name)
            => parent[name].Type == JTokenType.Null ? null : Text(parent, name);
        private static int EnumIndex(string[] values, string value)
        {
            var index = System.Array.IndexOf(values, value);
            if (index < 0) throw new ArgumentException("Unknown memory enum.");
            return index;
        }
        private static void RequireAdded(MemoryWriteResult result)
        {
            if (result != MemoryWriteResult.Added) throw new ArgumentException("Invalid or repeated persisted record: " + result);
        }
        private static string Hash(string value)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Utf8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }

        // Json.NET also accepts JavaScript syntax. Persisted files allow JSON lexemes only.
        public static void ValidateLexemes(string text)
        {
            var previous = '\0';
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n') continue;
                if (c == '"')
                {
                    var closed = false;
                    while (++i < text.Length)
                    {
                        c = text[i];
                        if (c < 32) throw new ArgumentException("Unescaped control character.");
                        if (c == '"') { closed = true; break; }
                        if (c != '\\') continue;
                        if (++i >= text.Length) throw new ArgumentException("Unfinished JSON escape.");
                        c = text[i];
                        if (c == 'u')
                        {
                            for (var j = 0; j < 4; j++)
                                if (++i >= text.Length || !Uri.IsHexDigit(text[i])) throw new ArgumentException("Invalid Unicode escape.");
                        }
                        else if ("\"\\/bfnrt".IndexOf(c) < 0) throw new ArgumentException("Invalid JSON escape.");
                    }
                    if (!closed) throw new ArgumentException("Unfinished JSON string.");
                    previous = '"'; continue;
                }
                if (c == '-' || (c >= '0' && c <= '9') || (c >= 'a' && c <= 'z'))
                {
                    var start = i;
                    while (i + 1 < text.Length && ",]}:{[ \t\r\n".IndexOf(text[i + 1]) < 0) i++;
                    var word = text.Substring(start, i - start + 1);
                    if (word != "true" && word != "false" && word != "null" && !Number.IsMatch(word)) throw new ArgumentException("Invalid JSON literal.");
                    previous = 'v'; continue;
                }
                if ("{}[],:".IndexOf(c) < 0 || ((c == '}' || c == ']') && previous == ',')) throw new ArgumentException("Invalid JSON punctuation or trailing comma.");
                previous = c;
            }
        }
    }
}
