namespace AvaritiaMod.Content.Items.Tools
{
    public sealed class WorldBreaker : FrameItem
    {
        public override Dictionary<byte, FrameTexture?> FrameTextures => new()
        {
            [1] = FrameTextureSystem.Register("WorldBreaker2", "AvaritiaMod/Content/Items/Tools/WorldBreaker2", 9, FrameTimeline, 3)
        };
        protected override FrameDef[] FrameTimeline =>
        [
            0, 0, 0,
            1, 1, 1,
            2, 2, 2,
            3, 3,
            4, 4,
            5,
            6,
            7,
            8,
            7, 6, 5,
            4, 4, 3, 3,
            2, 2, 2, 1, 1, 1
        ];
        protected override int FrameCount => 9;
        protected override int FrameDuration => 3;
        public override string Texture => "AvaritiaMod/Content/Items/Tools/WorldBreaker1";
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.damage = 8;
            Item.DamageType = DamageClass.Melee;
            Item.pick = int.MaxValue;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 4;
            Item.value = 0;
            Item.rare = ModContent.RarityType<LightRedRarity>();
            Item.UseSound = SoundID.Item1;
            Item.useTurn = true;
            Item.autoReuse = false;
            Item.noUseGraphic = false;
            Item.noMelee = false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips.Where(line => line.Mod == "Terraria"))
            {
                switch (line.Name)
                {
                    case "PickPower":
                        line.Hide();
                        break;
                }
            }
        }
        public override void HoldItem(Player player)
        {
            if (!Main.keyState.IsKeyDown(Keys.LeftShift) || !Main.mouseRight || !Main.mouseRightRelease)
            {
                return;
            }
            Mode = (byte)(Mode == 0 ? 1 : 0);
            Item.knockBack = Mode == 0 ? 2 : 32;
            Item.pick = Mode == 0 ? int.MaxValue : 0;
        }
        public override bool CanUseItem(Player player)
        {
            if (!player.IsInTileInteractionRange((int)(Main.MouseWorld.X / 16), (int)(Main.MouseWorld.Y / 16),
                    TileReachCheckSettings.Simple) || !Main.tile[(int)Main.MouseWorld.X / 16, (int)Main.MouseWorld.Y / 16].HasTile || Mode != 1)
            {
                return base.CanUseItem(player);
            }
            List<Item> drops = [];
            int baseX = (int)(Main.MouseWorld.X / 16);
            int baseY = (int)(Main.MouseWorld.Y / 16);
            for (int dx = -14; dx < 14; dx++)
            {
                for (int dy = -14; dy < 14; dy++)
                {
                    int x = baseX + dx;
                    int y = baseY + dy;
                    if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
                    {
                        continue;
                    }
                    Tile tile = Main.tile[x, y];
                    Tile tileSafely = Framing.GetTileSafely(x, y);
                    if (tileSafely.HasTile && TileID.Sets.CanBeDugByShovel[tileSafely.TileType])
                    {
                        WorldGen.KillTile(x, y, noItem: true);
                    }
                    else if (tile.HasTile && !Main.tileAxe[tile.TileType])
                    {
                        int i = x;
                        int j = y;
                        if (tile.TileFrameX % 36 != 0)
                        {
                            i--;
                        }
                        if (tile.TileFrameY % 36 != 0)
                        {
                            j--;
                        }
                        int chestIndex = Chest.FindChest(i, j);
                        int chestEmptyIndex = Chest.FindEmptyChest(i, j);
                        if (chestIndex != -1 && chestEmptyIndex == -1)
                        {
                            Item[]? items = TileHelper.ProcessChestMining(tile, i, j, false);
                            if (items != null)
                            {
                                if (Main.netMode != NetmodeID.SinglePlayer)
                                {
                                    ModPacket packet = Mod.GetPacket();
                                    packet.Write((byte)AvaritiaMod.SyncMessageType.ServerKillTile);
                                    packet.Write(i);
                                    packet.Write(j);
                                    packet.Send();
                                }
                                WorldGen.KillTile(i, j);
                                drops.AddRange(items.Where(item => item?.type != ItemID.None && item?.stack > 0));
                            }
                        }
                        else
                        {
                            object[] parameters = [x, y, tile, 0, 0, 0, 0, false];
                            typeof(WorldGen).GetMethod("KillTile_GetItemDrops", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, parameters);
                            if ((int)parameters[3] > 0 && (int)parameters[4] > 0)
                            {
                                drops.Add(TileID.Sets.Ore[tile.TileType]
                                    ? new Item((int)parameters[3], (int)parameters[4] * Main.rand.Next(4, 41))
                                    : new Item((int)parameters[3], (int)parameters[4]));
                            }
                            else if (Main.netMode != NetmodeID.SinglePlayer)
                            {
                                ModPacket packet = Mod.GetPacket();
                                packet.Write((byte)AvaritiaMod.SyncMessageType.ServerKillTile);
                                packet.Write(x);
                                packet.Write(y);
                                packet.Send();
                            }
                            WorldGen.KillTile(x, y, noItem: true);
                        }
                    }
                    WorldGen.KillWall(x, y);
                }
            }
            NetMessage.SendTileSquare(-1, baseX - 14, baseY - 14, 28, 28);
            if (drops.Count <= 0)
            {
                return base.CanUseItem(player);
            }
            Dictionary<(int type, int prefix), int> merged = [];
            foreach (Item item in drops)
            {
                if (!(item.type > ItemID.None) || item.stack <= 0)
                {
                    continue;
                }
                (int type, int prefix) key = (item.type, item.prefix);
                merged.TryAdd(key, 0);
                merged[key] += item.stack;
            }
            List<Item> consolidated = [];
            foreach (KeyValuePair<(int type, int prefix), int> kv in merged)
            {
                Item consolidatedItem = new(kv.Key.type);
                consolidatedItem.Prefix(kv.Key.prefix);
                consolidatedItem.stack = kv.Value;
                consolidated.Add(consolidatedItem);
            }
            while (consolidated.Count > 0)
            {
                Item clusterItem = new(ModContent.ItemType<MatterCluster>());
                if (clusterItem.ModItem is MatterCluster cluster)
                {
                    cluster.items = [];
                    cluster.currentTotal = 0;
                    for (int i = consolidated.Count - 1; i >= 0; i--)
                    {
                        Item leftover = cluster.TryAddItem(consolidated[i]);
                        if (leftover.IsAir || leftover.stack <= 0)
                        {
                            consolidated.RemoveAt(i);
                        }
                        else
                        {
                            consolidated[i] = leftover;
                        }
                    }
                }
                int index = Item.NewItem(clusterItem.GetSource_DropAsItem(), Main.MouseWorld, clusterItem);
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index, 1f);
            }
            drops.Clear();
            return base.CanUseItem(player);
        }
        public override void AddRecipes()
        {
            int a = ModContent.ItemType<InfinityIngot>();
            int b = ModContent.ItemType<NeutroniumIngot>();
            int c = ModContent.ItemType<CrystalMatrix>();
            new AvaritiaRecipe(Type, 9).AddIngredients(
            [
                0,a,a,a,a,a,a,a,0,
                a,a,a,a,c,a,a,a,a,
                a,a,0,0,b,0,0,a,a,
                0,0,0,0,b,0,0,0,0,
                0,0,0,0,b,0,0,0,0,
                0,0,0,0,b,0,0,0,0,
                0,0,0,0,b,0,0,0,0,
                0,0,0,0,b,0,0,0,0,
                0,0,0,0,b,0,0,0,0
            ]).Register();
        }
    }
}