using System;
using System.Security.Cryptography;
using System.Text;
using EchoStation.Dialogue.Commands;
using EchoStation.Echo;
using EchoStation.Memory;
using EchoStation.Memory.Integration;
using EchoStation.Player;
using UnityEngine;

namespace EchoStation.Dialogue.Integration
{
    [DisallowMultipleComponent]
    public sealed class ScriptDialogueController : MonoBehaviour
    {
        private DialoguePanel view;
        private EchoBrain robot;
        private MemorySession memories;
        private JournalMemoryBridge knowledge;
        private Func<ScriptTarget[]> availableTargets;
        private Func<double> clock;
        private bool attached;
        private DialogueSession observedSession;
        private ulong dialogueVersion, memoryVersion;
        private int reservedEvents;
        public bool OrdinaryEventRecordingPaused => memories != null && memories.Store.Events.Count >= MemoryLimits.MaximumEvents - reservedEvents;
        public IntentKind? PendingAction { get; private set; }
        public IntentKind? LastDispatchedKind { get; private set; }
        public IntentRejection LastRejection { get; private set; }
        public string LastMemoryError { get; private set; } = "";
        public Func<ScriptParseResult, IntentRequest> AnswerProvider { get; set; }
        // Returning true consumes this input. False is allowed only for the original script route.
        public Func<DialogueMessage, bool> InputInterceptor { get; set; }
        public event Action<IntentRequest> PlayerIntentRequested;

        public void Configure(DialoguePanel panel, EchoBrain echo, MemorySession session, JournalMemoryBridge known, Func<ScriptTarget[]> targets, Func<ScriptParseResult, IntentRequest> answer = null, Func<double> gameClock = null, int reservedEventSlots = 0)
        {
            if (!panel || !echo || session == null || known == null || targets == null) throw new ArgumentException("Complete dialogue execution dependencies required.");
            if (reservedEventSlots < 0 || reservedEventSlots > MemoryLimits.MaximumEvents) throw new ArgumentOutOfRangeException(nameof(reservedEventSlots));
            Detach(); InputInterceptor = null; view = panel; robot = echo; memories = session; knowledge = known; availableTargets = targets; AnswerProvider = answer; clock = gameClock ?? (() => 0);
            reservedEvents = reservedEventSlots;
            ObserveVersions(); if (isActiveAndEnabled) Attach();
        }
        private void ObserveVersions()
        { PendingAction = null; observedSession = view.Session; dialogueVersion = observedSession.Version; memoryVersion = memories.Version; }
        private void ClearPending() { PendingAction = null; }
        private void Attach()
        {
            if (attached || !view || !robot) return;
            view.PlayerSubmitted += OnPlayerSubmitted; robot.Speech += OnSpeech; memories.Replaced += ClearPending; attached = true;
        }
        private void Detach()
        {
            if (!attached) return;
            if (view) view.PlayerSubmitted -= OnPlayerSubmitted; if (robot) robot.Speech -= OnSpeech; memories.Replaced -= ClearPending;
            attached = false; PendingAction = null;
        }
        private void OnEnable() { if (view) { ObserveVersions(); Attach(); } }
        private void OnDisable() => Detach();
        private void OnDestroy() { Detach(); InputInterceptor = null; PlayerIntentRequested = null; }

