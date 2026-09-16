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
        /// 槽位前物品，用于物品改变时判定
        /// </summary>
        private Item OldItem { get; set; }
        /// <summary>
        /// 构造方法，初始化UI和将<see cref="Item"/>克隆到<see cref="OldItem"/>
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
            Item = newItem.Clone();
            OldItem = Item.Clone();
        }
        /// <summary>
        /// 当<see cref="Item"/>与<see cref="OldItem"/>不同时触发
        /// </summary>
        protected virtual void OnItemChanged() { }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            //判定OldItem是否与Item相同，不相同则触发OnItemChanged方法并将Item克隆到OldItem
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
            //鼠标位于槽位范围内并且槽位中物品不为空时将Item克隆到HoverItem
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
            if (Main.mouseItem.IsAir)
            {
                if (Item.IsAir)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick);
                    return;
                }
                if (ItemSlot.ShiftInUse)
                {
                    AvaritiaUIUtils.MoveItemToPlayerInventory(Item);
                    OnItemChanged();
                }
                else if (ItemSlot.ControlInUse)
                {
                    Main.LocalPlayer.trashItem = Item.Clone();
                    Item.TurnToAir();
                    OnItemChanged();
                    SoundEngine.PlaySound(SoundID.Grab);
                }
                else
                {
                    Main.mouseItem = Item.Clone();
                    Item.TurnToAir();
                    OnItemChanged();
                    SoundEngine.PlaySound(SoundID.Grab);
                }
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
            if (Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                AvaritiaUIUtils.MoveItemToPlayerInventory(Item);
                OnItemChanged();
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
        /// <summary>
        /// 处理右键单击逻辑
        /// </summary>
        /// <param name="evt"></param>
        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
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