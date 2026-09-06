using AvaritiaMod.Content.Buffs;

namespace AvaritiaMod.Content.Items.Consumables
{
    public sealed class UltimateStew : ModItem
    {
        private static readonly List<int> ItemTypes = [ModContent.ItemType<PileOfNeutrons>(), 276, 276, 5, 5, 183, 183, 4009, 4009, 4023, 4023, 4282, 4282, 4283, 4283, 4284, 4284,
            4285, 4285, 4286, 4286, 4287, 4287, 4288, 4288, 4289, 4289, 4290, 4290, 4291, 4291, 4292, 4292, 4293, 4293, 4294, 4294, 4295, 4295, 4296, 4296, 4297, 4297, 5277, 5277, 5278, 5278];
        public static bool AddItemToRecipe(int type)
        {
            if (ItemTypes.Count >= 81)
            {
                return false;
            }
            ItemTypes.Add(type);
            return true;
        }
        public override void SetStaticDefaults() => Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(6, 28));
        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.maxStack = Item.CommonMaxStack;
            Item.UseSound = SoundID.Item3;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useTurn = true;
            Item.useTime = 17;
            Item.useAnimation = 17;
            Item.maxStack = Item.CommonMaxStack;
            Item.buffType = ModContent.BuffType<CelestiallyCharged1>();
            Item.buffTime = 151200;
        }
        public override void AddRecipes()
        {
            AvaritiaRecipe recipe = new(Type, 9, 2, false);
            foreach (int type in ItemTypes)
            {
                recipe.AddIngredient(type);
            }
            recipe.Register();
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D value = TextureAssets.Item[Type].Value;
            Rectangle frame = Main.itemAnimations[Type] == null ? value.Frame() : Main.itemAnimations[Type].GetFrame(value);
            Vector2 vector = frame.Size() / 2f;
            Vector2 vector2 = new(Item.width / 2f - vector.X, Item.height - frame.Height);
            Vector2 vector3 = Item.position - Main.screenPosition + vector + vector2;
            spriteBatch.Draw(value, vector3, frame, lightColor, rotation, vector, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}