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
        /// <summary>破坏一整条藤蔓：向下与向上都要处理，每次访问方块前必须做边界检查。</summary>
        private void BreakVineColumn(int x, int y, List<Point16>? pendingBreaks)
        {
            for (int vineY = y; BreakHelper.InBounds(x, vineY) && IsVineTile(Framing.GetTileSafely(x, vineY).TileType); vineY++)
            {
                BreakTileBatched(x, vineY, pendingBreaks);
            }
            for (int vineY = y - 1; BreakHelper.InBounds(x, vineY) && IsVineTile(Framing.GetTileSafely(x, vineY).TileType); vineY--)
            {
                BreakTileBatched(x, vineY, pendingBreaks);
            }
        }
        /// <summary>破坏一格：本地立即执行以保证手感，多人客户端把坐标收集起来稍后<b>一次性</b>发给服务端。</summary>
        private static void BreakTileBatched(int x, int y, List<Point16>? pendingBreaks)
        {
            if (!BreakHelper.InBounds(x, y))
            {
                return;
            }
            pendingBreaks?.Add(new Point16(x, y));
            //本地破坏（不发包，批量包由调用方统一发出）+ 屏蔽自动掉落（家具类方块会无视 noItem）
            BreakHelper.BreakTileLocal(x, y);
        }
        /// <summary>
        /// 把同步区域裁剪到世界范围内再发送（贴边挖掘会算出越界矩形）。
        /// <para>只有<b>服务端</b>才主动同步地形：客户端发这个只是把本地那份（可能过期的）地形报上去，会与服务端的破坏互相覆盖；
        /// 客户端的破坏通过批量包交给服务端同步（见库里的 <c>BreakNet.HandleServerKillTiles</c>）。</para>
        /// </summary>
        private static void SyncArea(int left, int top, int width, int height)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                return;
            }
            int minX = Math.Max(0, left);
            int minY = Math.Max(0, top);
            int maxX = Math.Min(Main.maxTilesX - 1, left + width - 1);
            int maxY = Math.Min(Main.maxTilesY - 1, top + height - 1);
            if (maxX < minX || maxY < minY)
            {
                return;
            }
            NetMessage.SendTileSquare(-1, minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
        public override bool CanUseItem(Player player)
        {
            int baseX = (int)(Main.MouseWorld.X / 16);
            int baseY = (int)(Main.MouseWorld.Y / 16);
            if (!(Main.MouseWorld.X < player.Center.X + Player.tileRangeX * 16) ||
                !(Main.MouseWorld.Y < player.Center.Y + Player.tileRangeY * 16) ||
                !(Main.MouseWorld.X > player.Center.X - Player.tileRangeX * 16) ||
                !(Main.MouseWorld.Y > player.Center.Y - Player.tileRangeY * 16) ||
                //鼠标可能在世界外：必须先做边界检查再取方块
                !BreakHelper.InBounds(baseX, baseY) ||
                !Framing.GetTileSafely(baseX, baseY).HasTile ||
                player.altFunctionUse != 2 || !Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                return base.CanUseItem(player);
            }
            List<Item> drops = [];
            //多人客户端：整片挖掘的破坏坐标先收集，循环结束后合并成少量批量包发出
            List<Point16>? pendingBreaks = Main.netMode == NetmodeID.MultiplayerClient ? [] : null;
            for (int dx = -14; dx < 14; dx++)
            {
                for (int dy = -28; dy < 28; dy++)
                {
                    int x = baseX + dx;
                    int y = baseY + dy;
                    if (!BreakHelper.InBounds(x, y))
                    {
                        continue;
                    }
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (!tile.HasTile)
                    {
                        continue;
                    }
                    if (Main.tileAxe[tile.TileType])
                    {
                        if (BreakHelper.TryGetDrop(x, y, tile, out int itemType, out int stack))
                        {
                            drops.Add(new Item(itemType, stack));
                        }
                        BreakTileBatched(x, y, pendingBreaks);
                    }
                    else if (IsGrassTile(tile.TileType))
                    {
                        //草皮两端必须一致：客户端也要本地执行，否则会出现一端有草一端没草
                        BreakTileBatched(x, y, pendingBreaks);
                    }
                    else if (IsPlantTile(tile.TileType))
                    {
                        BreakTileBatched(x, y, pendingBreaks);
                    }
                    else if (IsVineTile(tile.TileType))
                    {
                        BreakVineColumn(x, y, pendingBreaks);
                    }
                }
            }
            if (pendingBreaks is { Count: > 0 })
            {
                BreakHelper.RequestServerKillTiles(pendingBreaks, noItem: true);
            }
            SyncArea(baseX - 14, baseY - 28, 28, 56);
            if (drops.Count <= 0)
            {
                return base.CanUseItem(player);
            }
            //合并（按类型 + 前缀）后装入物质团，与 WorldBreaker / PlanetEater 共用 AvaritiaBreakHelper 的同一实现
            AvaritiaBreakHelper.SpawnAsClusters(drops, Main.MouseWorld);
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