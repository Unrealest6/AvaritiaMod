namespace AvaritiaMod.Content.Tiles
{
    public sealed class NeutroniumCompressorTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            HitSound = SoundID.Dig;
            DustType = DustID.Vortex;
            MinPick = 210;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.PreventsTileRemovalIfOnTopOfIt[Type] = true;
            TileID.Sets.PreventsTileHammeringIfOnTopOfIt[Type] = true;
            TileID.Sets.AvoidedByMeteorLanding[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            Main.tileSolid[Type] = false;
            Main.tileSolidTop[Type] = false;
            Main.tileTable[Type] = false;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
            TileObjectData.newTile.Width = 3;
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.newTile.HookPostPlaceMyPlayer = ModContent.GetInstance<NeutroniumCompressorTileEntity>().Generic_HookPostPlaceMyPlayer;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(200, 200, 200), CreateMapEntryName());
        }
        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            if (!TileEntity.TryGet(i, j, out NeutroniumCompressorTileEntity entity))
            {
                return;
            }
            if (entity.IsWorking)
            {
                frameYOffset += (int)(52 * (Main.GameUpdateCount / 12 % 5));
            }
        }
        public override bool RightClick(int i, int j)
        {
            if (!TileEntity.TryGet(i, j, out NeutroniumCompressorTileEntity myTileEntity))
            {
                return true;
            }
            if (!NeutroniumCompressorUI.Visible)
            {
                ModContent.GetInstance<NeutroniumCompressorUISystem>().ShowMyUI(myTileEntity);
                Main.playerInventory = true;
                SoundEngine.PlaySound(SoundID.MenuOpen);
                NeutroniumCompressorUI.Visible = true;
            }
            else
            {
                ModContent.GetInstance<NeutroniumCompressorUISystem>().HideUI();
                SoundEngine.PlaySound(SoundID.MenuClose);
                NeutroniumCompressorUI.Visible = false;
            }
            return true;
        }
        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<NeutroniumCompressor>();
        }
        public override void PlaceInWorld(int i, int j, Item item)
        {
            int id = ModContent.TileEntityType<NeutroniumCompressorTileEntity>();
            TileEntity.PlaceEntityNet(i, j, id);
        }
        public override void KillTile(int i, int j, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (fail || effectOnly)
            {
                return;
            }
            NeutroniumCompressorUISystem uiSystem = ModContent.GetInstance<NeutroniumCompressorUISystem>();
            uiSystem.CurrentUI?.OnDeactivate();
            if (uiSystem.IsTileCurrent(i, j))
            {
                uiSystem.HideUI();
                SoundEngine.PlaySound(SoundID.MenuClose);
                NeutroniumCompressorUI.Visible = false;
            }
            if (!TileEntity.TryGet(i, j, out NeutroniumCompressorTileEntity entity))
            {
                return;
            }
            TileEntity.ByPosition.Remove(new Point16(i, j));
            TileEntity.ByID.Remove(entity.ID);
        }
        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            if (!TileEntity.TryGet(i, j, out NeutroniumCompressorTileEntity entity) || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return base.GetItemDrops(i, j);
            }
            List<Item> items = [.. base.GetItemDrops(i, j), entity.InputItem, entity.OutputItem];
            return items;
        }
    }
}