namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 无尽贪婪的输出槽UI元素
    /// </summary>
    public abstract class AvaritiaOutputSlot : UIElement
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
        protected AvaritiaOutputSlot()
        {
            OldItem = Item.Clone();
            Width.Set(78, 0);
            Height.Set(78, 0);
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
        /// <summary>
        /// 处理鼠标单击逻辑
        /// </summary>
        /// <param name="evt"></param>
        protected virtual void MouseClick(UIMouseEvent evt)
        {
            if (Item.IsAir)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            bool shift = Main.keyState.IsKeyDown(Keys.LeftShift);
            if (!TryGiveToMouseOrInventory(shift))
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Grab);
        }
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
            Texture2D bg = IsMouseHovering ? TextureAssets.InventoryBack15.Value : TextureAssets.InventoryBack4.Value;
            AvaritiaUIUtils.DrawItemSlot(spriteBatch, Item, GetDimensions().ToRectangle().TopLeft(), bg, scale: 1.5f);
            if (!IsMouseHovering || Item.IsAir)
            {
                return;
            }
            Main.HoverItem = Item.Clone();
            Main.hoverItemName = Item.Name;
            Main.LocalPlayer.mouseInterface = true;
        }
        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            MouseClick(evt);
        }
        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            MouseClick(evt);
        }
        /// <summary>
        /// 尝试将<see cref="Item"/>获取到鼠标或物品栏中
        /// </summary>
        /// <param name="shift">是否按下shift</param>
        /// <returns></returns>
        private bool TryGiveToMouseOrInventory(bool shift)
        {
            if (Item.IsAir)
            {
                return false;
            }
            if (shift)
            {
                AvaritiaUIUtils.MoveItemToPlayerInventory(Item);
                OnItemChanged();
                return Item.IsAir;
            }
            if (Main.mouseItem.IsAir)
            {
                Main.mouseItem = Item.Clone();
                Item.TurnToAir();
                OnItemChanged();
                return true;
            }
            if (Main.mouseItem.type != Item.type || Main.mouseItem.maxStack != Item.maxStack)
            {
                return false;
            }
            int space = Main.mouseItem.maxStack - Main.mouseItem.stack;
            if (space >= Item.stack)
            {
                Main.mouseItem.stack += Item.stack;
                Item.TurnToAir();
                OnItemChanged();
                return true;
            }
            Item.stack -= space;
            OnItemChanged();
            Main.mouseItem.stack = Item.maxStack;
            return false;
        }
    }
}