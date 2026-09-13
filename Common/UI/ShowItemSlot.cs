namespace AvaritiaMod.Common.UI
{
    public sealed class ShowItemSlot : UIElement
    {
        public Item Item { get; set; } = new();
        public ShowItemSlot()
        {
            Width.Set(52, 0f);
            Height.Set(52, 0f);
        }
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
        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
    }
}