namespace AvaritiaMod.Content.Items.Weapons
{
    public sealed class LongbowOfTheHeavens : ModItem
    {
        internal static FrameTexture? FrameTexture { get; private set; }
        internal static FrameTexture? FrameTexture1 { get; private set; }
        internal static Texture2D? Texture2D { get; private set; }
        internal static FrameTexture? MaskFrameTexture { get; private set; }
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 6));
            FrameTexture = FrameTextureSystem.Register(FullName, "AvaritiaMod/Content/Items/Weapons/LongbowOfTheHeavens", 6, 6);
            FrameTexture1 = FrameTextureSystem.Register("LongbowOfTheHeavens1", "AvaritiaMod/Content/Items/Weapons/LongbowOfTheHeavens1", 6, 6);
            Texture2D = ModContent.Request<Texture2D>("AvaritiaMod/Content/Items/Weapons/LongbowOfTheHeavens2", AssetRequestMode.ImmediateLoad).Value;
            MaskFrameTexture = FrameTextureSystem.Register("LongbowOfTheHeavensMask", "AvaritiaMod/Content/Items/Weapons/LongbowOfTheHeavensMask", 6, 6);
        }
        public override void SetDefaults()
        {
            Item.width = 64;
            Item.height = 128;
            Item.damage = 2000;
            Item.crit = 6;
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.shoot = ModContent.ProjectileType<HeavenArrow>();
            Item.shootSpeed = 54f;
            Item.shootsEveryUse = true;
            Item.knockBack = 2;
            Item.value = 0;
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player) => player.altFunctionUse == 2
            && (!player.TryGetModPlayer(out AvaritiaPlayer target) || target.LongbowOfTheHeavensMarkTimer > 0)
                ? false : base.CanUseItem(player);
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            if (player.whoAmI != Main.myPlayer || player.itemAnimation <= 0)
            {
                return;
            }
            int heldType = ModContent.ProjectileType<LongbowOfTheHeavensProj>();
            if (player.ownedProjectileCounts[heldType] <= 0)
            {
                Projectile.NewProjectile(
                    player.GetSource_ItemUse(Item),
                    player.Center,
                    Vector2.Zero,
                    heldType,
                    0,
                    0,
                    player.whoAmI
                );
            }
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return false;
            }
            if (player.altFunctionUse == 2)
            {
                if (!player.TryGetModPlayer(out AvaritiaPlayer target))
                {
                    return false;
                }
                Projectile.NewProjectileDirect(source, Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<AvaritiaMarkProj>(), 0, 0, player.whoAmI, 192);
                for (int i = 0; i < 42; i++)
                {
                    Projectile projectile = Projectile.NewProjectileDirect(source, Main.MouseWorld + Vector2.One.RotatedBy(MathHelper.Pi / 21f * i) * 128f, Vector2.Zero
                        , ModContent.ProjectileType<HeavenArrow>(), damage, knockback, player.whoAmI);
                    if (projectile.ModProjectile is HeavenArrow proj1)
                    {
                        proj1.MarkCenter = Main.MouseWorld;
                        proj1.SerialNum = i;
                    }
                    projectile.timeLeft += 162;
                    projectile.penetrate = -1;
                    projectile.tileCollide = false;
                    projectile.netUpdate = true;
                }
                target.LongbowOfTheHeavensMarkTimer = 300;
                return false;
            }
            if (Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, player.whoAmI).ModProjectile is not HeavenArrow proj)
            {
                return false;
            }
            if (player.altFunctionUse != 2)
            {
                proj.IsTrack = true;
            }
            return false;
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Effect effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/CosmicShader", AssetRequestMode.ImmediateLoad).Value;
            Vector2 screenCenter = position / new Vector2(Main.screenWidth, Main.screenHeight);
            effect.Parameters["uScreenOffset"].SetValue(screenCenter * 6f);
            effect.Parameters["uMaskTexture"].SetValue(MaskFrameTexture?.GetCurrentFrame());
            effect.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 1f);
            effect.Parameters["uAlpha"].SetValue(1f);
            effect.Parameters["uSpeed"].SetValue(0.006f);
            effect.Parameters["uStarDensity"].SetValue(0.1f);
            effect.Parameters["uBrightness"].SetValue(1.3f);
            effect.Parameters["externalScale"].SetValue(0.3f);
            effect.Parameters["uLayers"].SetValue(16);
            for (int i = 0; i < 10; i++)
            {
                string texName = "uTexture" + (i + 1);
                Texture2D? starTex = AvaritiaFrameSystem.CosmicTextures[i]?.GetCurrentFrame();
                effect.Parameters[texName].SetValue(starTex);
            }
            spriteBatch.Draw(Texture2D, position + new Vector2(24, 58) * scale, null, drawColor, 0, origin, scale, SpriteEffects.FlipHorizontally, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect, Main.UIScaleMatrix);
            spriteBatch.Draw(FrameTexture?.GetCurrentFrame(), position, null, drawColor, 0, origin, scale, SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            spriteBatch.Draw(FrameTexture1?.GetCurrentFrame(), position, null, drawColor, 0, origin, scale, SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            return false;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Texture2D? value = FrameTexture?.GetCurrentFrame();
            Vector2 vector = new Vector2(value?.Width ?? 0, value?.Height ?? 0) / 2f;
            Vector2 vector3 = Item.position - Main.screenPosition + vector;
            Effect effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/CosmicShader", AssetRequestMode.ImmediateLoad).Value;
            Vector2 screenCenter = vector3 / new Vector2(Main.screenWidth, Main.screenHeight);
            effect.Parameters["uScreenOffset"].SetValue(screenCenter.RotatedBy(-rotation) * 6f);
            effect.Parameters["uMaskTexture"].SetValue(MaskFrameTexture?.GetCurrentFrame());
            effect.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 1f);
            effect.Parameters["uAlpha"].SetValue(1f);
            effect.Parameters["uSpeed"].SetValue(0.006f);
            effect.Parameters["uStarDensity"].SetValue(0.1f);
            effect.Parameters["uBrightness"].SetValue(1.3f);
            effect.Parameters["externalScale"].SetValue(0.3f);
            effect.Parameters["uLayers"].SetValue(16);
            for (int i = 0; i < 10; i++)
            {
                string texName = "uTexture" + (i + 1);
                Texture2D? starTex = AvaritiaFrameSystem.CosmicTextures[i]?.GetCurrentFrame();
                effect.Parameters[texName].SetValue(starTex);
            }
            spriteBatch.Draw(Texture2D, vector3 + (new Vector2(24, 58) * scale).RotatedBy(rotation), null, lightColor, 0, vector, scale, SpriteEffects.FlipHorizontally, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(value, vector3, null, lightColor, rotation, vector, scale, SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(FrameTexture1?.GetCurrentFrame(), vector3, null, lightColor, rotation, vector, scale, SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override void AddRecipes()
        {
            int a = ModContent.ItemType<InfinityIngot>();
            const int b = ItemID.Silk;
            int c = ModContent.ItemType<CrystalMatrix>();
            new AvaritiaRecipe(Type, 9).AddIngredients(
            [
                0,0,0,a,a,0,0,0,0,
                0,0,a,0,b,0,0,0,0,
                0,a,0,0,b,0,0,0,0,
                a,0,0,0,b,0,0,0,0,
                c,0,0,0,b,0,0,0,0,
                a,0,0,0,b,0,0,0,0,
                0,a,0,0,b,0,0,0,0,
                0,0,a,0,b,0,0,0,0,
                0,0,0,a,a,0,0,0,0
            ]).Register();
        }
    }
}