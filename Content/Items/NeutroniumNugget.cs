using AvaritiaMod;

namespace AvaritiaMod.Content.Items
{
    public sealed class NeutroniumNugget : ModItem
    {
        public override void SetDefaults()
        {
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Yellow;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearWrap, null, null, null, Main.UIScaleMatrix);
            if (AvaritiaFrameSystem.HaloNoise != null)
            {
                spriteBatch.Draw(AvaritiaFrameSystem.HaloNoise.GetCurrentFrame(), position - new Vector2(frame.Width * scale / 2.5f, frame.Height * scale / 2.5f), null, Color.White * 0.7f
                    , 0, origin, scale, SpriteEffects.None, 0);
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
            return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }
        public override void AddRecipes()
        {
            AvaritiaRecipe recipe = new(Type, 3);
            for (int i = 0; i < 9; i++)
            {
                recipe.AddIngredient(ModContent.ItemType<PileOfNeutrons>());
            }
            recipe.Register();
            CreateRecipe(9).AddIngredient(ModContent.ItemType<NeutroniumIngot>()).Register();
        }
    }
}