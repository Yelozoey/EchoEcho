using UnityEngine;
using UnityEngine.EventSystems;

namespace EchoStation.SalvageExpansion
{
    public sealed class BackpackCellPointer : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        BackpackPanel panel; int x, y;
        public void Configure(BackpackPanel owner, int column, int row) { panel = owner; x = column; y = row; }
        public void OnPointerClick(PointerEventData data) { if (panel) panel.Select(x, y); }
        public void OnBeginDrag(PointerEventData data) { if (panel) panel.BeginDrag(x, y); }
        public void OnDrag(PointerEventData data) { if (panel && panel.Drag.Active) panel.PointerHover(data); }
        public void OnEndDrag(PointerEventData data) { if (panel && panel.Drag.Active) panel.PointerEnd(data); }
    }
}
