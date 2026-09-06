using AvaritiaMod;
using AvaritiaMod.Content.Items;

namespace AvaritiaMod.Content.Items.Placeable
{
    public sealed class NeutroniumBlock : ModItem
    {
        public override void SetDefaults()
        {
            Item.createTile = ModContent.TileType<NeutroniumBlockTile>();
            Item.consumable = true;
            Item.maxStack = Item.CommonMaxStack;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 15;
            Item.UseSound = null;
            Item.useAnimation = 15;
            Item.autoReuse = true;
        }
        public override void AddRecipes()
        {
            AvaritiaRecipe recipe = new(Type, 3);
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    recipe.AddIngredient(x, y, ModContent.ItemType<NeutroniumIngot>());
                }
            }
            recipe.Register();
        }
    }
}