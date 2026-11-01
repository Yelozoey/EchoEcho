using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Object=UnityEngine.Object;

namespace EchoStation.Tests.Player
{
    public sealed class SyntheticInputScope:IDisposable
    {
        private readonly InputSettings previous,settings;private readonly HideFlags previousFlags;private bool disposed;
        public Keyboard Keyboard{get;}
        public Mouse Mouse{get;}
        public bool IsRestored=>disposed&&previous&&InputSystem.settings==previous&&previous.hideFlags==previousFlags;
        public SyntheticInputScope()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Synthetic background scope requires PlayMode.");previous=InputSystem.settings;if(!previous)throw new InvalidOperationException("Input settings unavailable.");previousFlags=previous.hideFlags;
            // InputManager's setter destroys an outgoing object whose flags equal HideAndDontSave.
            // Preserve that transient object while the temporary clone is installed, then restore flags.
            if(previousFlags==HideFlags.HideAndDontSave)previous.hideFlags=HideFlags.DontSave;
            settings=Object.Instantiate(previous);settings.hideFlags=HideFlags.DontSave;settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings=settings;Keyboard=InputSystem.AddDevice<Keyboard>();Mouse=InputSystem.AddDevice<Mouse>();
        }
        public void Dispose(){if(disposed)return;if(Keyboard.added)InputSystem.RemoveDevice(Keyboard);if(Mouse.added)InputSystem.RemoveDevice(Mouse);InputSystem.settings=previous;previous.hideFlags=previousFlags;if(settings)Object.Destroy(settings);disposed=true;}
    }
}
