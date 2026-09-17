namespace AvaritiaMod.Content.Items.Consumables
{
    public sealed class EndestPearl : FrameItem
    {
        protected override int FrameCount => 5;
        protected override int FrameDuration => 3;
        protected override FrameDef[] FrameTimeline =>
        [
            0, 0, 0,0,0,0,
            1, 1, 1,
            2, 2,
            3,
            4, 4,
            3, 3,
            2, 1, 1
        ];
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            Item.ResearchUnlockCount = 1;
            Item.rare = ModContent.RarityType<AquaRarity>();
        }
        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.maxStack = Item.CommonMaxStack;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTurn = true;
            Item.useTime = 17;
            Item.useAnimation = 17;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Master;
            Item.shoot = ModContent.ProjectileType<EndestPearlProjectile>();
            Item.shootSpeed = 6f;
            Item.noMelee = true;
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return false;
            }
            Projectile.NewProjectile(source, position, velocity, type, 75, 0, player.whoAmI);
            return false;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/HaloSmall").Value;
            Texture2D? value = FrameTexture?.GetCurrentFrame();
            if (value is null)
            {
                //没有序列帧时交回原版绘制：返回 false 会让物品在背包里彻底不可见。
                return true;
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearWrap, null, null, null, Main.UIScaleMatrix);
            spriteBatch.Draw(texture, position - new Vector2(value.Width * scale / 4f, value.Height * scale / 4f), new Rectangle(0, 0, texture.Width, texture.Height), Color.Black, 0, origin, scale, SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
            spriteBatch.Draw(value, position, null, drawColor, 0, origin, Main.rand.NextFloat(scale * 0.95f, scale * 1.15f), SpriteEffects.None, 0);
            return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
        }
        public override void AddRecipes()
        {
            List<int> a = [ItemID.SolarBrick, ItemID.NebulaBrick, ItemID.StardustBrick, ItemID.VortexBrick];
            const int b = ItemID.WhitePearl;
            int c = ModContent.ItemType<NeutroniumIngot>();
            int d = ModContent.ItemType<NetherStar>();
            new AvaritiaRecipe(Type, 9).AddIngredients(
            [
                0,0,0,a,a,a,0,0,0,
                0,a,a,b,b,b,a,a,0,
                0,a,b,b,b,b,b,a,0,
                a,b,b,b,c,b,b,b,a,
                a,b,b,c,d,c,b,b,a,
                a,b,b,b,c,b,b,b,a,
                0,a,b,b,b,b,b,a,0,
                0,a,a,b,b,b,a,a,0,
                0,0,0,a,a,a,0,0,0
            ]).Register();
        }
    }
}