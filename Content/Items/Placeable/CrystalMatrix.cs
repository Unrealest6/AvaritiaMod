using AvaritiaMod.Content.Items;

namespace AvaritiaMod.Content.Items.Placeable
{
    public class CrystalMatrix : FrameItem
    {
        internal static FrameTexture? AnimatedTexture { get; private set; }
        protected override int FrameCount => 6;
        protected override int FrameDuration => 3;
        protected override FrameDef[] FrameTimeline =>
        [
            0, 0, 0,
            1, 1, 1,
            2, 2, 2,
            3, 3,
            4, 4,
            5,
            4, 4, 3, 3,
            2, 2, 2, 1, 1, 1
        ];
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            AnimatedTexture = FrameTexture;
        }
        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.createTile = ModContent.TileType<CrystalMatrixTile>();
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
            for (int i = 0; i < 9; i++)
            {
                recipe.AddIngredient(ModContent.ItemType<CrystalMatrixIngot>());
            }
            recipe.Register();
        }
    }
}