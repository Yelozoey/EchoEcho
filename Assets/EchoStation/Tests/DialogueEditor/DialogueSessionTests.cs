using System;
using System.Collections.Generic;
using System.Linq;
using EchoStation.Dialogue;
using NUnit.Framework;

namespace EchoStation.Tests.Dialogue
{
    public sealed class DialogueSessionTests
    {
        [Test] public void D01_T01_PlayerEchoAndRefusalKeepStableOrderAndLiteralText()
        {
            var session = new DialogueSession(); var changes = 0; session.Added += _ => changes++;
            Assert.That(session.TryAdd(DialogueSpeaker.Player, "  <b>调查储藏室</b>  ", false, out var player), Is.EqualTo(DialogueWriteResult.Added));
            Assert.That(session.TryAdd(DialogueSpeaker.Echo, "我前往指定位置扫描。", false, out _), Is.EqualTo(DialogueWriteResult.Added));
            Assert.That(session.TryAdd(DialogueSpeaker.Echo, "我无法代替你操作机关。", true, out var refusal), Is.EqualTo(DialogueWriteResult.Added));
            Assert.That(player.Text, Is.EqualTo("<b>调查储藏室</b>")); Assert.That(refusal.IsRefusal, Is.True); Assert.That(changes, Is.EqualTo(3));
            CollectionAssert.AreEqual(new long[] { 1, 2, 3 }, session.Messages.Select(m => m.Number));
        }
        [Test] public void D01_T02_EmptyAndOversizedInputsRejectWithoutNotificationOrNumberGap()
        {
            var session = new DialogueSession(); var changes = 0; session.Added += _ => changes++;
            foreach (var text in new[] { null, "", " \t\n" }) Assert.That(session.TryAdd(DialogueSpeaker.Player, text, false, out _), Is.EqualTo(DialogueWriteResult.Empty));
            Assert.That(session.TryAdd(DialogueSpeaker.Player, new string('字', 201), false, out _), Is.EqualTo(DialogueWriteResult.TooLong));
            Assert.That(session.TryAdd(DialogueSpeaker.Echo, new string('字', 281), false, out _), Is.EqualTo(DialogueWriteResult.TooLong));
            Assert.That(session.TryAdd(DialogueSpeaker.Player, new string('字', 200), false, out var first), Is.EqualTo(DialogueWriteResult.Added));
            Assert.That(session.TryAdd(DialogueSpeaker.Echo, new string('字', 280), false, out _), Is.EqualTo(DialogueWriteResult.Added));
            Assert.That(first.Number, Is.EqualTo(1)); Assert.That(changes, Is.EqualTo(2));
        }
        [Test] public void D01_T03_InvalidSpeakerAndPlayerRefusalNeverEnterHistory()
        {
            var session = new DialogueSession();
            Assert.That(session.TryAdd((DialogueSpeaker)8, "文本", false, out _), Is.EqualTo(DialogueWriteResult.Invalid));
            Assert.That(session.TryAdd(DialogueSpeaker.Player, "文本", true, out _), Is.EqualTo(DialogueWriteResult.Invalid)); Assert.That(session.Messages, Is.Empty);
            Assert.Throws<ArgumentException>(() => new DialogueMessage(0, DialogueSpeaker.Player, "文本"));
            Assert.Throws<ArgumentException>(() => new DialogueMessage(1, DialogueSpeaker.Player, "文本", true));
        }
        [Test] public void D01_T04_OneHundredMessageLimitEvictsOldUiHistoryWithoutResettingNumbers()
        {
            var session = new DialogueSession(); for (var i = 0; i < 120; i++) session.TryAdd(DialogueSpeaker.Echo, "消息" + i, false, out _);
            Assert.That(session.Messages.Count, Is.EqualTo(100)); Assert.That(session.Messages[0].Number, Is.EqualTo(21)); Assert.That(session.Messages[99].Number, Is.EqualTo(120));
        }
        [Test] public void D01_T05_SaveSnapshotKeepsLastFiftyAndRestoredSequenceContinues()
        {
            var session = new DialogueSession(); for (var i = 0; i < 120; i++) session.TryAdd(DialogueSpeaker.Echo, "消息" + i, false, out _);
            var saved = session.CaptureRecent(); Assert.That(saved.Count, Is.EqualTo(50)); Assert.That(saved[0].Number, Is.EqualTo(71));
            session.TryAdd(DialogueSpeaker.Player, "后续问题", false, out _); Assert.That(saved[49].Number, Is.EqualTo(120));
            var restored = new DialogueSession(); var additions = 0; var replacements = 0; restored.Added += _ => additions++; restored.Replaced += () => replacements++;
            Assert.That(restored.TryRestoreRecent(saved), Is.True); Assert.That(additions, Is.Zero); Assert.That(replacements, Is.EqualTo(1));
            restored.TryAdd(DialogueSpeaker.Player, "继续", false, out var next); Assert.That(next.Number, Is.EqualTo(121)); Assert.That(restored.Version, Is.EqualTo(2));
        }
        [Test] public void D01_T06_InvalidRestoreIsRejectedAsAWholeAndPreservesVersion()
        {
            var session = new DialogueSession(); session.TryAdd(DialogueSpeaker.Player, "原始问题", false, out var original); var changes = 0; session.Replaced += () => changes++;
            foreach (var bad in new IReadOnlyList<DialogueMessage>[] { null, new DialogueMessage[] { null }, new[] { original, original }, new[] { new DialogueMessage(2, DialogueSpeaker.Echo, "后"), original }, new[] { new DialogueMessage(long.MaxValue, DialogueSpeaker.Echo, "溢出") }, Enumerable.Range(1, 51).Select(i => new DialogueMessage(i, DialogueSpeaker.Echo, "消息")).ToArray() })
            { Assert.That(session.TryRestoreRecent(bad), Is.False); Assert.That(session.Messages.Single(), Is.SameAs(original)); Assert.That(session.Version, Is.EqualTo(1)); }
            Assert.That(changes, Is.Zero);
        }
        [Test] public void D01_T07_ViewsAndSnapshotsRejectExternalMutationAndRestoreCopiesArrays()
        {
            var session = new DialogueSession(); session.TryAdd(DialogueSpeaker.Player, "问题", false, out _);
            Assert.Throws<NotSupportedException>(() => ((IList<DialogueMessage>)session.Messages).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<DialogueMessage>)session.CaptureRecent()).Clear());
            var array = new[] { new DialogueMessage(4, DialogueSpeaker.Echo, "已保存回复") }; Assert.That(session.TryRestoreRecent(array), Is.True); array[0] = new DialogueMessage(5, DialogueSpeaker.Echo, "外部更改");
            Assert.That(session.Messages[0].Text, Is.EqualTo("已保存回复"));
        }
        [Test] public void D01_T08_ResetClearsOnlyThisSessionAndDataAssemblyHasNoEngineOrMemoryDependency()
        {
            var session = new DialogueSession(); session.TryAdd(DialogueSpeaker.Player, "问题", false, out _); var snapshot = session.CaptureRecent(); var changed = 0; session.Replaced += () => changed++;
            session.Reset(); Assert.That(session.Messages, Is.Empty); Assert.That(session.Version, Is.EqualTo(2)); Assert.That(changed, Is.EqualTo(1));
            session.TryAdd(DialogueSpeaker.Echo, "新局", false, out var next); Assert.That(next.Number, Is.EqualTo(1)); Assert.That(snapshot[0].Text, Is.EqualTo("问题"));
            Assert.That(typeof(DialogueSession).Assembly.GetReferencedAssemblies().Any(a => a.Name.StartsWith("Unity", StringComparison.Ordinal) || a.Name.StartsWith("EchoStation.Memory", StringComparison.Ordinal)), Is.False);
        }
    }
}
