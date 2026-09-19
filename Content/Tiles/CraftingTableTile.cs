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
            //补写放置端暂存的内容物：多人客户端放置时实体尚未建立，只能等实体就绪后补一次。
            Point16 clickPos = new(i, j);
            bool appliedPending = CraftingTablePending.TryTake(tileEntity.Position, out Item[,]? pending)
                                  || CraftingTablePending.TryTake(clickPos, out pending);
            if (appliedPending)
            {
                tileEntity.ApplyItems(pending);
            }
            //多人：向服务端要一次最新内容物以覆盖过期数据；刚补写过放置数据时不拉取，避免被服务端旧数据覆盖成空。
            if (!appliedPending && Main.netMode == NetmodeID.MultiplayerClient)
            {
                AvaritiaNet.RequestTableData(tileEntity.Position);
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
            if (item.ModItem is not CraftingTableItem craftItem || craftItem.Items is null)
            {
                return;
            }
            Point16 pos = new(i, j);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                //多人客户端此刻本地还没有实体：先暂存内容物（首次右键补写），同时上传给服务端写入并广播。
                CraftingTablePending.Set(pos, craftItem.Items);
                AvaritiaNet.RequestWholeTable(pos, craftItem.Items, craftItem.Items.GetLength(0));
                return;
            }
            //单人 / 服务端：实体已经建立，直接写入（内容物为空时不要覆盖已有数据）
            if (CraftingTableTileEntity.HasAnyItem(craftItem.Items) && TileEntity.TryGet(i, j, out CraftingTableTileEntity entity))
            {
                craftItem.ApplyTo(entity);
            }
        }
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (fail || effectOnly)
            {
                return;
            }
            //方块没了就不再需要暂存的内容物
            CraftingTablePending.Remove(new Point16(i, j));
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
            //多人客户端不产出任何物品：内容物只存在于服务端实体，客户端本地破坏会凭空生成空工作台并同步给服务端。
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return [];
            }
            if (!TileEntity.TryGet(i, j, out CraftingTableTileEntity entity))
            {
                return base.GetItemDrops(i, j);
            }
            Item item = new(ModContent.ItemType<TItem>());
            //内容物交给物品自己写入：它同时判定是否带内容物并据此设置 maxStack = 1（Item.Clone 会重置 maxStack）。
            CraftingTableItem? craftItem = item.ModItem as CraftingTableItem;
            craftItem?.SetContents(entity.Items);
            return [item];
        }
    }
}