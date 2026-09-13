namespace AvaritiaMod.Common.UI
{
    public abstract class AvaritiaInputSlot : UIElement
    {
        public Item Item { get; set; } = new();
        protected Item OldItem { get; set; }
        protected AvaritiaInputSlot()
        {
            OldItem = Item.Clone();
            Width.Set(52, 0f);
            Height.Set(52, 0f);
        }
        public void SetItemSilently(Item newItem)
        {
            Item = newItem.Clone();
            OldItem = Item.Clone();
        }
        protected virtual void OnItemChanged() { }
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
        private void SwapWithMouse()
        {
            (Main.mouseItem, Item) = (Item.Clone(), Main.mouseItem.Clone());
            OnItemChanged();
            SoundEngine.PlaySound(SoundID.Grab);
        }
    }
}