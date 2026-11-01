#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace EchoStation.Salvage.Validation
{
    public sealed class SalvageR2InputScope:IDisposable
    {
        readonly InputSettings previous,temporary;readonly HideFlags flags;bool disposed;
        public Keyboard Keyboard{get;}public Mouse Mouse{get;}public bool IsRestored=>disposed&&InputSystem.settings==previous&&previous.hideFlags==flags;
        public SalvageR2InputScope()
        {
            if(!Application.isPlaying)throw new InvalidOperationException("PlayMode required.");previous=InputSystem.settings;flags=previous.hideFlags;if(flags==HideFlags.HideAndDontSave)previous.hideFlags=HideFlags.DontSave;temporary=UnityEngine.Object.Instantiate(previous);temporary.hideFlags=HideFlags.DontSave;temporary.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            temporary.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=temporary;Keyboard=InputSystem.AddDevice<Keyboard>();Mouse=InputSystem.AddDevice<Mouse>();
        }
        public void Dispose(){if(disposed)return;if(Keyboard.added)InputSystem.RemoveDevice(Keyboard);if(Mouse.added)InputSystem.RemoveDevice(Mouse);InputSystem.settings=previous;previous.hideFlags=flags;if(temporary)UnityEngine.Object.Destroy(temporary);disposed=true;}
    }
}
#endif
