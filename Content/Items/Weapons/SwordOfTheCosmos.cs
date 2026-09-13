namespace AvaritiaMod.Content.Items.Weapons
{
    public sealed class SwordOfTheCosmos : ModItem
    {
        internal static FrameTexture? FrameTexture { get; private set; }
        internal static FrameTexture? FrameTexture1 { get; private set; }
        internal static FrameTexture? FrameTexture2 { get; private set; }
        internal static FrameTexture? FrameTexture3 { get; private set; }
        internal static FrameTexture? MaskFrameTexture { get; private set; }
        public override void SetStaticDefaults()
        {
            Main.RegisterItemAnimation(Type, new DrawAnimationVertical(6, 6));
            FrameTexture = FrameTextureSystem.Register(FullName, "AvaritiaMod/Content/Items/Weapons/SwordOfTheCosmos", 6, 6);
            FrameTexture1 = FrameTextureSystem.Register("SwordOfTheCosmos1", "AvaritiaMod/Content/Items/Weapons/SwordOfTheCosmos1", 6, 6);
            FrameTexture2 = FrameTextureSystem.Register("SwordOfTheCosmos2", "AvaritiaMod/Content/Items/Weapons/SwordOfTheCosmos2", 48, 6);
            FrameTexture3 = FrameTextureSystem.Register("SwordOfTheCosmos3", "AvaritiaMod/Content/Items/Weapons/SwordOfTheCosmos3", 28, 6);
            MaskFrameTexture = FrameTextureSystem.Register("SwordOfTheCosmosMask", "AvaritiaMod/Content/Items/Weapons/SwordOfTheCosmosMask", 6, 6);
        }
        public override void SetDefaults()
        {
            Item.width = 128;
            Item.height = 128;
            Item.damage = 1;
            Item.crit = 0;
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 15;
            Item.knockBack = 6;
            Item.value = 0;
            Item.rare = ModContent.RarityType<LightRedRarity>();
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<SwordOfTheCosmosProj>();
        }
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                if (!player.TryGetModPlayer(out AvaritiaPlayer target) || target.SwordOfTheCosmosMarkTimer > 0)
                {
                    return false;
                }
                Projectile.NewProjectileDirect(player.GetSource_ItemUse(Item), Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<AvaritiaMarkProj>(), 0, 0, player.whoAmI, 60, 1);
                target.SwordOfTheCosmosMarkTimer = 180;
            }
            else
            {
                if (player.ownedProjectileCounts[ModContent.ProjectileType<SwordOfTheCosmosProj>()] < 1)
                {
                    Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, Vector2.Zero, Item.shoot, Item.damage, Item.knockBack, player.whoAmI);
                }
            }
            return false;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips.Where(line => line.Mod == "Terraria"))
            {
                switch (line.Name)
                {
                    case "Damage":
                        line.Text = Lang.GetTooltip(Type).GetLine(0).ApplyGradient(Mod.Name + "Rainbow") + Item.DamageType.DisplayName.Value;
                        break;
                    case "CritChance" or "Tooltip0":
                        line.Hide();
                        break;
                }
            }
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            scale *= 1.5f;
            const float rotation = MathHelper.PiOver4;
            Effect effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/CosmicShader", AssetRequestMode.ImmediateLoad).Value;
            Vector2 screenCenter = position / new Vector2(Main.screenWidth, Main.screenHeight);
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
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect, Main.UIScaleMatrix);
            spriteBatch.Draw(FrameTexture?.GetCurrentFrame(), position, null, drawColor, rotation, origin, scale, SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            spriteBatch.Draw(FrameTexture1?.GetCurrentFrame(), position, null, drawColor, rotation, origin, scale, SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            spriteBatch.Draw(FrameTexture2?.GetCurrentFrame(), position + (new Vector2(58, 94) * scale).RotatedBy(rotation), null, drawColor, rotation, origin, scale, SpriteEffects.None, 0);
            spriteBatch.Draw(FrameTexture3?.GetCurrentFrame(), position + (new Vector2(62, 120) * scale).RotatedBy(rotation), null, drawColor, rotation, origin, scale, SpriteEffects.None, 0);
            return false;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            rotation = MathHelper.PiOver4;
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
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(value, vector3, null, lightColor, rotation, vector, scale, SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(FrameTexture1?.GetCurrentFrame(), vector3, null, lightColor, rotation, vector, scale, SpriteEffects.None, 0);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            spriteBatch.Draw(FrameTexture2?.GetCurrentFrame(), vector3 + (new Vector2(58, 94) * scale).RotatedBy(rotation), null, lightColor, rotation, vector, scale, SpriteEffects.None, 0);
            spriteBatch.Draw(FrameTexture3?.GetCurrentFrame(), vector3 + (new Vector2(62, 120) * scale).RotatedBy(rotation), null, lightColor, rotation, vector, scale, SpriteEffects.None, 0);
            return false;
        }
        public override void AddRecipes()
        {
            AvaritiaRecipe recipe = new(Type, 9);
            int a = ModContent.ItemType<InfinityIngot>();
            int b = ModContent.ItemType<NeutroniumIngot>();
            int c = ModContent.ItemType<InfinityCatalyst>();
            int d = ModContent.ItemType<CrystalMatrixIngot>();
            recipe.AddIngredients(
            [
                0,0,0,0,0,0,0,a,a,
                0,0,0,0,0,0,a,a,a,
                0,0,0,0,0,a,a,a,0,
                0,0,0,0,a,a,a,0,0,
                0,d,0,a,a,a,0,0,0,
                0,0,d,a,a,0,0,0,0,
                0,0,b,d,0,0,0,0,0,
                0,b,0,0,d,0,0,0,0,
                c,0,0,0,0,0,0,0,0
            ]).Register();
        }
    }
}