        private void OnPlayerSubmitted(DialogueMessage message)
        {
            if (!robot || !robot.IsInitialized) { view.Session.TryAdd(DialogueSpeaker.Echo, "通信尚未就绪，请稍后重试。", true, out _); return; }
            if (observedSession != view.Session || dialogueVersion != view.Session.Version || memoryVersion != memories.Version) ObserveVersions();
            if (InputInterceptor != null)
            {
                try { if (InputInterceptor(message)) { PendingAction = null; return; } }
                catch (Exception error)
                { PendingAction = null; Debug.LogException(error, this); Reply("通信请求未能处理，请重试。", true); return; }
            }
            ScriptParseResult result;
            try { result = new ScriptCommandParser(availableTargets()).Parse(message.Text, PendingAction); }
            catch (ArgumentException) { Reply("当前目标信息不可用，请稍后重试。", true); return; }
            PendingAction = result.Kind == ScriptParseKind.Clarify ? result.PendingAction : null;
            if (result.Kind == ScriptParseKind.Clarify || result.Kind == ScriptParseKind.Unknown) { Reply(result.Message, false); return; }
            if (result.Kind == ScriptParseKind.Question)
            {
                if (!knowledge.IsReady) { Reply("记忆同步尚未完成，请稍后再问。", false); return; }
                var response = AnswerProvider == null ? ScriptCommands.Answer("我目前没有相关记录。") : AnswerProvider(result);
                if (response == null || response.Kind != IntentKind.Answer && response.Kind != IntentKind.Refuse)
                    response = ScriptCommands.Refuse("信息请求不能触发行动，请明确指定调查指令。");
                Dispatch(response); return;
            }
            Dispatch(result.Intent);
        }
        public bool SubmitPlayerIntent(IntentRequest request)
        {
            if (!robot || !robot.IsInitialized || !view || memories == null || request == null) return false;
            PlayerIntentRequested?.Invoke(request);
            return Dispatch(request);
        }
        private bool Dispatch(IntentRequest request)
        {
            if (!robot.TrySubmit(request, out var rejection))
            { LastRejection = rejection; Reply("当前无法执行该行动，请检查目标、距离、视角或通路。", true); return false; }
            LastRejection = IntentRejection.None; LastDispatchedKind = request.Kind;
            if (request.Kind == IntentKind.Follow) Reply("开始跟随。", false);
            else if (request.Kind == IntentKind.Wait) Reply("原地待命。", false);
            if (request.Kind == IntentKind.Follow || request.Kind == IntentKind.Wait || request.Kind == IntentKind.Investigate || request.Kind == IntentKind.Warn) RecordAction(request);
            return true;
        }
        private void Reply(string text, bool refusal)
        {
            var request = refusal ? ScriptCommands.Refuse(text) : ScriptCommands.Answer(text);
            if (!robot || !robot.TrySubmit(request, out _)) view.Session.TryAdd(DialogueSpeaker.Echo, text, refusal, out _);
        }
        private void OnSpeech(string text, bool refusal)
        {
            view.Session.TryAdd(DialogueSpeaker.Echo, text, refusal, out _);
            if (robot.CurrentMode == IntentKind.Warn && robot.LastWarningTargetId != null)
            {
                var target = robot.LastWarningTargetId;
                Remember(new CharacterMemory(StableId("danger_", target), CharacterMemoryKind.KnownDanger, "已确认设备存在危险：" + target, target));
            }
        }
        private void RecordAction(IntentRequest request)
        {
            var summary = request.Kind == IntentKind.Follow ? "玩家要求Echo跟随。" : request.Kind == IntentKind.Wait ? "玩家要求Echo原地待命。" : request.Kind == IntentKind.Investigate ? "玩家要求调查：" + request.TargetName : "Echo确认危险：" + request.DangerTarget;
            var priority = request.Kind == IntentKind.Warn ? MemoryPriority.Critical : request.Kind == IntentKind.Investigate ? MemoryPriority.Normal : MemoryPriority.Low;
            // UI history may start a new conversation; use the memory timeline, not a UI message number.
            LastMemoryError = "";
            if (OrdinaryEventRecordingPaused) LastMemoryError = "普通指令事件已达记录预算，行动仍执行，流程事件空间已保留。";
            else
            {
                var result = memories.Store.AddEvent("event_command_" + memories.Version + "_" + (memories.Store.Events.Count + 1), clock(), summary, priority);
                if (result != MemoryWriteResult.Added && result != MemoryWriteResult.AlreadyKnown) LastMemoryError = result.ToString();
            }
            if (request.Kind == IntentKind.Follow || request.Kind == IntentKind.Wait)
                Remember(new CharacterMemory("echo_position_agreement", CharacterMemoryKind.Agreement, request.Kind == IntentKind.Follow ? "Echo与玩家约定保持跟随。" : "Echo按玩家指令原地待命。"));
        }
        private void Remember(CharacterMemory state)
        {
            var result = memories.Store.TryGetCharacter(state.Id, out _) ? memories.Store.UpdateCharacter(state) : memories.Store.AddCharacter(state);
            if (result != MemoryWriteResult.Added && result != MemoryWriteResult.AlreadyKnown && result != MemoryWriteResult.Updated) LastMemoryError = result.ToString();
        }
        private static string StableId(string prefix, string value)
        { using (var sha = SHA256.Create()) return prefix + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").Substring(0, 32).ToLowerInvariant(); }
    }
}
