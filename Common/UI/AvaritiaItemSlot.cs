namespace AvaritiaMod.Common.UI
{
    public sealed class AvaritiaItemSlot : AvaritiaInputSlot
    {
        private int SlotX { get; }
        private int SlotY { get; }
        private const int DoubleClickCooldownFrames = 12;
        public List<Item> ShowItems { get; set; } = [];
        private static ulong _lastDoubleClickFrame;
        private void GatherFromAllSlots(ref Item target)
        {
            if (Parent.Parent is not CraftingTableUI parent || parent.Slots is null)
            {
                return;
            }
            foreach (AvaritiaItemSlot slot in parent.Slots)
            {
                if (slot.Item.IsAir || slot.Item.type != target.type)
                {
                    continue;
                }
                int space = target.maxStack - target.stack;
                if (space <= 0)
                {
                    break;
                }
                if (space >= slot.Item.stack)
                {
                    target.stack += slot.Item.stack;
                    slot.Item.TurnToAir();
                }
                else
                {
                    slot.Item.stack -= space;
                    target.stack = target.maxStack;
                    return;
                }
            }
        }
        private static void GatherFromPlayerInventory(ref Item target)
        {
            for (int i = 0; i < 50; i++)
            {
                Item inv = Main.LocalPlayer.inventory[i];
                if (inv?.IsAir != false || inv.type != target.type)
                {
                    continue;
                }
                int space = target.maxStack - target.stack;
                if (space <= 0)
                {
                    break;
                }
                if (space >= inv.stack)
                {
                    target.stack += inv.stack;
                    inv.TurnToAir();
                }
                else
                {
                    inv.stack -= space;
                    target.stack = target.maxStack;
                    return;
                }
            }
        }
        public AvaritiaItemSlot(int x, int y)
        {
            SlotX = x;
            SlotY = y;
            Item = new Item();
        }
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle rect = GetDimensions().ToRectangle();
            if (!Item.IsAir)
            {
                AvaritiaUIUtils.DrawItemSlot(spriteBatch, Item, rect.TopLeft(),
                    IsMouseHovering ? TextureAssets.InventoryBack14.Value : TextureAssets.InventoryBack.Value);
                if (ShowItems.Count > 0 && (ShowItems.All(item => item.type != Item.type) || (ShowItems.FirstOrDefault(item => item.type == Item.type)?.stack ?? int.MaxValue) > Item.stack))
                {
                    spriteBatch.Draw(TextureAssets.InventoryBack11.Value, rect.TopLeft(), null, Color.Red * 0.2f, 0f,
                        Vector2.Zero, 1, SpriteEffects.None, 0f);
                }
            }
            else
            {
                AvaritiaUIUtils.DrawItemSlot(spriteBatch, ShowItems.Count == 0 ? new Item() : ShowItems[(int)(Main.GameUpdateCount / 60 % ShowItems.Count)], rect.TopLeft(),
                    IsMouseHovering ? TextureAssets.InventoryBack14.Value : TextureAssets.InventoryBack.Value,
                    itemColor: Color.White * 0.4f);
                if (ShowItems.Count > 0)
                {
                    spriteBatch.Draw(TextureAssets.InventoryBack11.Value, rect.TopLeft(), null, Color.Red * 0.2f, 0f,
                        Vector2.Zero, 1, SpriteEffects.None, 0f);
                }
            }
            if (DragManager.OriginStacksContainsKey(this))
            {
                spriteBatch.Draw(TextureAssets.InventoryBack13.Value, rect.TopLeft(), null, Color.White * 0.2f, 0f,
                    Vector2.Zero, 1, SpriteEffects.None, 0f);
            }
            if (IsMouseHovering)
            {
                if (!Item.IsAir)
                {
                    if (ItemSlot.ControlInUse)
                    {
                        Main.cursorOverride = 6;
                    }
                    Main.HoverItem = Item.Clone();
                    Main.hoverItemName = Item.Name;
                }
                else if (ShowItems.Count > 0)
                {
                    Main.HoverItem = ShowItems[(int)(Main.GameUpdateCount / 60 % ShowItems.Count)];
                    Main.hoverItemName = ShowItems[(int)(Main.GameUpdateCount / 60 % ShowItems.Count)].Name;
                }
            }
            DragManager.JustReleased = false;
            if (DragManager.IsDragging && IsMouseHovering)
            {
                DragManager.OnSlotHovered(this);
            }
            if (DragManager.StartSlot == null || DragManager.JustReleased)
            {
                return;
            }
            bool buttonReleased = DragManager.StartType == DragManager.DragType.Left ? !Main.mouseLeft : !Main.mouseRight;
            if (buttonReleased)
            {
                DragManager.MouseUp();
            }
        }
        public override void LeftClick(UIMouseEvent evt)
        {
            if (Main.GameUpdateCount <= _lastDoubleClickFrame + DoubleClickCooldownFrames)
            {
                return;
            }
            if (DragManager.IsInRollbackCooldown)
            {
                return;
            }
            base.LeftClick(evt);
        }
        public override void LeftDoubleClick(UIMouseEvent evt)
        {
            base.LeftDoubleClick(evt);
            if (!Item.IsAir || Main.mouseItem.IsAir)
            {
                return;
            }
            int startStack = Main.mouseItem.stack;
            GatherFromAllSlots(ref Main.mouseItem);
            GatherFromPlayerInventory(ref Main.mouseItem);
            if (Main.mouseItem.stack == startStack)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Grab);
            _lastDoubleClickFrame = Main.GameUpdateCount;
            Recipe.FindRecipes();
        }
        public override void RightClick(UIMouseEvent evt)
        {
            if (DragManager.IsInRollbackCooldown)
            {
                return;
            }
            base.RightClick(evt);
        }
        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            if (DragManager.IsDragging && DragManager.StartType == DragManager.DragType.Right)
            {
                DragManager.RollbackDrag();
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            if (!Main.mouseItem.IsAir)
            {
                DragManager.MouseDown(DragManager.DragType.Left, this);
            }
        }
        public override void RightMouseDown(UIMouseEvent evt)
        {
            base.RightMouseDown(evt);
            if (DragManager.IsDragging && DragManager.StartType == DragManager.DragType.Left)
            {
                DragManager.RollbackDrag();
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            if (!Main.mouseItem.IsAir)
            {
                DragManager.MouseDown(DragManager.DragType.Right, this);
            }
        }
        public override void MiddleMouseDown(UIMouseEvent evt)
        {
            base.MiddleMouseDown(evt);
            if (!DragManager.IsDragging)
            {
                return;
            }
            DragManager.RollbackDrag();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        public override void LeftMouseUp(UIMouseEvent evt)
        {
            base.LeftMouseUp(evt);
            if (!DragManager.IsDragging || DragManager.StartType != DragManager.DragType.Right)
            {
                return;
            }
            DragManager.RollbackDrag();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        public override void RightMouseUp(UIMouseEvent evt)
        {
            base.RightMouseUp(evt);
            if (!DragManager.IsDragging || DragManager.StartType != DragManager.DragType.Left)
            {
                return;
            }
            DragManager.RollbackDrag();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        public override void MiddleMouseUp(UIMouseEvent evt)
        {
            base.MiddleMouseUp(evt);
            if (!DragManager.IsDragging)
            {
                return;
            }
            DragManager.RollbackDrag();
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            if (DragManager.StartSlot != null)
            {
                DragManager.OnSlotHovered(this);
            }
        }
        protected override void OnItemChanged()
        {
            if (Parent.Parent is not CraftingTableUI parent)
            {
                return;
            }
            parent.TileEntity.Items?[SlotX, SlotY] = Item.Clone();
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                CraftingTableTileEntity.SendSlotChange(parent.TileEntity.Position, SlotX, SlotY, Item.Clone());
            }
        }
    }
}