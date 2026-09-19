namespace AvaritiaMod.Common.AvaritiaUtils
{
    /// <summary>
    /// 拖拽管理器
    /// </summary>
    public static class DragManager
    {
        /// <summary>
        /// 拖拽类型
        /// </summary>
        public enum DragType
        {
            None,
            Left,
            Right
        }
        public static bool JustReleased { get; set; }
        /// <summary>
        /// 开始拖拽的类型
        /// </summary>
        public static DragType StartType { get; private set; }
        /// <summary>
        /// 拖拽开始槽位
        /// </summary>
        public static AvaritiaItemSlot? StartSlot { get; private set; }
        public static bool IsDragging => _currentDrag != DragType.None;
        public static bool IsInRollbackCooldown => _rollbackFrame + 12 >= Main.GameUpdateCount;
        private static DragType _currentDrag = DragType.None;
        private static readonly List<AvaritiaItemSlot> _draggedSlots = [];
        private static readonly Dictionary<AvaritiaItemSlot, int> _originStacks = [];
        private static ulong _rollbackFrame;
        private static int _snapshotType;
        private static int _snapshotMaxStack;
        private static int _snapshotTotalAmount;
        private static bool _dragStarted;
        public static bool OriginStacksContainsKey(AvaritiaItemSlot slot) => _originStacks.ContainsKey(slot);
        public static void MouseUp()
        {
            _currentDrag = DragType.None;
            StartSlot = null;
            StartType = DragType.None;
            _draggedSlots.Clear();
            _originStacks.Clear();
            _dragStarted = false;
            JustReleased = true;
        }
        public static void RollbackDrag()
        {
            foreach (AvaritiaItemSlot slot in _originStacks.Keys)
            {
                if (_originStacks.TryGetValue(slot, out int original))
                {
                    SetSlotStack(slot, original);
                }
            }
            Main.mouseItem.SetDefaults(_snapshotType);
            Main.mouseItem.stack = _snapshotTotalAmount;
            _currentDrag = DragType.None;
            StartSlot = null;
            StartType = DragType.None;
            _draggedSlots.Clear();
            _originStacks.Clear();
            _dragStarted = false;
            _rollbackFrame = Main.GameUpdateCount;
            JustReleased = true;
        }
        /// <summary>
        /// 取消进行中的槽位拖拽（面板被拖拽时调用）。
        /// <para>与 <see cref="RollbackDrag"/> 的区别：没有拖拽时什么都不做，
        /// 避免把上一次的快照套用到当前鼠标物品上。</para>
        /// </summary>
        public static void CancelActiveDrag()
        {
            if (!IsDragging && StartSlot is null)
            {
                return;
            }
            RollbackDrag();
        }
        public static void MouseDown(DragType type, AvaritiaItemSlot slot)
        {
            //拖拽窗口时不要开始槽位拖拽，否则鼠标上的物品会被分到经过的每一个槽位
            if (DragUISession.IsAnyPanelDragging)
            {
                return;
            }
            if (Main.mouseItem.IsAir || IsDragging)
            {
                return;
            }
            StartType = type;
            StartSlot = slot;
            _snapshotType = Main.mouseItem.type;
            _snapshotMaxStack = Main.mouseItem.maxStack;
            _snapshotTotalAmount = Main.mouseItem.stack;
            _draggedSlots.Clear();
            _originStacks.Clear();
            _dragStarted = false;
            JustReleased = false;
        }
        public static void OnSlotHovered(AvaritiaItemSlot slot)
        {
            if (JustReleased || StartSlot == null)
            {
                return;
            }
            bool buttonHeld = StartType == DragType.Left ? Main.mouseLeft : Main.mouseRight;
            if (!buttonHeld)
            {
                MouseUp();
                return;
            }
            if (slot == StartSlot || !CanSlotAccept(slot.Item))
            {
                return;
            }
            if (!_dragStarted)
            {
                _dragStarted = true;
                _currentDrag = StartType;
                if (CanSlotAccept(StartSlot.Item))
                {
                    AddSlotToDrag(StartSlot);
                    if (StartType == DragType.Right)
                    {
                        PlaceOneItem(StartSlot);
                    }
                }
            }
            if (_draggedSlots.Contains(slot))
            {
                return;
            }
            if (!CanAcceptNewSlot(slot))
            {
                return;
            }
            AddSlotToDrag(slot);
            switch (_currentDrag)
            {
                case DragType.Left:
                    {
                        ApplyLeftDrag();
                        break;
                    }
                case DragType.Right:
                    {
                        ApplyRightDrag();
                        break;
                    }
            }
        }
        private static void AddSlotToDrag(AvaritiaItemSlot slot)
        {
            _draggedSlots.Add(slot);
            if (!_originStacks.ContainsKey(slot))
            {
                _originStacks[slot] = slot.Item.IsAir ? 0 : slot.Item.stack;
            }
        }
        private static bool CanSlotAccept(Item slotItem) => slotItem.IsAir || slotItem.type == _snapshotType && slotItem.stack < _snapshotMaxStack;
        private static bool CanAcceptNewSlot(AvaritiaItemSlot candidate)
        {
            int sum = (from s in _draggedSlots let orig = _originStacks.GetValueOrDefault(s, 0) let curr = s.Item.IsAir ? 0 : s.Item.stack select Math.Max(0, curr - orig)).Sum();
            int totalAvailable = Main.mouseItem.stack + sum;
            int needOne = _draggedSlots.Count(s => _originStacks.GetValueOrDefault(s, 0) == 0);
            if (!candidate.Item.IsAir &&
                (candidate.Item.type != _snapshotType || candidate.Item.stack >= _snapshotMaxStack))
            {
                return totalAvailable >= needOne;
            }
            int candOrig = _originStacks.GetValueOrDefault(candidate, candidate.Item.IsAir ? 0 : candidate.Item.stack);
            if (candOrig == 0)
            {
                needOne++;
            }
            return totalAvailable >= needOne;
        }
        private static void ApplyLeftDrag()
        {
            List<AvaritiaItemSlot> validSlots = [.. _draggedSlots.Where(s => s.Item.IsAir || s.Item.type == _snapshotType)];
            if (validSlots.Count == 0)
            {
                return;
            }
            foreach (AvaritiaItemSlot slot in validSlots)
            {
                int original = _originStacks.GetValueOrDefault(slot, 0);
                int current = slot.Item.IsAir ? 0 : slot.Item.stack;
                int excess = current - original;
                if (excess <= 0)
                {
                    continue;
                }
                SetSlotStack(slot, original);
                Main.mouseItem.stack += excess;
            }
            int totalAvailable = Main.mouseItem.stack;
            if (totalAvailable <= 0)
            {
                return;
            }
            int slotCount = validSlots.Count;
            int perSlot = totalAvailable / slotCount;
            if (perSlot == 0)
            {
                for (int i = 0; i < validSlots.Count; i++)
                {
                    int original = _originStacks.GetValueOrDefault(validSlots[i], 0);
                    int add = i < totalAvailable ? 1 : 0;
                    int finalStack = original + add;
                    SetSlotStack(validSlots[i], finalStack);
                }
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            foreach (AvaritiaItemSlot itemSlot in validSlots)
            {
                int original = _originStacks.GetValueOrDefault(itemSlot, 0);
                int capacity = _snapshotMaxStack - original;
                int add = Math.Min(perSlot, capacity);
                SetSlotStack(itemSlot, original + add);
            }
            int actuallyPlaced = validSlots.Sum(s =>
            {
                int orig = _originStacks.GetValueOrDefault(s, 0);
                int curr = s.Item.IsAir ? 0 : s.Item.stack;
                return curr - orig;
            });
            int leftOver = totalAvailable - actuallyPlaced;
            if (leftOver > 0)
            {
                Main.mouseItem.SetDefaults(_snapshotType);
                Main.mouseItem.stack = leftOver;
            }
            else
            {
                Main.mouseItem.TurnToAir();
            }
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        private static void ApplyRightDrag()
        {
            if (_draggedSlots.Count == 0)
            {
                return;
            }
            AvaritiaItemSlot targetSlot = _draggedSlots[^1];
            int original = _originStacks.GetValueOrDefault(targetSlot, 0);
            int current = targetSlot.Item.IsAir ? 0 : targetSlot.Item.stack;
            if (current > original)
            {
                return;
            }
            PlaceOneItem(targetSlot);
        }
        private static void PlaceOneItem(AvaritiaItemSlot slot)
        {
            if (Main.mouseItem.stack <= 0)
            {
                return;
            }
            Item slotItem = slot.Item;
            if (!slotItem.IsAir && (slotItem.type != _snapshotType || slotItem.stack >= _snapshotMaxStack))
            {
                return;
            }
            if (!_draggedSlots.Contains(slot))
            {
                AddSlotToDrag(slot);
            }
            if (slotItem.IsAir)
            {
                slotItem = new Item(_snapshotType, 0);
            }
            slotItem.stack++;
            Main.mouseItem.stack--;
            slot.Item = slotItem;
            if (Main.mouseItem.stack <= 0)
            {
                Main.mouseItem.TurnToAir();
            }
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        private static void SetSlotStack(AvaritiaItemSlot slot, int stack)
        {
            if (stack > 0)
            {
                if (slot.Item.IsAir)
                {
                    slot.Item = new Item(_snapshotType, stack);
                }
                else
                {
                    slot.Item.stack = stack;
                }
            }
            else
            {
                slot.Item = new Item();
            }

        }
    }
}