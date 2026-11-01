using System;
using System.Collections.Generic;
using EchoStation.Dialogue;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EchoStation.Player
{
    [DefaultExecutionOrder(-60), RequireComponent(typeof(PlayerInputReader), typeof(PlayerJournal), typeof(JournalPanel)), DisallowMultipleComponent]
    public sealed class DialoguePanel : MonoBehaviour
    {
        private sealed class Row { public RectTransform root; public Image background; public Text speaker, body; public long number; }
        private Canvas canvas;
        private GameObject dialog;
        private PlayerInputReader playerInput;
        private GameTextInput inputField;
        private Text status;
        private RectTransform content;
        private ScrollRect scroll;
        private readonly List<Row> rows = new List<Row>();
        private DialogueSession session = new DialogueSession();
        private bool subscribed, dirty = true, scrollToEnd = true;
        public bool IsOpen { get; private set; }
        public DialogueSession Session => session;
        public Canvas Canvas => canvas;
        public GameTextInput InputField => inputField;
        public ScrollRect MessageScroll => scroll;
        public int AllocatedRows => rows.Count;
        public int DisplayedCount { get; private set; }
        public Button SendButton { get; private set; }
        public event Action<DialogueMessage> PlayerSubmitted;

        private void Awake() { if (Application.isPlaying) Build(); }
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            Build(); canvas.gameObject.SetActive(true); Subscribe(); dirty = true;
        }
        public void Bind(DialogueSession value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Unsubscribe(); session = value; if (isActiveAndEnabled && Application.isPlaying) Subscribe(); OnReplaced();
        }
        private void Subscribe() { if (subscribed) return; session.Added += OnAdded; session.Replaced += OnReplaced; subscribed = true; }
        private void Unsubscribe() { if (!subscribed) return; session.Added -= OnAdded; session.Replaced -= OnReplaced; subscribed = false; }
        private void OnAdded(DialogueMessage message) { dirty = true; if (message.Speaker == DialogueSpeaker.Player || !scroll || scroll.verticalNormalizedPosition <= .02f) scrollToEnd = true; }
        private void OnReplaced() { dirty = scrollToEnd = true; if (inputField) inputField.text = ""; }
        private static RectTransform Rect(string name, Transform parent, float width, float height, float x, float y)
            => RuntimeUi.Rect(name, parent, new Vector2(width, height), new Vector2(x, y), new Vector2(0, 1), new Vector2(0, 1));

        private void Build()
        {
            if (canvas || !Application.isPlaying) return;
            playerInput = GetComponent<PlayerInputReader>(); canvas = RuntimeUi.CreateCanvas("ES_DialogueOverlay", gameObject.scene, 130);
            var shade = RuntimeUi.Fill("Dialog", canvas.transform, Vector2.zero); RuntimeUi.Image(shade, new Color(0, 0, 0, .76f)); dialog = shade.gameObject;
            var panel = RuntimeUi.Rect("Panel", shade, new Vector2(1280, 840), Vector2.zero, new Vector2(.5f, .5f), new Vector2(.5f, .5f)); RuntimeUi.Image(panel, new Color(.025f, .045f, .07f, .98f));
            RuntimeUi.Label(Rect("Heading", panel, 850, 44, 36, -22), 28).text = "Echo 通信";
            RuntimeUi.Button(Rect("Close", panel, 220, 42, 1024, -24), "返回探索  [Esc]", () => SetOpen(false));
            scroll = RuntimeUi.ScrollViewport("Messages", panel, new Vector2(36, -92), new Vector2(1208, 584), out content);
            status = RuntimeUi.Label(Rect("Examples", panel, 1208, 32, 36, -685), 17); status.text = "可输入：跟随我 / 等在这里 / 调查储藏室可疑点 / 给我提示";
            var inputRect = Rect("Input", panel, 1048, 64, 36, -726); RuntimeUi.Image(inputRect, new Color(.07f, .12f, .17f)); inputRect.gameObject.SetActive(false);
            var text = RuntimeUi.Label(RuntimeUi.Fill("Text", inputRect, new Vector2(16, 10)), 22); text.verticalOverflow = VerticalWrapMode.Truncate;
            var placeholder = RuntimeUi.Label(RuntimeUi.Fill("Placeholder", inputRect, new Vector2(16, 10)), 20); placeholder.color = new Color(.43f, .57f, .66f); placeholder.text = "输入给 Echo 的信息，Enter 发送";
            inputField = inputRect.gameObject.AddComponent<GameTextInput>(); inputField.textComponent = text; inputField.placeholder = placeholder; inputField.targetGraphic = inputRect.GetComponent<Image>();
            inputField.characterLimit = DialogueSession.MaximumPlayerCharacters; inputField.lineType = UnityEngine.UI.InputField.LineType.SingleLine;
            inputField.SubmitRequested += OnSubmit; inputField.CancelRequested += OnCancel;
            inputRect.gameObject.SetActive(true);
            SendButton = RuntimeUi.Button(Rect("Send", panel, 136, 64, 1108, -726), "发送", () => SubmitInput(), 22);
            RuntimeUi.Label(Rect("Footer", panel, 1208, 24, 36, -805), 16).text = "Echo 提供调查和提示；门、终端和供电仍由你操作。";
            dialog.SetActive(false);
        }
        private void Update()
        {
            if (!playerInput) return;
            if (!playerInput.HasApplicationFocus) { if (IsOpen) SetOpen(false); return; }
            if (!IsOpen && Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame) SetOpen(true);
            if (IsOpen && dirty) RefreshMessages();
        }
        public bool SetOpen(bool value)
        {
            Build(); if (!canvas) return false; if (value == IsOpen) return true;
            if (value && (!isActiveAndEnabled || !playerInput.HasApplicationFocus || playerInput.UiBlocked)) return false;
            IsOpen = value; dialog.SetActive(value); playerInput.SetUiBlocked(value);
            if (value)
            {
                dirty = scrollToEnd = true; RefreshMessages(); inputField.Select(); inputField.ActivateInputField();
            }
            return true;
        }
        private void OnSubmit() { SubmitInput(); }
        private void OnCancel() { SetOpen(false); }
        public bool SubmitInput()
        {
            if (!IsOpen || !playerInput.HasApplicationFocus || inputField.IsComposing) return false;
            var result = session.TryAdd(DialogueSpeaker.Player, inputField.text, false, out var message);
            if (result != DialogueWriteResult.Added) { status.text = result == DialogueWriteResult.Empty ? "请输入信息。" : "输入超过长度或当前不可提交。"; return false; }
            inputField.text = ""; status.text = "信息已发送。"; scrollToEnd = true;
            PlayerSubmitted?.Invoke(message); inputField.ActivateInputField(); return true;
        }
        private void RefreshMessages()
        {
            var oldScroll = scroll.verticalNormalizedPosition; var offset = 12f;
            for (var i = 0; i < session.Messages.Count; i++)
            {
                if (i == rows.Count)
                {
                    var row = new Row(); row.root = Rect("Message_" + i, content, 1072, 80, 8, 0); row.background = RuntimeUi.Image(row.root, RuntimeUi.ButtonColor, false);
                    row.speaker = RuntimeUi.Label(Rect("Speaker", row.root, 1036, 24, 18, -9), 16); row.body = RuntimeUi.Label(Rect("Body", row.root, 1036, 36, 18, -38), 21, TextAnchor.UpperLeft); rows.Add(row);
                }
                var item = session.Messages[i]; var current = rows[i]; current.number = item.Number; current.root.gameObject.SetActive(true);
                current.speaker.text = item.Speaker == DialogueSpeaker.Player ? "你" : "Echo"; current.body.text = item.Text;
                var height = Mathf.Max(36, current.body.preferredHeight); current.body.rectTransform.sizeDelta = new Vector2(1036, height);
                current.root.sizeDelta = new Vector2(1072, height + 56); current.root.anchoredPosition = new Vector2(item.Speaker == DialogueSpeaker.Player ? 128 : 8, -offset);
                current.background.color = item.Speaker == DialogueSpeaker.Player ? new Color(.055f, .15f, .22f) : new Color(.045f, .075f, .105f);
                offset += height + 70;
            }
            for (var i = session.Messages.Count; i < rows.Count; i++) rows[i].root.gameObject.SetActive(false);
            content.sizeDelta = new Vector2(0, Mathf.Max(scroll.viewport.rect.height, offset)); UnityEngine.Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = scrollToEnd ? 0 : oldScroll; scrollToEnd = false; dirty = false; DisplayedCount = session.Messages.Count;
        }
        public string DisplayedText(long number)
        { foreach (var row in rows) if (row.root.gameObject.activeSelf && row.number == number) return row.body.text; return null; }
        private void OnDisable()
        { Unsubscribe(); if (IsOpen) SetOpen(false); if (canvas) canvas.gameObject.SetActive(false); }
        private void OnDestroy()
        {
            Unsubscribe(); if (inputField) { inputField.SubmitRequested -= OnSubmit; inputField.CancelRequested -= OnCancel; }
            if (canvas) Destroy(canvas.gameObject);
        }
    }
}
