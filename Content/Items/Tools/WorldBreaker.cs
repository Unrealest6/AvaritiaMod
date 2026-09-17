namespace AvaritiaMod.Content.Items.Tools
{
    public sealed class WorldBreaker : AvaritiaModeItem
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
            Item.rare = ModContent.RarityType<LightRedRarity>();
            Item.UseSound = SoundID.Item1;
            Item.useTurn = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips.Where(line => line.Mod == "Terraria"))
            {
                if (line.Name == "PickPower")
                {
                    line.Hide();
                }
            }
        }
        /// <summary>形态 0：无限镐力；形态 1：范围破坏。</summary>
        protected override byte ModeCount => 2;
        protected override void ApplyModeStats(Item heldItem, byte mode)
        {
            heldItem.knockBack = mode == 0 ? 2 : 32;
            heldItem.pick = mode == 0 ? int.MaxValue : 0;
        }
        /// <summary>
        /// 形态 1 的范围破坏。
        /// <para>必须放在 <c>UseItem</c> 而不是 <c>CanUseItem</c>：后者一帧可能被调用多次，
        /// 原实现把整片挖掘与物质团生成都写在里面，一次挥动会产出好几倍的掉落。</para>
        /// </summary>
        public override bool? UseItem(Player player)
        {
            int baseX = (int)(Main.MouseWorld.X / 16);
            int baseY = (int)(Main.MouseWorld.Y / 16);
            if (!player.IsInTileInteractionRange(baseX, baseY, TileReachCheckSettings.Simple)
                || !AvaritiaBreakHelper.InBounds(baseX, baseY)
                || !Framing.GetTileSafely(baseX, baseY).HasTile
                || GetHeldMode(player) != 1)
            {
                return base.UseItem(player);
            }
            List<Item> drops = [];
            //一次挥动内同一个箱子只能处理一次：FindChestByGuessing 会命中箱子的多个相邻格子
            HashSet<int> processedChests = [];
            for (int dx = -14; dx < 14; dx++)
            {
                for (int dy = -14; dy < 14; dy++)
                {
                    int x = baseX + dx;
                    int y = baseY + dy;
                    if (!AvaritiaBreakHelper.InBounds(x, y))
                    {
                        continue;
                    }
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (tile.HasTile && TileID.Sets.CanBeDugByShovel[tile.TileType])
                    {
                        AvaritiaBreakHelper.BreakTile(x, y);
                    }
                    else if (tile.HasTile && !Main.tileAxe[tile.TileType])
                    {
                        //箱子：按左上角定位并先真正清空箱内物品，否则 Chest.DestroyChest 会失败，
                        //WorldGen.KillTile 就会认为“该格应当存活”，箱子永远打不掉且每次挥动重复取内容。
                        //内容物与箱子本体都会并入 drops（最终合并成物质团）。
                        List<Item>? chestLoot = AvaritiaBreakHelper.TakeChestLoot(tile, x, y, processedChests);
                        if (chestLoot is not null)
                        {
                            drops.AddRange(chestLoot);
                        }
                        else
                        {
                            if (AvaritiaBreakHelper.TryGetDrop(x, y, tile, out int itemType, out int stack))
                            {
                                drops.Add(TileID.Sets.Ore[tile.TileType]
                                    ? new Item(itemType, stack * Main.rand.Next(4, 41))
                                    : new Item(itemType, stack));
                            }
                            AvaritiaBreakHelper.BreakTile(x, y);
                        }
                    }
                    WorldGen.KillWall(x, y);
                }
            }
            NetMessage.SendTileSquare(-1, baseX - 14, baseY - 14, 28, 28);
            if (drops.Count <= 0)
            {
                return base.UseItem(player);
            }
            //合并后装进物质团（单个上限 4096，装不下的部分才散落）——内容按物品实例保存，多个物质团互不覆盖
            AvaritiaBreakHelper.SpawnAsClusters(drops, Main.MouseWorld);
            drops.Clear();
            return base.UseItem(player);
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