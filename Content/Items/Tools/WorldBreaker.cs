namespace AvaritiaMod.Content.Items.Tools
{
    public sealed class WorldBreaker : AoeToolItem
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
        public override byte ModeCount => 2;
        protected override void ApplyModeStats(Item heldItem, byte mode)
        {
            heldItem.knockBack = mode == 0 ? 2 : 32;
            heldItem.pick = mode == 0 ? int.MaxValue : 0;
        }
        /// <summary>
        /// 形态 1 的范围破坏。
        /// <para>必须放在 <c>UseItem</c> 而不是 <c>CanUseItem</c>：后者一帧可能被调用多次，写在那里会让一次挥动产出好几倍的掉落。</para>
        /// </summary>
        public override bool? UseItem(Player player)
        {
            if (!TryBeginAoeSwing(player, out BreakHelper.AoeSwing swing))
            {
                return base.UseItem(player);
            }
            ForEachAoeTile((tile, x, y) =>
            {
                if (!tile.HasTile)
                {
                    //空地上也可能有墙，抹墙照常处理
                    swing.KillWall(x, y);
                    return;
                }
                if (TileID.Sets.CanBeDugByShovel[tile.TileType])
                {
                    //软物块（泥土、沙等）只抹掉、不产出掉落，坐标先收集、结束时批量交服务端；必须带上收集窗口，否则被它支撑的罐子级联碎掉后奖励会被屏蔽掉
                    swing.ServerBreakOrigins?.Add(new Point16(x, y));
                    BreakHelper.BreakTileLocal(x, y, noItem: true, sink: swing.Drops);
                }
                else if (!Main.tileAxe[tile.TileType])
                {
                    //统一结算：多格物块只在左上角结算一次；箱子内容物与本体、模组机器 / 家具里存的物品一并计入
                    BreakHelper.ProcessAoeTile(tile, x, y, swing);
                }
                swing.KillWall(x, y);
            });
            //地形同步交给 FinishAoeSwing：服务端按整片区域同步（客户端自己发只会把本地那份报上去）
            BreakHelper.FinishAoeSwing(swing, Main.MouseWorld);
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
