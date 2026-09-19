namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 无尽贪婪合成输入槽UI元素
    /// </summary>
    public sealed class AvaritiaItemSlot : AvaritiaInputSlot
    {
        /// <summary>
        /// 上一次双击所在的帧号，用于抑制紧随其后的单击。
        /// </summary>
        private static ulong _lastDoubleClickFrame;
        /// <summary>
        /// 选中配方时填充的所需材料列表，用于槽位提示与红色不足标记。
        /// </summary>
        public List<Item> ShowItems { get; set; } = [];
        /// <summary>
        /// 槽位X坐标
        /// </summary>
        private int SlotX { get; }
        /// <summary>
        /// 槽位Y坐标
        /// </summary>
        private int SlotY { get; }
        /// <summary>
        /// 构造方法，初始化槽位坐标与物品
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public AvaritiaItemSlot(int x, int y)
        {
            SlotX = x;
            SlotY = y;
            Item = new Item();
        }
        /// <summary>
        /// 绘制槽位中物品或展示物品
        /// </summary>
        /// <param name="spriteBatch"></param>
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
            if (DragUISession.IsAnyPanelDragging)
            {
                //面板拖拽中：取消槽位拖拽且不做分堆处理。
                DragManager.CancelActiveDrag();
                return;
            }
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
        /// <summary>
        /// 处理左键单击逻辑
        /// </summary>
        /// <param name="evt"></param>
        public override void LeftClick(UIMouseEvent evt)
        {
            if (Main.GameUpdateCount <= _lastDoubleClickFrame + 12)
            {
                return;
            }
            if (DragManager.IsInRollbackCooldown)
            {
                return;
            }
            base.LeftClick(evt);
        }
        /// <summary>
        /// 处理左键双击逻辑
        /// </summary>
        /// <param name="evt"></param>
        public override void LeftDoubleClick(UIMouseEvent evt)
        {
            base.LeftDoubleClick(evt);
            if (!Item.IsAir || Main.mouseItem.IsAir)
            {
                return;
            }
            int startStack = Main.mouseItem.stack;
            if (Parent.Parent is not CraftingTableUI parent || parent.Slots is null)
            {
                return;
            }
            foreach (AvaritiaItemSlot slot in parent.Slots)
            {
                if (slot.Item.IsAir || slot.Item.type != Main.mouseItem.type)
                {
                    continue;
                }
                int space = Main.mouseItem.maxStack - Main.mouseItem.stack;
                if (space <= 0)
                {
                    break;
                }
                if (space >= slot.Item.stack)
                {
                    Main.mouseItem.stack += slot.Item.stack;
                    slot.Item.TurnToAir();
                }
                else
                {
                    slot.Item.stack -= space;
                    Main.mouseItem.stack = Main.mouseItem.maxStack;
                    return;
                }
            }
            for (int i = 0; i < 50; i++)
            {
                Item inv = Main.LocalPlayer.inventory[i];
                if (inv?.IsAir != false || inv.type != Main.mouseItem.type)
                {
                    continue;
                }
                int space = Main.mouseItem.maxStack - Main.mouseItem.stack;
                if (space <= 0)
                {
                    break;
                }
                if (space >= inv.stack)
                {
                    Main.mouseItem.stack += inv.stack;
                    inv.TurnToAir();
                }
                else
                {
                    inv.stack -= space;
                    Main.mouseItem.stack = Main.mouseItem.maxStack;
                    return;
                }
            }
            if (Main.mouseItem.stack == startStack)
            {
                return;
            }
            //直接改动过槽位，必须立刻写回实体 / 服务端（不能等下一帧的变更检测）
            SyncSlotsOfParent(parent);
            SoundEngine.PlaySound(SoundID.Grab);
            _lastDoubleClickFrame = Main.GameUpdateCount;
            Recipe.FindRecipes();
        }
        /// <summary>
        /// 处理右键单击逻辑
        /// </summary>
        /// <param name="evt"></param>
        public override void RightClick(UIMouseEvent evt)
        {
            if (DragManager.IsInRollbackCooldown)
            {
                return;
            }
            base.RightClick(evt);
        }
        /// <summary>
        /// 处理左键按下逻辑
        /// </summary>
        /// <param name="evt"></param>
        public override void LeftMouseDown(UIMouseEvent evt)
        {
            base.LeftMouseDown(evt);
            if (DragUISession.IsAnyPanelDragging)
            {
                return;
            }
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
        /// <summary>
        /// 处理右键按下逻辑
        /// </summary>
        /// <param name="evt"></param>
        public override void RightMouseDown(UIMouseEvent evt)
        {
            base.RightMouseDown(evt);
            if (DragUISession.IsAnyPanelDragging)
            {
                return;
            }
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
        /// <summary>
        /// 处理中键按下逻辑
        /// </summary>
        /// <param name="evt"></param>
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
        /// <summary>
        /// 处理左键抬起逻辑
        /// </summary>
        /// <param name="evt"></param>
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
        /// <summary>
        /// 处理右键抬起逻辑
        /// </summary>
        /// <param name="evt"></param>
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
        /// <summary>
        /// 处理中键抬起逻辑
        /// </summary>
        /// <param name="evt"></param>
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
        /// <summary>
        /// 处理鼠标经过逻辑
        /// </summary>
        /// <param name="evt"></param>
        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            if (DragUISession.IsAnyPanelDragging)
            {
                return;
            }
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
        /// <summary>把该面板所有输入槽立即写回实体 / 服务端（直接改动过槽位内容后调用）。</summary>
        public static void SyncSlotsOfParent(CraftingTableUI parent)
        {
            if (parent.Slots is null)
            {
                return;
            }
            foreach (AvaritiaItemSlot slot in parent.Slots)
            {
                slot.SyncItem();
            }
        }
    }
}