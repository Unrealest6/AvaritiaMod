namespace AvaritiaMod.Content.Projectiles
{
    public class LongbowOfTheHeavensProj : ModProjectile
    {
        public override string Texture => "AvaritiaMod/Content/Items/Weapons/LongbowOfTheHeavens";
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 2;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (player.HeldItem.type != ModContent.ItemType<LongbowOfTheHeavens>()
                || player.itemAnimation <= 0)
            {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 2;
            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 toMouse = Main.MouseWorld - player.MountedCenter;
                float targetItemRotation = (float)Math.Atan2(toMouse.Y * player.direction, toMouse.X * player.direction);
                Projectile.ai[0] = targetItemRotation;
                Projectile.netUpdate = true;
            }
            Projectile.rotation = Projectile.ai[0];
            Projectile.Center = player.Center;
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, 0);
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - player.direction * MathHelper.PiOver2);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = new(0, 64f);
            float drawRotation = Projectile.rotation + (player.direction == -1 ? MathHelper.Pi : 0);
            Effect effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/CosmicShader", AssetRequestMode.ImmediateLoad).Value;
            effect.Parameters["uScreenOffset"].SetValue(-Projectile.Center / 256f);
            effect.Parameters["uMaskTexture"].SetValue(LongbowOfTheHeavens.MaskFrameTexture?.GetCurrentFrame());
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
            Main.spriteBatch.Draw(LongbowOfTheHeavens.Texture2D, drawPos + (new Vector2(24, 58) * Projectile.scale).RotatedBy(drawRotation), null, lightColor, drawRotation, origin, Projectile.scale, SpriteEffects.FlipHorizontally, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(LongbowOfTheHeavens.FrameTexture?.GetCurrentFrame(), drawPos, null, lightColor, drawRotation, origin, Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(LongbowOfTheHeavens.FrameTexture1?.GetCurrentFrame(), drawPos, null, lightColor, drawRotation, origin, Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}