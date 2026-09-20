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
        /// <summary>本 tick 是否已经结算过一次拖拽（防止一帧内重复应用）。</summary>
        private static bool _actedThisFrame;
        private static ulong _lastActionFrame;
        public static bool OriginStacksContainsKey(AvaritiaItemSlot slot) => _originStacks.ContainsKey(slot);
        /// <summary>诊断日志用：当前拖拽状态的一行描述。</summary>
        public static string Describe()
            => $"drag type={StartType} current={_currentDrag} started={_dragStarted} slots={_draggedSlots.Count} origins={_originStacks.Count} "
                + $"snapshot={_snapshotType}x{_snapshotTotalAmount}/max{_snapshotMaxStack} startSlot={(StartSlot is null ? "null" : "set")} "
                + $"justReleased={JustReleased} rollbackCooldown={IsInRollbackCooldown}";
        public static void MouseUp()
        {
            ResetState();
            JustReleased = true;
        }
        /// <summary>清空拖拽状态（不改 <see cref="JustReleased"/>：新会话可以立刻开始）。</summary>
        private static void ResetState()
        {
            _currentDrag = DragType.None;
            StartSlot = null;
            StartType = DragType.None;
            _draggedSlots.Clear();
            _originStacks.Clear();
            _dragStarted = false;
            //快照必须一起作废：否则之后再来一次回滚，会把上一轮的旧数量和旧类型写到鼠标上
            //（比当前多就是刷物品，比当前少就是凭空消失）。
            _snapshotType = 0;
            _snapshotMaxStack = 0;
            _snapshotTotalAmount = 0;
        }
        public static void RollbackDrag()
        {
            //没有任何槽位被这次拖拽改动过，就没有东西需要回滚。此时鼠标上的物品可能已被其它操作
            //（中间那次单击 / 双击收集）拿走或替换，再拿旧快照覆盖鼠标就是凭空造出物品 / 抹掉物品。
            if (_originStacks.Count == 0)
            {
                FinishRollback();
                return;
            }
            int delta = (from slot in _originStacks.Keys let original = _originStacks[slot] let current = slot.Item.IsAir ? 0 : slot.Item.stack select current - original).Sum();
            //回滚 = 槽位还原 + 把拖拽对槽位的净改动从鼠标上撤销（放进去了就还给鼠标，拿出去了就收回）。
            //因此鼠标上必须仍是同类物品（或为空）且数量够用、装得下。不满足时放弃回滚、保持现状：
            //宁可这次拖拽的结果留着，也不能凭空增删物品。
            bool sameType = Main.mouseItem.IsAir
                || Main.mouseItem.type == _snapshotType && Main.mouseItem.maxStack == _snapshotMaxStack;
            int restore = Main.mouseItem.stack + delta;
            if (!sameType || restore < 0 || restore > Math.Max(1, _snapshotMaxStack))
            {
                FinishRollback();
                return;
            }
            foreach (AvaritiaItemSlot slot in _originStacks.Keys)
            {
                SetSlotStack(slot, _originStacks[slot]);
            }
            if (restore > 0)
            {
                if (Main.mouseItem.IsAir)
                {
                    Main.mouseItem.SetDefaults(_snapshotType);
                }
                Main.mouseItem.stack = restore;
            }
            else
            {
                Main.mouseItem.TurnToAir();
            }
            FinishRollback();
        }
        /// <summary>结束一次拖拽会话：清状态、进入回滚冷却期，并标记“刚抬起”（避免同一次操作被结算两次）。</summary>
        private static void FinishRollback()
        {
            ResetState();
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
            if (Main.mouseItem.IsAir)
            {
                return;
            }
            //残留的上一轮会话先清掉：否则快照与原始堆叠都是旧的，
            //之后回滚会把旧数据写回槽位（物品凭空多出来 / 丢失）
            if (StartSlot is not null || IsDragging)
            {
                ResetState();
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
            //这次分堆只允许搬“鼠标上仍是开始拖拽时的那份物品”。快速左右键连点时，中间的那次单击
            //可能已经把鼠标物品放进槽位、并让鼠标换成了别的物品；此时继续分堆会按快照类型造物
            //却从鼠标里扣掉另一种物品。注意：鼠标被分完（air）是正常情况，会话必须保留，
            //否则按住左键拖拽时按中键 / 右键就没得回滚了。
            if (!Main.mouseItem.IsAir
                && (Main.mouseItem.type != _snapshotType || Main.mouseItem.maxStack != _snapshotMaxStack))
            {
                return;
            }
            if (slot == StartSlot || !CanSlotAccept(slot.Item))
            {
                return;
            }
            //同一 tick 内只结算一次：绘制可能一帧被调用多次，重复结算会让同一次拖拽被应用两遍
            if (_actedThisFrame && Main.GameUpdateCount == _lastActionFrame)
            {
                return;
            }
            _actedThisFrame = true;
            _lastActionFrame = Main.GameUpdateCount;
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
        /// <summary>诊断用：本次拖拽涉及的所有槽位 + 鼠标上的物品总数。</summary>
        private static int DraggedTotal()
        {
            int total = Main.mouseItem.IsAir ? 0 : Main.mouseItem.stack;
            foreach (AvaritiaItemSlot slot in _draggedSlots)
            {
                if (slot.Item is { IsAir: false, stack: > 0 })
                {
                    total += slot.Item.stack;
                }
            }
            return total;
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