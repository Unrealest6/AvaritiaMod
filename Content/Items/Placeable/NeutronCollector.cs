namespace AvaritiaMod.Content.Items.Placeable
{
    public sealed class NeutronCollector : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 34;
            Item.maxStack = Item.CommonMaxStack;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<NeutronCollectorTile>();
        }
        public override void AddRecipes()
        {
            const int a = ItemID.PearlstoneBrick;
            (int, int) b = (ItemID.Ruby, 9);
            (int, int) c = (ItemID.IronBar, 9);
            int d = ModContent.ItemType<CrystalMatrixIngot>();
            new AvaritiaRecipe(Type, 9).AddIngredients(
            [
                c,c,a,a,a,a,a,c,c,
                c,0,a,a,a,a,a,0,c,
                c,0,0,b,b,b,0,0,c,
                d,0,b,b,b,b,b,0,d,
                c,0,b,b,d,b,b,0,c,
                d,0,b,b,b,b,b,0,d,
                c,0,0,b,b,b,0,0,c,
                c,0,0,0,0,0,0,0,c,
                c,c,c,d,c,d,c,c,c
            ]).Register();
        }
    }
}