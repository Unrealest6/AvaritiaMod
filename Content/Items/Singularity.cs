namespace AvaritiaMod.Content.Items
{
    public abstract class Singularity : ModItem
    {
        public static FrameTexture? Singularity1 { get; private set; }
        public static FrameTexture? Singularity2 { get; private set; }
        internal static Dictionary<int, Singularity> Singularities { get; set; } = [];
        public abstract int RequiredQuantity { get; }
        protected virtual Color OverlayColor => Color.White;
        protected virtual Color UnderlayColor => OverlayColor - 64;
        protected abstract int RequiredItemType { get; }
        protected internal virtual bool AddInfinityCatalystRecipe => true;
        public sealed override string Texture => "AvaritiaMod/Content/Items/Singularity2";
        public sealed override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 6));
            Singularities.Add(RequiredItemType, (Singularity)new Item(Type).ModItem);
            Singularity1 = FrameTextureSystem.Register("Singularity1", "AvaritiaMod/Content/Items/Singularity1", 3, 6);
            Singularity2 = FrameTextureSystem.Register("Singularity2", "AvaritiaMod/Content/Items/Singularity2", 6, 3);
        }
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Yellow;
        }
        public virtual bool PreDrawInInventory(ref SpriteBatch spriteBatch, ref Vector2 position, ref Rectangle frame, ref Color drawColor, ref Color itemColor, ref Vector2 origin, ref float scale) => true;
        public virtual bool PreDrawInWorld(ref SpriteBatch spriteBatch, ref Color lightColor, ref Color alphaColor, ref float rotation, ref float scale, int whoAmI) => true;
        public sealed override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (!PreDrawInInventory(ref spriteBatch, ref position, ref frame, ref drawColor, ref itemColor, ref origin, ref scale))
            {
                return false;
            }
            Texture2D texture = ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/HaloSmall").Value;
            Texture2D? value = Singularity1?.GetCurrentFrame();
            Texture2D? value1 = Singularity2?.GetCurrentFrame();
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearWrap, null, null, null, Main.UIScaleMatrix);
            spriteBatch.Draw(texture, position, null, Color.Black, 0, texture.Size() / 2f, scale, SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, Main.UIScaleMatrix);
            spriteBatch.Draw(value, position, null, UnderlayColor, 0, origin, scale, SpriteEffects.None, 0);
            spriteBatch.Draw(value1, position, null, OverlayColor, 0, origin, scale, SpriteEffects.None, 0);
            return false;
        }
        public sealed override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (!PreDrawInWorld(ref spriteBatch, ref lightColor, ref alphaColor, ref rotation, ref scale, whoAmI))
            {
                return false;
            }
            Texture2D? value = Singularity1?.GetCurrentFrame();
            Texture2D? value1 = Singularity2?.GetCurrentFrame();
            Vector2 vector = new(value?.Width / 2f ?? 0, value?.Height / 2f ?? 0);
            Vector2 vector2 = new(Item.width / 2f - vector.X, Item.height - value?.Height ?? 0);
            Vector2 vector3 = Item.position - Main.screenPosition + vector + vector2;
            spriteBatch.Draw(value, vector3, null, UnderlayColor, 0, vector, scale, SpriteEffects.None, 0);
            spriteBatch.Draw(value1, vector3, null, OverlayColor, 0, vector, scale, SpriteEffects.None, 0);
            return false;
        }
    }
}