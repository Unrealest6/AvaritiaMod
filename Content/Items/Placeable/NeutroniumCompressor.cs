using AvaritiaMod.Content.Items;

namespace AvaritiaMod.Content.Items.Placeable
{
    public sealed class NeutroniumCompressor : ModItem
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
            Item.createTile = ModContent.TileType<NeutroniumCompressorTile>();
        }
        public override void AddRecipes()
        {
            const int a = ItemID.Chest;
            (int, int) b = (ItemID.Ruby, 9);
            (int, int) c = (ItemID.IronBar, 9);
            int d = ModContent.ItemType<CrystalMatrixIngot>();
            int e = ModContent.ItemType<NeutroniumBlock>();
            int f = ModContent.ItemType<NeutroniumIngot>();
            new AvaritiaRecipe(Type, 9).AddIngredients(
            [
                c,c,c,a,a,a,c,c,c,
                d,0,f,0,0,0,f,0,d,
                c,0,f,0,0,0,f,0,c,
                d,0,f,0,0,0,f,0,d,
                b,f,f,0,e,0,f,f,b,
                d,0,f,0,0,0,f,0,d,
                c,0,f,0,0,0,f,0,c,
                d,0,f,0,0,0,f,0,d,
                c,c,c,d,c,d,c,c,c
            ]).Register();
        }
    }
}