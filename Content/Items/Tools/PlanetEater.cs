namespace AvaritiaMod.Content.Items.Tools
{
    public sealed class PlanetEater : AvaritiaModeItem
    {
        public override Dictionary<byte, FrameTexture?> FrameTextures => new()
        {
            [1] = FrameTextureSystem.Register("PlanetEater2", "AvaritiaMod/Content/Items/Tools/PlanetEater2")
        };
        public override string Texture => "AvaritiaMod/Content/Items/Tools/PlanetEater1";
        protected override int FrameCount => 9;
        protected override FrameDef[] FrameTimeline => [
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
        protected override int FrameDuration => 3;
        public override void SetDefaults()
        {
            Item.damage = 9;
            Item.DamageType = DamageClass.Melee;
            Item.pick = 1;
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
            Item.width = 32;
            Item.height = 32;
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
        /// <summary>形态 0：只能挖 1 点镐力的方块；形态 1：范围铲除。</summary>
        protected override byte ModeCount => 2;
        protected override void ApplyModeStats(Item heldItem, byte mode) => heldItem.pick = mode == 0 ? 1 : 0;
        /// <summary>
        /// 形态 1 的范围铲除。
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
                    if (!tile.HasTile || !TileID.Sets.CanBeDugByShovel[tile.TileType])
                    {
                        continue;
                    }
                    if (AvaritiaBreakHelper.TryGetDrop(x, y, tile, out int itemType, out int stack))
                    {
                        drops.Add(new Item(itemType, stack));
                    }
                    AvaritiaBreakHelper.BreakTile(x, y);
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
            int c = ModContent.ItemType<InfinityBlock>();
            new AvaritiaRecipe(Type, 9).AddIngredients(
            [
                0,0,0,0,0,0,a,a,a,
                0,0,0,0,0,a,a,c,a,
                0,0,0,0,0,0,a,a,a,
                0,0,0,0,0,b,0,a,0,
                0,0,0,0,b,0,0,0,0,
                0,0,0,b,0,0,0,0,0,
                0,0,b,0,0,0,0,0,0,
                0,b,0,0,0,0,0,0,0,
                b,0,0,0,0,0,0,0,0
            ]).Register();
        }
    }
}