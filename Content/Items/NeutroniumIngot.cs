namespace AvaritiaMod.Content.Items
{
    public sealed class NeutroniumIngot : ModItem
    {
        public override void SetStaticDefaults() => Main.RegisterItemAnimation(Type, new DrawAnimationVertical(9, 3, true));
        public override void SetDefaults() => Item.maxStack = Item.CommonMaxStack;
        public override void AddRecipes()
        {
            AvaritiaRecipe recipe = new(Type, 3);
            for (int i = 0; i < 9; i++)
            {
                recipe.AddIngredient(ModContent.ItemType<NeutroniumNugget>());
            }
            recipe.Register();
            CreateRecipe(9).AddIngredient(ModContent.ItemType<NeutroniumBlock>()).Register();
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