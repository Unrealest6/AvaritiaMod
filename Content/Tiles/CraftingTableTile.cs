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
        /// <summary>
        /// 放置端暂存的内容物（按方块坐标）。
        /// <para>多人客户端放置时本地实体还没建立，只能先记下来，等首次右键（实体就绪）时补写；
        /// 用按坐标的字典而不是 ModTile 单例字段，避免“右键任意工作台套用上一次内容”的隐患。</para>
        /// </summary>
        private static readonly Dictionary<Point16, Item[,]> PendingItems = [];
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
            //放置端暂存的内容物：多人客户端放置时本地还没有实体（PlaceEntityNet 只是向服务端发请求），
            //所以那时写不进去，这里在实体就绪后补写一次，并且只补一次。
            Point16 clickPos = new(i, j);
            bool appliedPending = PendingItems.Remove(tileEntity.Position, out Item[,]? pending)
                                  || PendingItems.Remove(clickPos, out pending);
            if (appliedPending)
            {
                tileEntity.ApplyItems(pending);
            }
            //多人：再向服务端要一次最新内容物，覆盖“后加入的客户端 / 数据过期”的情况；
            //回包会写入实体并刷新已打开的界面（见 AvaritiaNet.HandleBroadcastWholeTable）。
            //刚补写过放置数据时不拉取，避免服务端那份（可能尚未收到上传）把本地数据覆盖成空。
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
                //多人客户端：PlaceEntityNet 只是向服务端发请求，本地这一刻还没有实体，
                //所以先把内容物暂存下来（首次右键时补写），同时上传给服务端由其写入并广播，
                //否则只有放置端能看到物品、其它端全是空的。
                PendingItems[pos] = craftItem.Items;
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
            PendingItems.Remove(new Point16(i, j));
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
            //多人客户端不生成掉落（掉落由服务端负责）；
            //原先写成“非服务端一律返回空”，导致单人模式下挖掉工作台会连同里面的物品一起消失。
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return base.GetItemDrops(i, j);
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