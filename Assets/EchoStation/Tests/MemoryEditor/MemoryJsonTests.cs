using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using EchoStation.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace EchoStation.Tests.Memory
{
    public sealed class MemoryJsonTests
    {
        private static MemoryStore Sample()
        {
            var store = new MemoryStore();
            store.AddEvent("event_first", 0, "进入\"小屋\"。\n记录已读取。", MemoryPriority.Critical);
            store.AddEvent("event_second", 12.25, "扫描完成。", MemoryPriority.Low);
            store.AddClue(new ClueMemory("clue_note", "维护记录", "已知密码与标记。", MemoryClueKind.Scan, "7316", "A区校准架"));
            store.AddClue(new ClueMemory("clue_data", "数据", "独立摘要", MemoryClueKind.Data));
            store.AddCharacter(new CharacterMemory("state_danger", CharacterMemoryKind.KnownDanger, "保持安全距离。", "power_panel_01"));
            store.AddCharacter(new CharacterMemory("state_agreement", CharacterMemoryKind.Agreement, "原地等待。"));
            store.AddCharacter(new CharacterMemory("state_player", CharacterMemoryKind.PlayerKnowledge, "玩家已读维护记录。"));
            return store;
        }
        private static string Encode(MemoryStore store = null) => MemoryJson.Encode((store ?? Sample()).CaptureSnapshot());
        private static string Changed(Action<JObject> change)
        {
            var json = JObject.Parse(Encode()); change(json);
            using (var sha = SHA256.Create()) json["sha256"] = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json["payload"].ToString(Formatting.None)))).Replace("-", "").ToLowerInvariant();
            return json.ToString(Formatting.None);
        }
        private static void Invalid(string json)
        {
            var result = MemoryJson.Decode(json);
            Assert.That(result.Status, Is.EqualTo(MemoryReadStatus.Invalid), result.Detail);
            Assert.That(result.Store, Is.Null);
        }

        [Test] public void M02_T01_ThreeCategoriesUnicodeAndOptionalFactsRoundTrip()
        {
            var text = Encode(); var read = MemoryJson.Decode(text);
            Assert.That(read.Success, Is.True, read.Detail);
            Assert.That(read.Store.Events.Count, Is.EqualTo(2)); Assert.That(read.Store.Events[0].Summary, Does.Contain("\"小屋\"。\n"));
            Assert.That(read.Store.Events[1].ElapsedGameSeconds, Is.EqualTo(12.25));
            Assert.That(read.Store.Clues[0].Password, Is.EqualTo("7316")); Assert.That(read.Store.Clues[0].KeyMarker, Is.EqualTo("A区校准架"));
            Assert.That(read.Store.Clues[1].Password, Is.Null); Assert.That(read.Store.Characters[1].TargetId, Is.Null);
            Assert.That(MemoryJson.Encode(read.Store.CaptureSnapshot()), Is.EqualTo(text));
            Assert.That(text, Does.Not.Contain("FullText")); Assert.That(text, Does.Not.Contain("$type"));
        }
        [Test] public void M02_T02_EmptyStoreRoundTripsAndNullEncodeRejects()
        {
            var read = MemoryJson.Decode(Encode(new MemoryStore()));
            Assert.That(read.Success, Is.True, read.Detail); Assert.That(read.Store.Events, Is.Empty); Assert.That(read.Store.Clues, Is.Empty); Assert.That(read.Store.Characters, Is.Empty);
            Assert.Throws<ArgumentNullException>(() => MemoryJson.Encode(null));
        }
        [Test] public void M02_T03_ChecksumDetectsModifiedPayload()
        {
            Invalid(Encode().Replace("7316", "9999"));
            var root = JObject.Parse(Encode()); root["sha256"] = "wrong"; Invalid(root.ToString());
        }
        [Test] public void M02_T04_MissingOrUnknownFieldsAreRejectedAtEveryLevel()
        {
            foreach (var level in new[] { "root", "payload", "event", "clue", "character" })
            {
                foreach (var remove in new[] { false, true })
                    Invalid(Changed(root =>
                    {
                        var obj = level == "root" ? root : level == "payload" ? (JObject)root["payload"] : (JObject)root["payload"][level == "event" ? "events" : level == "clue" ? "clues" : "characters"][0];
                        if (remove) obj.Property(level == "root" ? "format" : level == "payload" ? "events" : "id").Remove(); else obj["unexpected"] = 1;
                    }));
            }
        }
        [Test] public void M02_T05_DuplicatePropertiesAreRejectedIncludingNestedRecords()
        {
            Invalid(Encode().Replace("\"version\":1", "\"version\":1,\"version\":1"));
            Invalid(Encode().Replace("\"id\":\"event_first\"", "\"id\":\"event_first\",\"id\":\"event_first\""));
        }
        [Test] public void M02_T06_InvalidTypesAndEnumsCannotBecomeDefaultValues()
        {
            foreach (var value in new JToken[] { JValue.CreateNull(), new JValue("0"), new JValue(-1), new JValue(true) })
                Invalid(Changed(r => r["payload"]["events"][0]["time"] = value.DeepClone()));
            Invalid(Changed(r => r["payload"]["events"][0]["priority"] = "Critical"));
            Invalid(Changed(r => r["payload"]["clues"][0]["kind"] = 0));
            Invalid(Changed(r => r["payload"]["characters"][0]["targetId"] = "Bad target"));
            Invalid(Changed(r => r["payload"]["clues"][0]["summary"] = new string('摘', 121)));
            Invalid(Changed(r => r["payload"]["events"] = new JObject()));
        }
        [Test] public void M02_T07_SequenceGapsAndRepeatedIdsAreRejected()
        {
            Invalid(Changed(r => r["payload"]["events"][0]["number"] = 0));
            Invalid(Changed(r => r["payload"]["events"][1]["number"] = 3));
            Invalid(Changed(r => r["payload"]["events"][1]["id"] = "event_first"));
            Invalid(Changed(r => ((JArray)r["payload"]["clues"]).Add(r["payload"]["clues"][0].DeepClone())));
            Invalid(Changed(r => ((JArray)r["payload"]["characters"]).Add(r["payload"]["characters"][0].DeepClone())));
        }
        [Test] public void M02_T08_FutureVersionsAreRecognizedBeforeUnknownFutureFields()
        {
            foreach (var version in new[] { "2", "999999999999999999999999999999" })
            {
                var json = Encode().Replace("\"version\":1", "\"version\":" + version + ",\"newFutureField\":true");
                Assert.That(MemoryJson.Decode(json).Status, Is.EqualTo(MemoryReadStatus.UnsupportedVersion));
            }
            Invalid(Encode().Replace("\"version\":1", "\"version\":0"));
            Invalid(Encode().Replace("\"version\":1", "\"version\":\"1\""));
        }
        [Test] public void M02_T09_NonJsonAndPermissiveJavascriptSyntaxAreRejected()
        {
            foreach (var json in new[] { null, "", " ", "not json", "[]", Encode() + "{}", Encode().Replace("\"version\":1", "\"version\":01"), Encode().Replace("\"version\":1", "\"version\":1,"), Encode().Replace("\"format\"", "format"), Encode().Replace("\"format\"", "'format'"), "/*comment*/" + Encode(), Encode().Replace("\"time\":0.0", "\"time\":NaN") })
                Invalid(json);
        }
        [Test] public void M02_T10_ByteAndDepthBudgetsRejectBeforeReturningData()
        {
            Invalid(new string(' ', MemoryJson.MaximumBytes + 1));
            Invalid(new string('[', 20) + "0" + new string(']', 20));
            Invalid("\"" + new string('中', MemoryJson.MaximumBytes / 3 + 1) + "\"");
        }
        [Test] public void M02_T11_MaximumCapacityRoundTripsAndOverflowRejects()
        {
            var store = new MemoryStore();
            for (var i = 0; i < 512; i++) store.AddEvent("event_" + i, i, new string('摘', 120));
            for (var i = 0; i < 128; i++) store.AddClue(new ClueMemory("clue_" + i, new string('题', 80), new string('摘', 120), MemoryClueKind.Note, new string('密', 120), new string('标', 120)));
            for (var i = 0; i < 64; i++) store.AddCharacter(new CharacterMemory("state_" + i, CharacterMemoryKind.PlayerKnowledge, new string('摘', 120)));
            var read = MemoryJson.Decode(Encode(store)); Assert.That(read.Success, Is.True, read.Detail);
            Assert.That(read.Store.Events.Count, Is.EqualTo(512)); Assert.That(read.Store.Clues.Count, Is.EqualTo(128)); Assert.That(read.Store.Characters.Count, Is.EqualTo(64));
            var root = JObject.Parse(Encode(store)); ((JArray)root["payload"]["events"]).Add(root["payload"]["events"][0].DeepClone());
            using (var sha = SHA256.Create()) root["sha256"] = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(root["payload"].ToString(Formatting.None)))).Replace("-", "").ToLowerInvariant();
            Invalid(root.ToString());
        }
        [Test] public void M02_T12_FailedDecodeNeverMutatesExistingStoreAndNoExternalTypesExecute()
        {
            var original = Sample(); var before = Encode(original);
            Invalid(Changed(r => r["payload"]["characters"][2]["kind"] = "injected"));
            Invalid(Changed(r => r["payload"]["$type"] = "Arbitrary.External.Type"));
            Assert.That(Encode(original), Is.EqualTo(before));
            var decoded = MemoryJson.Decode(before).Store; decoded.UpdateCharacter(new CharacterMemory("state_player", CharacterMemoryKind.PlayerKnowledge, "新摘要"));
            Assert.That(Encode(original), Is.EqualTo(before));
        }
    }
}
