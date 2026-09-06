namespace AvaritiaMod.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Body)]
    public sealed class InfinityChestPlate : FrameItem
    {
        protected override int FrameCount => 9;
        protected override int FrameDuration => 3;
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
        public override void SetDefaults()
        {
            Item.defense = 14;
            Item.rare = ModContent.RarityType<LightRedRarity>();
        }
        public override void UpdateEquip(Player player)
        {
            player.CanFly = true;
            foreach (int type in player.buffType)
            {
                if (Main.debuff[type])
                {
                    player.ClearBuff(type);
                }
            }
        }
        public override void AddRecipes()
        {
            int a = ModContent.ItemType<InfinityIngot>();
            int b = ModContent.ItemType<NeutroniumIngot>();
            int c = ModContent.ItemType<CrystalMatrix>();
            new AvaritiaRecipe(Type, 9).AddIngredients(
            [
                0,b,b,0,0,0,b,b,0,
                b,b,b,0,0,0,b,b,b,
                b,b,b,0,0,0,b,b,b,
                0,b,a,a,a,a,a,b,0,
                0,b,a,a,c,a,a,b,0,
                0,b,a,a,a,a,a,b,0,
                0,b,a,a,a,a,a,b,0,
                0,b,a,a,a,a,a,b,0,
                0,0,b,b,b,b,b,0,0
            ]).Register();
        }
    }
}