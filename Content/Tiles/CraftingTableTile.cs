namespace AvaritiaMod.Content.Tiles
{
    public abstract class CraftingTableTile<TUI, TEntity, TItem> : ModTile where TUI : CraftingTableUI where TEntity : CraftingTableTileEntity where TItem : CraftingTableItem
    {
        protected virtual TileObjectData Copy => TileObjectData.Style3x2;
        protected virtual int TileWidth => 3;
        protected virtual int TileHeight => 2;
        protected virtual int[] CoordinateHeights => [16, 16];
        protected virtual int CoordinateWidth => 16;
        protected virtual int CoordinatePadding => 2;
        protected virtual int DrawYOffset => 2;
        protected virtual bool IsSolid => true;
        private Item? Item;
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
            TileID.Sets.PreventsTileHammeringIfOnTopOfIt[Type] = true;
            TileID.Sets.AvoidedByMeteorLanding[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            Main.tileSolid[Type] = IsSolid;
            if (IsSolid)
            {
                Main.tileSolidTop[Type] = true;
                Main.tileTable[Type] = true;
            }
            TileObjectData.newTile.CopyFrom(Copy);
            TileObjectData.newTile.Width = TileWidth;
            TileObjectData.newTile.Height = TileHeight;
            TileObjectData.newTile.CoordinateHeights = CoordinateHeights;
            TileObjectData.newTile.CoordinateWidth = CoordinateWidth;
            if (CoordinatePadding > 0)
            {
                TileObjectData.newTile.CoordinatePadding = CoordinatePadding;
            }
            TileObjectData.newTile.DrawYOffset = DrawYOffset;
            TileObjectData.newTile.HookPostPlaceMyPlayer = ModContent.GetInstance<TEntity>().Generic_HookPostPlaceMyPlayer;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(200, 200, 200), CreateMapEntryName());
        }
        public override bool RightClick(int i, int j)
        {
            if (!TileEntity.TryGet(i, j, out CraftingTableTileEntity tileEntity))
            {
                return false;
            }
            if (Item?.ModItem is CraftingTableItem { Items: not null } craftItem)
            {
                for (int x = 0; x < tileEntity.Size; x++)
                {
                    for (int y = 0; y < tileEntity.Size; y++)
                    {
                        tileEntity.Items?[x, y] = craftItem.Items[x, y].Clone();
                    }
                }
            }
            CraftingTableUISystem system = ModContent.GetInstance<CraftingTableUISystem>();
            if (!CraftingTableUISystem.CurrentUI?.Visible ?? true)
            {
                system.ShowUI<TUI>(tileEntity);
                Main.playerInventory = true;
                SoundEngine.PlaySound(SoundID.MenuOpen);
                CraftingTableUISystem.CurrentUI?.Visible = true;
            }
            else
            {
                system.HideUI();
                SoundEngine.PlaySound(SoundID.MenuClose);
                CraftingTableUISystem.CurrentUI?.Visible = false;
            }
            return true;
        }
        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<TItem>();
        }
        public override void PlaceInWorld(int i, int j, Item item)
        {
            TileEntity.PlaceEntityNet(i, j, ModContent.TileEntityType<TEntity>());
            Item = item.Clone();
        }
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (fail || effectOnly)
            {
                return;
            }
            CraftingTableUISystem system = ModContent.GetInstance<CraftingTableUISystem>();
            CraftingTableUISystem.CurrentUI?.OnDeactivate();
            if (system.IsTileCurrent(i, j))
            {
                system.HideUI();
                SoundEngine.PlaySound(SoundID.MenuClose);
                CraftingTableUISystem.CurrentUI?.Visible = false;
            }
            if (!TileEntity.TryGet(i, j, out CraftingTableTileEntity entity))
            {
                return;
            }
            TileEntity.ByPosition.Remove(new Point16(i, j));
            TileEntity.ByID.Remove(entity.ID);
        }
        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            if (!TileEntity.TryGet(i, j, out CraftingTableTileEntity entity))
            {
                return base.GetItemDrops(i, j);
            }
            if (Main.netMode != NetmodeID.Server || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return [];
            }
            Item item = new(ModContent.ItemType<TItem>());
            if (item.ModItem is CraftingTableItem craftItem)
            {
                for (int x = 0; x < entity.Size; x++)
                {
                    for (int y = 0; y < entity.Size; y++)
                    {
                        craftItem.Items?[x, y] = entity.Items?[x, y].Clone() ?? new Item();
                    }
                }
            }
            item.maxStack = 1;
            return [item];
        }
    }
}