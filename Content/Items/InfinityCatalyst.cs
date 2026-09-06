namespace AvaritiaMod.Content.Items
{
    public sealed class InfinityCatalyst : FrameItem
    {
        protected override int FrameCount => 9;
        protected override int FrameDuration => 3;
        protected override FrameDef[] FrameTimeline =>
        [
            0, 0, 0,
            1, 1, 1,
            2, 2, 2,
            3, 3,
            4, 4,
            5,
            6,
            7,
            8,
            7, 6, 5,
            4, 4, 3, 3,
            2, 2, 2, 1, 1, 1
        ];
        private static readonly List<int> ItemTypes = [ModContent.ItemType<DiamondLattice>(), ModContent.ItemType<CrystalMatrixIngot>(), ModContent.ItemType<PileOfNeutrons>(), ModContent.ItemType<NeutroniumNugget>(),
            ModContent.ItemType<NeutroniumIngot>(), ModContent.ItemType<UltimateStew>(), ModContent.ItemType<CosmicMeatballs>(), ModContent.ItemType<EndestPearl>(), ModContent.ItemType<RecordFragment>()];
        public override void SetDefaults()
        {
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ModContent.RarityType<PinkishPurpleRarity>();
        }
        public override void AddRecipes()
        {
            AvaritiaRecipe recipe = new(Type, 9, isOrdered: false);
            foreach (int type in ItemTypes)
            {
                recipe.AddIngredient(type);
            }
            foreach (int type in Singularity.Singularities.Values.Where(item => item.AddInfinityCatalystRecipe).Select(item => item.Type))
            {
                recipe.AddIngredient(type);
            }
            recipe.Register();
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