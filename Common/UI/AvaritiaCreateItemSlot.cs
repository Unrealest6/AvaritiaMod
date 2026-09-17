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
            //面板正在被拖拽时，这一次点击属于拖拽操作（拖拽开始时会丢弃按下缓存），不参与合成。
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
                    }
                }
            }
            base.RightClick(evt);
        }
        /// <summary>
        /// 判断能否合成>0数量的物品并将其克隆到<see cref="AvaritiaOutputSlot.Item"/>用于合成判定
        /// </summary>
        /// <returns>能否合成>0数量的物品</returns>
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
            //配方已经匹配就不可能一份都合不出来；这里兜底至少合成一份，
            //避免数量算成 0 时这一次点击被静默吞掉（什么都不会发生）。
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
            }
            Item.stack = stack;
            return Item.stack > 0;
        }
    }
}