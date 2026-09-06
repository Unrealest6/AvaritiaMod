namespace AvaritiaMod.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Legs)]
    public sealed class InfinityBoots : ModItem
    {
        public override void SetDefaults()
        {
            Item.defense = 18;
            Item.rare = ModContent.RarityType<LightRedRarity>();
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips.Where(line => line.Mod == "Terraria"))
            {
                if (line.Name == "Tooltip0")
                {
                    line.Text = "+" + Lang.GetTooltip(Type).GetLine(0).ApplyGradient("SANIC") + "% Speed";
                }
            }
        }
        public override void UpdateEquip(Player player)
        {
            player.jumpSpeedBoost += 8f;
            player.moveSpeed *= 2f;
        }
        public override void AddRecipes()
        {
            int a = ModContent.ItemType<InfinityIngot>();
            int b = ModContent.ItemType<NeutroniumIngot>();
            int c = ModContent.ItemType<CrystalMatrix>();
            int d = ModContent.ItemType<InfinityCatalyst>();
            new AvaritiaRecipe(Type, 9).AddIngredients(
            [
                b,b,b,b,b,b,b,b,b,
                b,a,a,a,d,a,a,a,b,
                b,a,b,b,d,b,b,a,b,
                b,a,b,0,0,0,b,a,b,
                b,c,b,0,0,0,b,c,b,
                b,a,b,0,0,0,b,a,b,
                b,a,b,0,0,0,b,a,b,
                b,a,b,0,0,0,b,a,b,
                b,b,b,0,0,0,b,b,b
            ]).Register();
        }
    }
}