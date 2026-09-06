namespace AvaritiaMod.Common.UI
{
    public abstract class AvaritiaOutputSlot : UIElement
    {
        public Item Item { get; set; } = new();
        protected Item OldItem { get; set; }
        private static bool TryGiveToMouseOrInventory(Item item, bool shift)
        {
            if (item.IsAir)
            {
                return false;
            }
            if (shift)
            {
                AvaritiaUIUtils.MoveItemToPlayerInventory(item);
                return item.IsAir;
            }
            if (Main.mouseItem.IsAir)
            {
                Main.mouseItem = item.Clone();
                item.TurnToAir();
                return true;
            }
            if (Main.mouseItem.type != item.type || Main.mouseItem.maxStack != item.maxStack)
            {
                return false;
            }
            int space = Main.mouseItem.maxStack - Main.mouseItem.stack;
            if (space >= item.stack)
            {
                Main.mouseItem.stack += item.stack;
                item.TurnToAir();
                return true;
            }
            item.stack -= space;
            Main.mouseItem.stack = item.maxStack;
            return false;
        }
        protected AvaritiaOutputSlot()
        {
            OldItem = Item.Clone();
            Width.Set(78, 0);
            Height.Set(78, 0);
        }
        public void SetItemSilently(Item newItem)
        {
            Item = newItem.Clone();
            OldItem = Item.Clone();
        }
        protected virtual void OnItemChanged() { }
        protected virtual void MouseClick(UIMouseEvent evt)
        {
            if (Item.IsAir)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            bool shift = Main.keyState.IsKeyDown(Keys.LeftShift);
            if (!TryGiveToMouseOrInventory(Item, shift))
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Grab);
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (OldItem.type == Item.type && OldItem.stack == Item.stack && OldItem.prefix == Item.prefix && OldItem.maxStack == Item.maxStack
                && OldItem.damage == Item.damage && OldItem.crit == Item.crit && OldItem.defense == Item.defense
                && OldItem.DamageType == Item.DamageType && OldItem.shoot == Item.shoot)
            {
                return;
            }
            OnItemChanged();
            OldItem = Item.Clone();
        }
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
    }
}