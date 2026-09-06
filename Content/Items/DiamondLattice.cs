namespace AvaritiaMod.Content.Items
{
    public sealed class DiamondLattice : ModItem
    {
        public override void SetDefaults()
        {
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Yellow;
        }
        public override void AddRecipes()
        {
            AvaritiaRecipe recipe = new(Type, 3);
            recipe.AddIngredient(0, 0, ItemID.Diamond);
            recipe.AddIngredient(2, 0, ItemID.Diamond);
            recipe.AddIngredient(1, 1, ItemID.Diamond);
            recipe.AddIngredient(0, 2, ItemID.Diamond);
            recipe.AddIngredient(2, 2, ItemID.Diamond);
            recipe.Register();
        }
    }
}