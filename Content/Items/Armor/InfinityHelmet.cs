namespace AvaritiaMod.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Head)]
    public sealed class InfinityHelmet : ModItem
    {
        public override void SetDefaults()
        {
            Item.defense = 8;
            Item.rare = ModContent.RarityType<LightRedRarity>();
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<InfinityHelmet>() && body.type == ModContent.ItemType<InfinityChestPlate>() && legs.type == ModContent.ItemType<InfinityBoots>();
        }
        public override void UpdateEquip(Player player)
        {
            player.breath = player.breathMax;
            player.breathCD = 0;
        }
        public override void UpdateArmorSet(Player player)
        {
            if (player.statLife < 25)
            {
                player.statLife = player.statLifeMax2;
            }
            player.statDefense += 960;
            player.lifeRegen += 50;
        }
        public override void AddRecipes()
        {
            int a = ModContent.ItemType<InfinityIngot>();
            int b = ModContent.ItemType<NeutroniumIngot>();
            int c = ModContent.ItemType<InfinityCatalyst>();
            new AvaritiaRecipe(Type, 9).AddIngredients(
            [
                0,0,b,b,b,b,b,0,0,
                0,b,a,a,a,a,a,b,0,
                0,b,0,c,a,c,0,b,0,
                0,b,a,a,a,a,a,b,0,
                0,b,a,a,a,a,a,b,0,
                0,b,a,0,a,0,a,b,0,
                0,0,0,0,0,0,0,0,0,
                0,0,0,0,0,0,0,0,0,
                0,0,0,0,0,0,0,0,0
            ]).Register();
        }
    }
}