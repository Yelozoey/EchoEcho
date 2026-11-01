using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace EchoStation.Player
{
    public sealed class GameTextInput : InputField
    {
        private static readonly Key[] EditKeys = { Key.Backspace, Key.Delete, Key.LeftArrow, Key.RightArrow, Key.Home, Key.End };
        private static readonly KeyCode[] EditCodes = { KeyCode.Backspace, KeyCode.Delete, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Home, KeyCode.End };
        private readonly float[] repeats = new float[EditKeys.Length];
        private readonly Event editingEvent = new Event();
        private Keyboard keyboard;
        private BaseInputModule module;
        private BaseInput previousInput;
        private GameTextBaseInput bridge;
        private string composition = "";
        private int lastImeFrame = -10;
        private bool previousIme;
        private IMECompositionMode previousNativeIme;
        private bool nativeImeCaptured;
        public string Composition => composition;
        public bool IsComposing => composition.Length != 0;
        public bool CanHandleModalEscape => !IsComposing && Time.frameCount > lastImeFrame + 1;
        public int ReceivedTextEvents { get; private set; }
        public IMECompositionMode ImeMode { get; private set; } = IMECompositionMode.Auto;
        public long LastImeCommandResult { get; private set; }
        public bool NativeImeBridgeActive => nativeImeCaptured;
        public event Action SubmitRequested;
        public event Action CancelRequested;

        protected override void OnEnable() { Acquire(); AttachKeyboard(); base.OnEnable(); }
        protected override void LateUpdate() { Acquire(); AttachKeyboard(); base.LateUpdate(); }
        protected override void OnDisable() { base.OnDisable(); Release(); }
        protected override void OnDestroy() { Release(); base.OnDestroy(); }

        private void Acquire()
        {
            if (module || !EventSystem.current) return;
            module = EventSystem.current.currentInputModule ? EventSystem.current.currentInputModule : EventSystem.current.GetComponent<BaseInputModule>();
            if (!module) return;
            previousInput = module.inputOverride;
            bridge = GetComponent<GameTextBaseInput>(); if (!bridge) bridge = gameObject.AddComponent<GameTextBaseInput>();
            bridge.Owner = this; module.inputOverride = bridge;
        }
        private void AttachKeyboard()
        {
            if (keyboard == Keyboard.current) return;
            DetachKeyboard(); keyboard = Keyboard.current;
            if (keyboard == null) return;
            previousIme = keyboard.imeSelected.isPressed;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Game text needs the engine IME path in both Windows Player and Editor Play.
            // Keep text/composition events on InputSystem; only initialize the engine IME path.
            if (Application.isPlaying && keyboard.native) { previousNativeIme = UnityEngine.Input.imeCompositionMode; nativeImeCaptured = true; }
#endif
            keyboard.onTextInput += OnText; keyboard.onIMECompositionChange += OnComposition;
            if (isFocused) SetImeMode(IMECompositionMode.On);
        }
        private void DetachKeyboard()
        {
            if (keyboard != null)
            {
                keyboard.onTextInput -= OnText; keyboard.onIMECompositionChange -= OnComposition;
                if (keyboard.added) SendImeCommand(previousIme);
            }
            if (nativeImeCaptured) UnityEngine.Input.imeCompositionMode = previousNativeIme;
            nativeImeCaptured = false;
            keyboard = null; composition = ""; lastImeFrame = -10; ImeMode = IMECompositionMode.Auto;
        }
        private void Release()
        {
            DetachKeyboard();
            if (module && module.inputOverride == bridge) module.inputOverride = previousInput;
            if (bridge) bridge.Owner = null;
            module = null; previousInput = null;
        }
        internal void SetImeMode(IMECompositionMode value)
        {
            ImeMode = value;
            if (keyboard != null && keyboard.added) SendImeCommand(value == IMECompositionMode.On || value == IMECompositionMode.Auto && previousIme);
            if (nativeImeCaptured) UnityEngine.Input.imeCompositionMode = value == IMECompositionMode.Auto ? previousNativeIme : value;
        }
        private void SendImeCommand(bool enabled)
        { var command = EnableIMECompositionCommand.Create(enabled); LastImeCommandResult = keyboard.ExecuteCommand(ref command); }
        private void OnApplicationFocus(bool focused)
        { if (focused && CanEdit) SetImeMode(IMECompositionMode.On); }
        internal void SetImeCursor(Vector2 value) { if (keyboard != null && keyboard.added) keyboard.SetIMECursorPosition(value); }
        private bool CanEdit => isActiveAndEnabled && IsInteractable() && isFocused && !readOnly;
        private void OnText(char value)
        {
            if (!CanEdit || value < 32 || value == 127) return;
            if (composition.Length != 0) { composition = ""; lastImeFrame = Time.frameCount; }
            ReceivedTextEvents++; SendEdit(KeyCode.None, value); UpdateLabel();
        }
        private void OnComposition(IMECompositionString value)
        {
            if (!CanEdit) return;
            composition = value.ToString(); lastImeFrame = Time.frameCount; UpdateLabel();
        }
        private void SendEdit(KeyCode code, char value = '\0')
        {
            editingEvent.type = EventType.KeyDown; editingEvent.keyCode = code; editingEvent.character = value;
            editingEvent.modifiers = keyboard != null && keyboard.shiftKey.isPressed ? EventModifiers.Shift : EventModifiers.None;
            if (keyboard != null && keyboard.ctrlKey.isPressed) editingEvent.modifiers |= EventModifiers.Control;
            ProcessEvent(editingEvent); UpdateLabel();
        }
        public override void OnUpdateSelected(BaseEventData eventData)
        {
            Acquire(); AttachKeyboard();
            if (!CanEdit || keyboard == null) return;
            // Native IMGUI text is intentionally not read: this field has one text source.
            if (IsComposing || Time.frameCount <= lastImeFrame + 1) { eventData.Use(); return; }
            for (var i = 0; i < EditKeys.Length; i++)
            {
                var key = keyboard[EditKeys[i]];
                if (!key.isPressed) { repeats[i] = 0; continue; }
                if (key.wasPressedThisFrame || repeats[i] > 0 && Time.unscaledTime >= repeats[i])
                { SendEdit(EditCodes[i]); repeats[i] = Time.unscaledTime + (key.wasPressedThisFrame ? .4f : .05f); }
            }
            if (keyboard.ctrlKey.isPressed)
            {
                if (keyboard.aKey.wasPressedThisFrame) SendEdit(KeyCode.A);
                if (keyboard.cKey.wasPressedThisFrame) SendEdit(KeyCode.C);
                if (keyboard.vKey.wasPressedThisFrame) SendEdit(KeyCode.V);
                if (keyboard.xKey.wasPressedThisFrame) SendEdit(KeyCode.X);
            }
            // Submission can synchronously disable this field and detach its keyboard.
            var submit = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            var cancel = keyboard.escapeKey.wasPressedThisFrame;
            if (submit) SubmitRequested?.Invoke();
            else if (cancel) CancelRequested?.Invoke();
            eventData.Use();
        }
    }
}
