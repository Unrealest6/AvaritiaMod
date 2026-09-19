namespace AvaritiaMod.Content.Items.Placeable
{
    public sealed class DoubleCompressedCraftingTable : CraftingTableItem
    {
        protected override BoundedSize GridSize => 6;
        protected override int TileType => ModContent.TileType<DoubleCompressedCraftingTableTile>();
        protected override string TagMaxStack => "DoubleCompressedCraftingTableMaxStack";
        protected override string TagItems => "DoubleCompressedCraftingTableItems";
        public override void AddRecipes()
        {
            AvaritiaRecipe recipe = new(Type, 3);
            for (int i = 0; i < 9; i++)
            {
                recipe.AddIngredient(ModContent.ItemType<CompressedCraftingTable>());
            }
            recipe.Register();
        }
    }
}