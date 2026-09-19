namespace AvaritiaMod.Content.Items
{
    /// <summary>奇点物品基类：中子态素压缩机消耗足量输入物品后产出对应奇点；每个奇点同时是无尽催化剂的配方材料。</summary>
    public abstract class Singularity : ModItem
    {
        /// <summary>底层动画帧贴图，以 UnderlayColor 着色。</summary>
        public static FrameTexture? Singularity1 { get; private set; }
        /// <summary>顶层动画帧贴图，以 OverlayColor 着色；同时作为物品图标贴图。</summary>
        public static FrameTexture? Singularity2 { get; private set; }
        /// <summary>奇点注册表：以 RequiredItemType 为键，供中子态素压缩机按输入物品反查奇点。</summary>
        internal static Dictionary<int, Singularity> Singularities { get; set; } = [];
        /// <summary>压缩机产出本奇点所需的输入物品数量。</summary>
        public abstract int RequiredQuantity { get; }
        /// <summary>顶层帧的着色颜色，各奇点靠它区分外观。</summary>
        protected virtual Color OverlayColor => Color.White;
        /// <summary>底层帧的着色颜色，默认比 OverlayColor 暗 64 阶。</summary>
        protected virtual Color UnderlayColor => OverlayColor - 64;
        /// <summary>压缩机消耗的输入物品类型，同时是奇点注册表的键。</summary>
        protected abstract int RequiredItemType { get; }
        /// <summary>为 false 时本奇点不参与无尽催化剂的配方。</summary>
        protected internal virtual bool AddInfinityCatalystRecipe => true;
        /// <summary>所有奇点共用同一张贴图，外观靠 OverlayColor 与 UnderlayColor 区分。</summary>
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
            // 光晕需要 NonPremultiplied + LinearWrap，画完必须切回常规 GUI 绘制状态。
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
