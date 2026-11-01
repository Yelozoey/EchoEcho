using System;
using System.Linq;
using EchoStation.Player;
using EchoStation.Salvage;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EchoStation.SalvageExpansion
{
    [DefaultExecutionOrder(-30)]
    public sealed class BackpackPanel : MonoBehaviour
    {
        const float Cell = 84;
        InventoryStore store; InventoryDrag drag; ExpansionPause pause; PlayerInputReader input; IDisposable lease;
        RectTransform grid; GameObject canvasRoot; Text details, notice, usage; Button equip, drop;
        readonly Image[] cells = new Image[24]; readonly Text[] labels = new Text[24];
        string selected; int hoverX, hoverY; long renderedRevision = -1; bool recapture;
        public bool IsOpen { get; private set; }
        public string Notice { get; private set; } = "";
        public Canvas Canvas => canvasRoot ? canvasRoot.GetComponent<Canvas>() : null;
        public InventoryDrag Drag => drag;
        public RectTransform Grid => grid;
        public Func<string, string> DropItem { get; set; }
        public void Configure(InventoryStore inventory, ExpansionPause pauseController, PlayerInputReader reader)
        {
            if (IsOpen) Close();
            store = inventory ?? throw new ArgumentNullException(nameof(inventory)); drag = new InventoryDrag(store);
            pause = pauseController ? pauseController : throw new ArgumentNullException(nameof(pauseController)); input = reader;
        }
        void Build()
        {
            if (canvasRoot) return;
            var title = SalvageUi.Panel(transform, "ExpansionBackpack", 1180, 740, 120);
            canvasRoot = SalvageUi.CanvasOf(title); canvasRoot.AddComponent<GraphicRaycaster>();
            var panel = (RectTransform)title.transform.parent; title.gameObject.SetActive(false);
            SalvageWidgets.Label(SalvageWidgets.Rect("Heading", panel, 36, 24, 1000, 48), "回收背包  ·  6 × 4", 30);
            SalvageWidgets.Label(SalvageWidgets.Rect("Help", panel, 36, 80, 1100, 36), "拖拽摆放 · R 旋转 · Esc 取消/返回 · 整理时任务与危险暂停", 22);
            grid = SalvageWidgets.Rect("Grid", panel, 36, 142, Cell * 6, Cell * 4);
            for (int y = 0; y < 4; y++) for (int x = 0; x < 6; x++)
            {
                int index = y * 6 + x;
                var rect = SalvageWidgets.Rect("Cell_" + x + "_" + y, grid, x * Cell + 2, y * Cell + 2, Cell - 4, Cell - 4);
                cells[index] = rect.gameObject.AddComponent<Image>();
                var pointer = rect.gameObject.AddComponent<BackpackCellPointer>(); pointer.Configure(this, x, y);
                labels[index] = SalvageWidgets.Label(SalvageWidgets.Rect("Label", rect, 4, 10, Cell - 12, Cell - 20), "", 19);
                labels[index].alignment = TextAnchor.MiddleCenter; labels[index].horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            details = SalvageWidgets.Label(SalvageWidgets.Rect("Details", panel, 590, 142, 550, 210), "选择一个物品。", 25);
            equip = SalvageWidgets.Button(panel, "Equip", "装备 / 卸下", 590, 390, 240, Equip);
            drop = SalvageWidgets.Button(panel, "Drop", "放到安全地面", 860, 390, 280, Drop);
            usage = SalvageWidgets.Label(SalvageWidgets.Rect("Usage", panel, 36, 510, 1100, 48), "", 24);
            notice = SalvageWidgets.Label(SalvageWidgets.Rect("Notice", panel, 36, 570, 1100, 70), "", 23);
            SalvageWidgets.Button(panel, "Close", "返回探索 [I / Esc]", 36, 665, 320, Close);
            if (!FindFirstObjectByType<EventSystem>())
            {
                var events = new GameObject("ExpansionEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            canvasRoot.SetActive(false);
        }
        public bool Open()
        {
            if (IsOpen || store == null || !pause || !input || !input.HasApplicationFocus || pause.Blocked) return false;
            Build(); lease = pause.Acquire(); IsOpen = true; canvasRoot.SetActive(true); Render(); return true;
        }
        public void Close()
        {
            drag?.Cancel(); if (canvasRoot) canvasRoot.SetActive(false); IsOpen = false;
            lease?.Dispose(); lease = null; recapture = true;
        }
        void Update()
        {
            if (!input || store == null) return;
            if (!input.HasApplicationFocus) { if (IsOpen) Close(); return; }
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.iKey.wasPressedThisFrame) { if (IsOpen) Close(); else Open(); }
                else if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) { if (drag.Active) { drag.Cancel(); Render(); } else Close(); }
                else if (IsOpen && keyboard.rKey.wasPressedThisFrame) Rotate();
            }
            if (IsOpen && store.Revision != renderedRevision) { if (drag.Stale) { drag.Cancel(); SetNotice("物品状态已变化，拖拽已取消。"); } Render(); }
        }
        void LateUpdate() { if (!recapture) return; recapture = false; if (pause) pause.RestorePointer(); }
        public string ItemAt(int x, int y)
        {
            return store?.Records.FirstOrDefault(r => r.OccupiesGrid && store.Definition(r.Id).Footprint.RotateClockwise(r.Rotation).Cells.Any(c => r.X + c.X == x && r.Y + c.Y == y))?.Id;
        }
        public void Select(int x, int y) { selected = ItemAt(x, y); Render(); }
        public bool BeginDrag(int x, int y)
        {
            if (!IsOpen) return false; selected = ItemAt(x, y); if (selected == null) return false;
            var row = store.Get(selected); bool result = drag.Begin(selected, x - row.X, y - row.Y); hoverX = x; hoverY = y; Render(); return result;
        }
        public void Hover(int x, int y) { hoverX = x; hoverY = y; Render(); }
        public bool CommitDrag(int x, int y)
        {
            if (!IsOpen || !drag.Active) return false;
            bool result = drag.Commit(x, y, out var error); SetNotice(result ? "摆放完成。" : error); Render(); return result;
        }
        public void Rotate()
        {
            if (!IsOpen) return;
            if (drag.Active) { drag.Rotate(); Render(); return; }
            if (selected == null) return;
            var row = store.Get(selected);
            if (row.Location == InventoryItemLocation.Held) { SetNotice("请先卸下，再调整物品朝向。"); return; }
            bool result = store.TryStow(selected, row.X, row.Y, (row.Rotation + 1) % 4, out var error);
            SetNotice(result ? "已旋转。" : error); Render();
        }
        public Vector2 ScreenCell(int x, int y) => RectTransformUtility.WorldToScreenPoint(null, grid.TransformPoint(new Vector3((x + .5f) * Cell, -(y + .5f) * Cell, 0)));
        public void PointerHover(PointerEventData data)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(grid, data.position, data.pressEventCamera, out var local))
                Hover(Mathf.FloorToInt(local.x / Cell), Mathf.FloorToInt(-local.y / Cell));
        }
        public void PointerEnd(PointerEventData data) { PointerHover(data); CommitDrag(hoverX, hoverY); }
        void Equip()
        {
            if (selected == null) return;
            string error; bool result = store.Get(selected).Location == InventoryItemLocation.Held ? store.TryUnequip(out error) : store.TryEquip(selected, out error);
            SetNotice(result ? "装备状态已更新，原格位保留。" : error); Render();
        }
        void Drop() { if (selected == null || DropItem == null) return; SetNotice(DropItem(selected)); Render(); }
        void SetNotice(string value) { Notice = value ?? ""; if (notice) notice.text = Notice; }
        static Color ItemColor(InventoryItemKind kind) => kind == InventoryItemKind.Tool ? new Color(.13f, .37f, .5f) : kind == InventoryItemKind.Battery ? new Color(.5f, .37f, .12f) : kind == InventoryItemKind.Core ? new Color(.25f, .45f, .29f) : new Color(.31f, .27f, .45f);
        public void Render()
        {
            if (!canvasRoot || store == null) return;
            for (int i = 0; i < 24; i++) { cells[i].color = new Color(.10f, .15f, .19f); labels[i].text = ""; }
            foreach (var row in store.Records.Where(r => r.OccupiesGrid && (!drag.Active || r.Id != drag.Id)))
            {
                var item = store.Definition(row.Id); var footprint = item.Footprint.RotateClockwise(row.Rotation);
                foreach (var cell in footprint.Cells) cells[(row.Y + cell.Y) * 6 + row.X + cell.X].color = row.Id == selected ? ItemColor(item.Kind) * 1.35f : ItemColor(item.Kind);
                var first = footprint.Cells[0]; labels[(row.Y + first.Y) * 6 + row.X + first.X].text = item.Name.Length > 3 ? item.Name.Substring(0, 2) : item.Name;
            }
            if (drag.Active)
            {
                bool valid = drag.Preview(hoverX, hoverY, out var error);
                foreach (var cell in store.Definition(drag.Id).Footprint.RotateClockwise(drag.Rotation).Cells)
                {
                    int x = hoverX - drag.GrabX + cell.X, y = hoverY - drag.GrabY + cell.Y;
                    if (x >= 0 && x < 6 && y >= 0 && y < 4) cells[y * 6 + x].color = valid ? new Color(.12f, .60f, .35f) : new Color(.75f, .18f, .14f);
                }
                notice.text = valid ? "可以放入 · 松开鼠标确认" : error;
            }
            else notice.text = Notice;
            var current = store.Records.FirstOrDefault(r => r.Id == selected);
            details.text = current == null ? "选择物品查看详情。\n\n工具和任务物共用空间。\nL形的空角可以放其他物品。" : store.Definition(current.Id).Name + "\n" + store.Definition(current.Id).Kilograms + " kg · " + store.Definition(current.Id).Footprint.Area + " 格\n方向 " + current.Rotation * 90 + "°\n" + (current.Location == InventoryItemLocation.Held ? "已装备 · 原格位保留" : current.Location == InventoryItemLocation.Backpack ? "背包内" : "已离开背包");
            equip.interactable = current != null && current.OccupiesGrid && !drag.Active;
            drop.interactable = current != null && current.OccupiesGrid && !drag.Active && DropItem != null;
            usage.text = "占用 " + store.Board().OccupiedCells.Count + " / 24 格    手持：" + (store.HeldId == null ? "无" : store.Definition(store.HeldId).Name);
            renderedRevision = store.Revision;
        }
        void OnDisable() => Close();
    }
}
