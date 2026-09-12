namespace AvaritiaMod.Content.Items.Tools
{
    public sealed class NatureRuin : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.damage = 21;
            Item.DamageType = DamageClass.Melee;
            Item.axe = int.MaxValue;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6;
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
                if (line.Name == "AxePower")
                {
                    line.Hide();
                }
            }
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = TextureAssets.Item[Type].Value;
            spriteBatch.Draw(texture, position, null, drawColor, MathHelper.PiOver2, texture.Size() / 2f, scale, SpriteEffects.FlipHorizontally, 0f);
            return false;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            rotation += MathHelper.PiOver2;
            Texture2D texture = TextureAssets.Item[Type].Value;
            Vector2 vector = new(texture.Width / 2f, texture.Height / 2f);
            Vector2 vector3 = Item.position - Main.screenPosition + vector;
            spriteBatch.Draw(texture, vector3, null, lightColor, rotation, vector, scale, SpriteEffects.FlipHorizontally, 0f);
            return false;
        }
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player)
        {
            if (!(Main.MouseWorld.X < player.Center.X + Player.tileRangeX * 16) ||
                !(Main.MouseWorld.Y < player.Center.Y + Player.tileRangeY * 16) ||
                !(Main.MouseWorld.X > player.Center.X - Player.tileRangeX * 16) ||
                !(Main.MouseWorld.Y > player.Center.Y - Player.tileRangeY * 16) ||
                !Main.tile[(int)Main.MouseWorld.X / 16, (int)Main.MouseWorld.Y / 16].HasTile || player.altFunctionUse != 2 || !Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                return base.CanUseItem(player);
            }
            List<Item> drops = [];
            int baseX = (int)(Main.MouseWorld.X / 16);
            int baseY = (int)(Main.MouseWorld.Y / 16);
            Type worldGenType = typeof(WorldGen);
            MethodInfo? killTileGetItemDrops = worldGenType.GetMethod("KillTile_GetItemDrops", BindingFlags.NonPublic | BindingFlags.Static);
            object[] parameters = new object[8];
            for (int dx = -14; dx < 14; dx++)
            {
                for (int dy = -28; dy < 28; dy++)
                {
                    int x = baseX + dx;
                    int y = baseY + dy;
                    if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
                    {
                        continue;
                    }
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile)
                    {
                        continue;
                    }
                    if (Main.tileAxe[tile.TileType])
                    {
                        parameters[0] = x;
                        parameters[1] = y;
                        parameters[2] = tile;
                        parameters[3] = 0;
                        parameters[4] = 0;
                        parameters[5] = 0;
                        parameters[6] = 0;
                        parameters[7] = false;
                        killTileGetItemDrops?.Invoke(null, parameters);
                        if ((int)parameters[3] > 0 && (int)parameters[4] > 0)
                        {
                            drops.Add(new Item((int)parameters[3], (int)parameters[4]));
                        }
                        if (Main.netMode != NetmodeID.SinglePlayer)
                        {
                            ModPacket packet = Mod.GetPacket();
                            packet.Write((byte)AvaritiaMod.SyncMessageType.ServerKillTile);
                            packet.Write(x);
                            packet.Write(y);
                            packet.Send();
                        }
                        WorldGen.KillTile(x, y, noItem: true);
                    }
                    else if (IsGrassTile(tile.TileType))
                    {
                        if (Main.netMode != NetmodeID.SinglePlayer)
                        {
                            ModPacket packet = Mod.GetPacket();
                            packet.Write((byte)AvaritiaMod.SyncMessageType.ServerKillTile);
                            packet.Write(x);
                            packet.Write(y);
                            packet.Send();
                        }
                        WorldGen.KillTile(x, y, true, noItem: true);
                    }
                    else if (IsPlantTile(tile.TileType))
                    {
                        if (Main.netMode != NetmodeID.SinglePlayer)
                        {
                            ModPacket packet = Mod.GetPacket();
                            packet.Write((byte)AvaritiaMod.SyncMessageType.ServerKillTile);
                            packet.Write(x);
                            packet.Write(y);
                            packet.Send();
                        }
                        WorldGen.KillTile(x, y, noItem: true, effectOnly: false);
                    }
                    else if (IsVineTile(tile.TileType))
                    {
                        int vineY = y;
                        while (vineY < Main.maxTilesY && IsVineTile(Main.tile[x, vineY].TileType))
                        {
                            if (Main.netMode != NetmodeID.SinglePlayer)
                            {
                                ModPacket packet = Mod.GetPacket();
                                packet.Write((byte)AvaritiaMod.SyncMessageType.ServerKillTile);
                                packet.Write(x);
                                packet.Write(y);
                                packet.Send();
                            }
                            WorldGen.KillTile(x, vineY, noItem: true, effectOnly: false);
                            vineY++;
                        }
                        vineY = y;
                        do
                        {
                            vineY--;
                        }
                        while (vineY < Main.maxTilesY && IsVineTile(Main.tile[x, vineY].TileType));
                        {
                            if (Main.netMode != NetmodeID.SinglePlayer)
                            {
                                ModPacket packet = Mod.GetPacket();
                                packet.Write((byte)AvaritiaMod.SyncMessageType.ServerKillTile);
                                packet.Write(x);
                                packet.Write(y);
                                packet.Send();
                            }
                            WorldGen.KillTile(x, vineY, noItem: true, effectOnly: false);
                        }
                    }
                }
            }
            NetMessage.SendTileSquare(-1, baseX - 14, baseY - 28, 28, 56);
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
                MatterCluster? cluster = clusterItem.ModItem as MatterCluster;
                if (cluster != null)
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
            new AvaritiaRecipe(Type, 9).AddIngredients(
            [
                0,0,0,a,0,0,0,0,0,
                0,0,a,a,a,a,a,0,0,
                0,0,0,a,a,a,a,0,0,
                0,0,0,0,0,a,b,0,0,
                0,0,0,0,0,0,b,0,0,
                0,0,0,0,0,0,b,0,0,
                0,0,0,0,0,0,b,0,0,
                0,0,0,0,0,0,b,0,0,
                0,0,0,0,0,0,b,0,0
            ]).Register();
        }
        private bool IsGrassTile(int type) => type == TileID.Grass || type == TileID.CorruptGrass || type == TileID.CrimsonGrass
                                              || type == TileID.HallowedGrass || type == TileID.JungleGrass || type == TileID.MushroomGrass;
        private bool IsPlantTile(int type) => type == TileID.Plants || type == TileID.Plants2 || type == TileID.CorruptPlants
                                              || type == TileID.CrimsonPlants || type == TileID.HallowedPlants || type == TileID.JunglePlants;
        private bool IsVineTile(int type) => type == TileID.Vines || type == TileID.CorruptVines || type == TileID.CrimsonVines
                                             || type == TileID.HallowedVines || type == TileID.JungleVines;
    }
}