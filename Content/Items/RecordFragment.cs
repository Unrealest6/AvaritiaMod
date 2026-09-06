namespace AvaritiaMod.Content.Items
{
    public sealed class RecordFragment : ModItem
    {
        public override void SetDefaults()
        {
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ModContent.RarityType<LightRedRarity>();
        }
        public override void AddRecipes() => CreateRecipe(8).AddIngredient(ItemID.MusicBox).AddTile(TileID.WorkBenches).Register();
    }
}