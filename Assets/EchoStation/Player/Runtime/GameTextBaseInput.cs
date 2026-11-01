using UnityEngine;
using UnityEngine.EventSystems;

namespace EchoStation.Player
{
    [AddComponentMenu("")]
    public sealed class GameTextBaseInput : BaseInput
    {
        internal GameTextInput Owner { get; set; }
        public override string compositionString => Owner ? Owner.Composition : "";
        public override IMECompositionMode imeCompositionMode
        { get => Owner ? Owner.ImeMode : IMECompositionMode.Auto; set { if (Owner) Owner.SetImeMode(value); } }
        public override Vector2 compositionCursorPos
        { get => Vector2.zero; set { if (Owner) Owner.SetImeCursor(value); } }
    }
}
