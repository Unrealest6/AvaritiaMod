namespace AvaritiaMod.Content.Items
{
    public sealed class InfinityIngot : FrameItem
    {
        protected override int FrameCount => 9;
        protected override FrameDef[] FrameTimeline =>
        [
            new(0,9),
            new(1,9),
            new(2,9),
            new(3,6),
            new(4,6),
            new(5,90),
            new(6,90),
            new(7,90),
            new(8,90),
            new(7,90),
            new(6,90),
            new(5,90),
            new(4,6),
            new(3,6),
            new(2,9),
            new(1,9),
        ];
        public override void SetDefaults()
        {
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ModContent.RarityType<LightRedRarity>();
        }
        public override void AddRecipes()
        {
            int a = ModContent.ItemType<CrystalMatrixIngot>();
            int b = ModContent.ItemType<NeutroniumIngot>();
            int c = ModContent.ItemType<InfinityCatalyst>();
            new AvaritiaRecipe(Type, 9).AddIngredients(
            [
                b,b,b,b,b,b,b,b,b,
                b,a,c,c,a,c,c,a,b,
                b,c,a,a,c,a,a,c,b,
                b,a,c,c,a,c,c,a,b,
                b,b,b,b,b,b,b,b,b
            ]).Register();
            CreateRecipe(9).AddIngredient(ModContent.ItemType<InfinityBlock>()).Register();
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/HaloBig").Value;
            Texture2D? value = FrameTexture?.GetCurrentFrame();
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearWrap, null, null, null, Main.UIScaleMatrix);
            spriteBatch.Draw(texture, position, null, Color.Black, 0, texture.Size() / 2f, scale, SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
            spriteBatch.Draw(value, position, null, drawColor, 0, origin, Main.rand.NextFloat(scale * 0.95f, scale * 1.15f), SpriteEffects.None, 0);
            return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }
    }
}