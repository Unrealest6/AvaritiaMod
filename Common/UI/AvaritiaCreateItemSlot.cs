namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 无尽贪婪合成输出槽UI元素
    /// </summary>
    public sealed class AvaritiaCreateItemSlot : AvaritiaOutputSlot
    {
        /// <summary>
        /// 缓存的配方实例，用于合成判定
        /// </summary>
        private AvaritiaRecipe? _cachedRecipe;
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Parent.Parent is not CraftingTableUI parent)
            {
                return;
            }
            _cachedRecipe = AvaritiaRecipe.FindMatchingRecipe(parent.Slots);
            if (_cachedRecipe is null)
            {
                Item = new Item();
                return;
            }
            Item = _cachedRecipe.Result.Clone();
            if (!Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                return;
            }
            int maxCount = _cachedRecipe.GetCraftableCount(parent.Slots);
            Item.stack *= Math.Max(1, maxCount);
        }
        /// <summary>
        /// 处理左键单击逻辑
        /// </summary>
        /// <param name="evt"></param>
        public override void LeftClick(UIMouseEvent evt)
        {
            //面板拖拽中的这一次点击归拖拽操作，不参与合成。
            if (DragUISession.IsAnyPanelDragging)
            {
                return;
            }
            if (Parent.Parent is CraftingTableUI parent)
            {
                if (Main.keyState.IsKeyDown(Keys.LeftShift))
                {
                    if (!CraftRepeatedly())
                    {
                        return;
                    }
                }
                else
                {
                    _cachedRecipe = AvaritiaRecipe.FindMatchingRecipe(parent.Slots);
                    if (_cachedRecipe is not null)
                    {
                        Item = _cachedRecipe.Result.Clone();
                        _cachedRecipe.ConsumeIngredients(parent.Slots);
                        //材料被直接改动过，立刻写回实体 / 服务端（不能等下一帧的变更检测）
                        AvaritiaItemSlot.SyncSlotsOfParent(parent);
                    }
                }
            }
            base.LeftClick(evt);
        }
        /// <summary>
        /// 处理右键单击逻辑
        /// </summary>
        /// <param name="evt"></param>
        public override void RightClick(UIMouseEvent evt)
        {
            if (DragUISession.IsAnyPanelDragging)
            {
                return;
            }
            if (Parent.Parent is CraftingTableUI parent)
            {
                if (Main.keyState.IsKeyDown(Keys.LeftShift))
                {
                    if (!CraftRepeatedly())
                    {
                        return;
                    }
                }
                else
                {
                    _cachedRecipe = AvaritiaRecipe.FindMatchingRecipe(parent.Slots);
                    if (_cachedRecipe is not null)
                    {
                        Item = _cachedRecipe.Result.Clone();
                        _cachedRecipe.ConsumeIngredients(parent.Slots);
                        //材料被直接改动过，立刻写回实体 / 服务端（不能等下一帧的变更检测）
                        AvaritiaItemSlot.SyncSlotsOfParent(parent);
                    }
                }
            }
            base.RightClick(evt);
        }
        /// <summary>
        /// 连续合成直到材料耗尽，并把总数量写入<see cref="AvaritiaOutputSlot.Item"/>。
        /// </summary>
        /// <returns>是否至少合成出一份物品。</returns>
        private bool CraftRepeatedly()
        {
            if (Parent.Parent is not CraftingTableUI parent)
            {
                return false;
            }
            int stack = 0;
            _cachedRecipe = AvaritiaRecipe.FindMatchingRecipe(parent.Slots);
            if (_cachedRecipe is null)
            {
                return false;
            }
            Item = _cachedRecipe.Result.Clone();
            //配方已匹配时兜底按至少一份计算，避免算出 0 时这次点击被静默吞掉。
            int maxCount = Math.Max(1, _cachedRecipe.GetCraftableCount(parent.Slots));
            for (int i = 0; i < maxCount; i++)
            {
                _cachedRecipe = AvaritiaRecipe.FindMatchingRecipe(parent.Slots);
                if (_cachedRecipe == null)
                {
                    break;
                }
                stack += _cachedRecipe.Result.stack;
                _cachedRecipe.ConsumeIngredients(parent.Slots);
                //材料被直接改动过，立刻写回实体 / 服务端
                AvaritiaItemSlot.SyncSlotsOfParent(parent);
            }
            Item.stack = stack;
            return Item.stack > 0;
        }
    }
}