using AvaritiaMod.Content.Items;

namespace AvaritiaMod.Content.Items.Placeable
{
    public sealed class ExtremeCraftingTable : CraftingTableItem
    {
        protected override byte GridSize => 9;
        protected override int TileType => ModContent.TileType<ExtremeCraftingTableTile>();
        protected override string TagMaxStack => "ExtremeCraftingTableMaxStack";
        protected override string TagItems => "ExtremeCraftingTableItems";
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips.Where(line => line.Mod == "Terraria").Where(line => line.Name == "ItemName"))
            {
                line.Text = "[c/E05050:" + line.Text + "]";
            }
        }
        public override void AddRecipes()
        {
            AvaritiaRecipe recipe = new(Type, 3);
            for (int i = 0; i < 9; i++)
            {
                recipe.AddIngredient(ModContent.ItemType<CrystalMatrixIngot>());
            }
            recipe.ClearIngredient(1, 1).AddIngredient(ModContent.ItemType<DoubleCompressedCraftingTable>()).Register();
        }
    }
}