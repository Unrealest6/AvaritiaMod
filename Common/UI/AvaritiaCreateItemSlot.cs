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
            CraftingTableUI? parent = Parent.Parent as CraftingTableUI;
            //联机下材料在每个客户端各有一份镜像：本地判定 + 本地扣材料会让同一份材料合出多份产物，
            //因此只发“我要合成”的请求，由服务端按自己槽位里的材料结算并把产物回发到鼠标。
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (parent is not null)
                {
                    bool shift = Main.keyState.IsKeyDown(Keys.LeftShift);
                    //Shift = 连合并直接进背包（服务端会先确认背包装得下，装不下就这一次不合成）
                    AvaritiaNet.RequestCraft(parent.TileEntity.Position, shift, shift);
                }
                return;
            }
            if (parent is not null)
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
            RescueCraftedResult();
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
            CraftingTableUI? parent = Parent.Parent as CraftingTableUI;
            //联机下同样只发请求，见 LeftClick 的说明。
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (parent is not null)
                {
                    bool shift = Main.keyState.IsKeyDown(Keys.LeftShift);
                    //Shift = 连合并直接进背包（服务端会先确认背包装得下，装不下就这一次不合成）
                    AvaritiaNet.RequestCraft(parent.TileEntity.Position, shift, shift);
                }
                return;
            }
            if (parent is not null)
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
            RescueCraftedResult();
        }

        /// <summary>
        /// 兜底抢救合成结果：<see cref="AvaritiaOutputSlot.TryGiveToMouseOrInventory"/>（间接调用）没能取走的余量
        /// 必须在本次点击结束前交出去。因为材料已经被消耗，而 <see cref="Update"/> 每帧都会用配方结果覆盖
        /// <see cref="AvaritiaOutputSlot.Item"/>，留在槽里的余量下一帧就会被抹掉（材料没了、产物也没了）。
        /// </summary>
        private void RescueCraftedResult()
        {
            if (Item.IsAir || Item.stack <= 0)
            {
                return;
            }
            AvaritiaUIUtils.MoveItemToPlayerInventory(Item);
            if (Item is not { IsAir: false, stack: > 0 })
            {
                return;
            }
            //背包也放不下，掉落到玩家脚下（宁可落地也不能凭空消失）。
            Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_Misc("AvaritiaCraftingTable"), Item, Item.stack);
            Item.TurnToAir();
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