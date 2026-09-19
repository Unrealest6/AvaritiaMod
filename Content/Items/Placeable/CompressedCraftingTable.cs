namespace AvaritiaMod.Content.Items.Placeable
{
    public sealed class CompressedCraftingTable : CraftingTableItem
    {
        protected override BoundedSize GridSize => 3;
        protected override int TileType => ModContent.TileType<CompressedCraftingTableTile>();
        protected override string TagMaxStack => "CompressedCraftingTableMaxStack";
        protected override string TagItems => "CompressedCraftingTableItems";
        public override void AddRecipes() => CreateRecipe().AddTile(TileID.WorkBenches).AddIngredient(ItemID.WorkBench, 9).Register();
    }
}