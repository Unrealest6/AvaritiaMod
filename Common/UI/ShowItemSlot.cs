namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 展示物品槽位UI元素
    /// </summary>
    public sealed class ShowItemSlot : UIElement
    {
        /// <summary>
        /// 物品实例
        /// </summary>
        public Item Item { get; set; } = new();
        /// <summary>
        /// 构造方法，初始化UI大小
        /// </summary>
        public ShowItemSlot()
        {
            Width.Set(52, 0f);
            Height.Set(52, 0f);
        }
        /// <summary>
        /// 处理并绘制需要展示的物品
        /// </summary>
        /// <param name="spriteBatch"></param>
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            if (Item.IsAir)
            {
                return;
            }
            Rectangle rect = GetDimensions().ToRectangle();
            Texture2D bg = IsMouseHovering ? TextureAssets.InventoryBack7.Value : TextureAssets.InventoryBack.Value;
            AvaritiaUIUtils.DrawItemSlot(spriteBatch, Item, rect.TopLeft(), bg);
            if (!IsMouseHovering)
            {
                return;
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
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        /// <summary>
        /// 处理右键单击逻辑
        /// </summary>
        /// <param name="evt"></param>
        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
    }
}