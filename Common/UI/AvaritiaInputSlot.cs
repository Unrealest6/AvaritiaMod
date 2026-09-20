namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 无尽贪婪的输入槽UI元素
    /// </summary>
    public abstract class AvaritiaInputSlot : UIElement
    {
        /// <summary>
        /// 槽位物品
        /// </summary>
        public Item Item { get; set; } = new();
        /// <summary>
        /// 用于检测变化的上一次物品快照
        /// </summary>
        private Item OldItem { get; set; }
        /// <summary>
        /// 初始化尺寸，并把<see cref="Item"/>同步到<see cref="OldItem"/>
        /// </summary>
        protected AvaritiaInputSlot()
        {
            OldItem = Item.Clone();
            Width.Set(52, 0f);
            Height.Set(52, 0f);
        }
        /// <summary>
        /// 静默改变<see cref="Item"/>和<see cref="OldItem"/>，防止触发<see cref="OnItemChanged"/>方法
        /// </summary>
        /// <param name="newItem">新物品实例</param>
        public void SetItemSilently(Item newItem)
        {
            //只在真正发生变化时记日志：单机 / 服务端的压缩机每帧都会调用这个方法，
            //无条件记录会把日志刷满。
            Item = newItem.Clone();
            OldItem = Item.Clone();
        }
        /// <summary>当<see cref="Item"/>与<see cref="OldItem"/>不同时触发</summary>
        protected virtual void OnItemChanged() { }
        /// <summary>
        /// 立即把当前物品写回数据源（物块实体 / 服务端）。
        /// <para>直接改动 <see cref="Item"/> 的地方必须调用：只靠 <see cref="Update"/> 的变更检测时，
        /// 界面若在同一帧被关闭 / 重开，就会把数据源里的旧内容搬回槽位，而玩家手上已经拿到了物品（刷物品）。</para>
        /// </summary>
        public void SyncItem() => OnItemChanged();
        /// <summary>
        /// 执行一次“鼠标 ↔ 槽位”的物品移动，并保证两者物品总数不变。
        /// <para>总数一旦变化（出现了意料之外的复制路径），立刻回滚到交互前的状态并记日志：
        /// 宁可这一次点击不生效，也不能凭空多出物品。</para>
        /// </summary>
        protected void GuardConservation(Action transfer)
        {
            Item slotBefore = Item.Clone();
            Item mouseBefore = Main.mouseItem.Clone();
            int before = StackOf(slotBefore) + StackOf(mouseBefore);
            transfer();
            int after = StackOf(Item) + StackOf(Main.mouseItem);
            if (after == before)
            {
                return;
            }
            Item slotAfter = Item.Clone();
            Item mouseAfter = Main.mouseItem.Clone();
            Item = slotBefore;
            Main.mouseItem = mouseBefore;
            OnItemChanged();
            Warn($"[SlotGuard] A slot interaction changed the item total ({before} -> {after}); it was rolled back. "
                + $"slot={Describe(slotAfter)} mouse={Describe(mouseAfter)} slotBefore={Describe(slotBefore)} mouseBefore={Describe(mouseBefore)}");
        }
        private static int StackOf(Item item) => item.IsAir || item.stack <= 0 ? 0 : item.stack;
        private static string Describe(Item item)
            => item.IsAir || item.stack <= 0 ? "air" : $"{item.type}x{item.stack}";
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            //Item 未重写 Equals，只能逐字段比对。
            if (OldItem.type == Item.type && OldItem.stack == Item.stack && OldItem.prefix == Item.prefix && OldItem.maxStack == Item.maxStack
                && OldItem.damage == Item.damage && OldItem.crit == Item.crit && OldItem.defense == Item.defense
                && OldItem.DamageType == Item.DamageType && OldItem.shoot == Item.shoot)
            {
                return;
            }
            OnItemChanged();
            OldItem = Item.Clone();
        }
        /// <summary>
        /// 处理槽位及其中物品的绘制
        /// </summary>
        /// <param name="spriteBatch"></param>
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle rect = GetDimensions().ToRectangle();
            Texture2D bg = IsMouseHovering ? TextureAssets.InventoryBack14.Value : TextureAssets.InventoryBack.Value;
            AvaritiaUIUtils.DrawItemSlot(spriteBatch, Item, rect.TopLeft(), bg);
            if (!IsMouseHovering)
            {
                return;
            }
            if (Item.IsAir)
            {
                return;
            }
            if (ItemSlot.ControlInUse)
            {
                Main.cursorOverride = 6;
            }
            Main.HoverItem = Item.Clone();
            Main.hoverItemName = Item.Name;
        }
        /// <summary>
        /// 处理左键单击逻辑
        /// </summary>
        /// <param name="evt"></param>
        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            switch (Main.mouseItem.IsAir)
            {
                //鼠标空 + 槽位有物品 + Shift / Ctrl：送背包 / 丢垃圾桶（物品总数本来就会变，不走守恒检查）
                case true when !Item.IsAir && (ItemSlot.ShiftInUse || ItemSlot.ControlInUse):
                    {
                        if (ItemSlot.ShiftInUse)
                        {
                            GiveSlotToInventory();
                            return;
                        }
                        TrashSlotItem();
                        return;
                    }
                //鼠标有物品 + 槽位有物品 + Shift：把槽位物品送背包
                case false when !Item.IsAir && Main.keyState.IsKeyDown(Keys.LeftShift):
                    GiveSlotToInventory();
                    return;
                default:
                    GuardConservation(TransferOnLeftClick);
                    break;
            }
        }
        /// <summary>
        /// 处理右键单击逻辑
        /// </summary>
        /// <param name="evt"></param>
        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            GuardConservation(TransferOnRightClick);
        }
        /// <summary>左键的“鼠标 ↔ 槽位”移动（不含背包 / 垃圾桶分支）。</summary>
        private void TransferOnLeftClick()
        {
            if (Main.mouseItem.IsAir)
            {
                if (Item.IsAir)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    return;
                }
                Main.mouseItem = Item.Clone();
                Item.TurnToAir();
                OnItemChanged();
                SoundEngine.PlaySound(SoundID.Grab);
                return;
            }
            if (Item.IsAir)
            {
                Item = Main.mouseItem.Clone();
                OnItemChanged();
                Main.mouseItem.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
                return;
            }
            if (Main.mouseItem.type == Item.type && Main.mouseItem.maxStack == Item.maxStack && Item.stack < Item.maxStack)
            {
                int canAdd = Item.maxStack - Item.stack;
                if (canAdd >= Main.mouseItem.stack)
                {
                    Item.stack += Main.mouseItem.stack;
                    Main.mouseItem.TurnToAir();
                    SoundEngine.PlaySound(SoundID.Grab);
                }
                else
                {
                    Main.mouseItem.stack -= canAdd;
                    Item.stack = Item.maxStack;
                    SoundEngine.PlaySound(SoundID.MenuTick);
                }
                OnItemChanged();
                return;
            }
            SwapWithMouse();
        }
        /// <summary>右键的“鼠标 ↔ 槽位”移动。</summary>
        private void TransferOnRightClick()
        {
            if (Main.mouseItem.IsAir)
            {
                if (Item.IsAir)
                {
                    return;
                }
                int half = (int)Math.Ceiling(Item.stack / 2d);
                Main.mouseItem = new Item(Item.type, half);
                Item.stack -= half;
                if (Item.stack <= 0)
                {
                    Item.TurnToAir();
                }
                OnItemChanged();
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            if (Item.IsAir)
            {
                Item = new Item(Main.mouseItem.type);
                OnItemChanged();
                if (--Main.mouseItem.stack <= 0)
                {
                    Main.mouseItem.TurnToAir();
                }
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            if (Item.type == Main.mouseItem.type && Item.stack < Item.maxStack)
            {
                Item.stack++;
                OnItemChanged();
                if (--Main.mouseItem.stack <= 0)
                {
                    Main.mouseItem.TurnToAir();
                }
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            SwapWithMouse();
        }
        /// <summary>把槽位物品送进玩家背包。</summary>
        private void GiveSlotToInventory()
        {
            if (Item.IsAir)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            AvaritiaUIUtils.MoveItemToPlayerInventory(Item);
            OnItemChanged();
        }
        /// <summary>把槽位物品丢进垃圾桶。</summary>
        private void TrashSlotItem()
        {
            if (Item.IsAir)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            Main.LocalPlayer.trashItem = Item.Clone();
            Item.TurnToAir();
            OnItemChanged();
            SoundEngine.PlaySound(SoundID.Grab);
        }
        /// <summary>
        /// 交换鼠标和槽位中物品
        /// </summary>
        private void SwapWithMouse()
        {
            (Main.mouseItem, Item) = (Item.Clone(), Main.mouseItem.Clone());
            OnItemChanged();
            SoundEngine.PlaySound(SoundID.Grab);
        }
    }
}
