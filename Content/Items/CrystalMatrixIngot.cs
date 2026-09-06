namespace AvaritiaMod.Content.Items
{
    public sealed class CrystalMatrixIngot : ModItem
    {
        public override void SetDefaults()
        {
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ModContent.RarityType<AquaRarity>();
        }
        public override void AddRecipes()
        {
            AvaritiaRecipe recipe = new(Type, 3);
            recipe.AddIngredient(0, 1, ModContent.ItemType<DiamondLattice>());
            recipe.AddIngredient(1, 1, ModContent.ItemType<NetherStar>());
            recipe.AddIngredient(2, 1, ModContent.ItemType<DiamondLattice>());
            recipe.AddIngredient(0, 2, ModContent.ItemType<DiamondLattice>());
            recipe.AddIngredient(1, 2, ModContent.ItemType<NetherStar>());
            recipe.AddIngredient(2, 2, ModContent.ItemType<DiamondLattice>());
            recipe.Register();
        }
    }
}