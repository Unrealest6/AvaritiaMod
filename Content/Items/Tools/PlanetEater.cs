namespace AvaritiaMod.Content.Items.Tools
{
    public sealed class PlanetEater : AoeToolItem
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
        public override byte ModeCount => 2;
        protected override void ApplyModeStats(Item heldItem, byte mode) => heldItem.pick = mode == 0 ? 1 : 0;
        /// <summary>
        /// 形态 1 的范围铲除。
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
                //铲子只处理软物块；统一结算：多格物块只算一次，掉落（含模组方块）最终合并成物质团
                if (tile.HasTile && TileID.Sets.CanBeDugByShovel[tile.TileType])
                {
                    BreakHelper.ProcessAoeTile(tile, x, y, swing);
                }
            });
            //地形同步交给 FinishAoeSwing：服务端按整片区域同步（客户端自己发只会把本地那份报上去）
            BreakHelper.FinishAoeSwing(swing, Main.MouseWorld);
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