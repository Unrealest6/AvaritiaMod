namespace AvaritiaMod.Common
{
    public sealed class SwordOfTheCosmosFlyLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Shield);
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (!player.TryGetModPlayer(out AvaritiaPlayer target) || player is { active: false } or { dead: true } or { sleeping.isSleeping: true } || player.ownedProjectileCounts[ModContent.ProjectileType<SwordOfTheCosmosProj>()] > 0)
            {
                return;
            }
            if (player.HeldItem.type == ModContent.ItemType<SwordOfTheCosmos>())
            {
                if (!target.SwordOfTheCosmosFlyHolding)
                {
                    target.SwordOfTheCosmosFlyTimer = 12;
                    target.SwordOfTheCosmosFlyRotation = MathHelper.Pi;
                }
                target.SwordOfTheCosmosFlyHolding = true;
            }
            else if (player.inventory[..50].Any(item => item.type == ModContent.ItemType<SwordOfTheCosmos>()))
            {
                if (target.SwordOfTheCosmosFlyHolding && target.SwordOfTheCosmosFlyTimer <= 0)
                {
                    target.SwordOfTheCosmosFlyHolding = false;
                    target.SwordOfTheCosmosFlyTimer = (sbyte)(12 + target.SwordOfTheCosmosBackTimer);
                    target.SwordOfTheCosmosFlyOffset = Vector2.Zero;
                }
            }
            else
            {
                return;
            }
            switch (target.SwordOfTheCosmosFlyHolding)
            {
                case true when target.SwordOfTheCosmosFlyTimer < 0 || target.SwordOfTheCosmosBackTimer < 60:
                    target.SwordOfTheCosmosFlyTimer = 0;
                    return;
                case false when target.SwordOfTheCosmosFlyTimer > 0:
                    target.SwordOfTheCosmosFlyTimer--;
                    return;
            }
            target.SwordOfTheCosmosFlyRotation = target.SwordOfTheCosmosFlyTimer > 0 ? float.Lerp(target.SwordOfTheCosmosFlyRotation, MathHelper.PiOver2 + MathHelper.PiOver4, 0.2f) : MathHelper.Pi;
            Texture2D? texture = SwordOfTheCosmos.FrameTexture?.GetCurrentFrame();
            if (texture is null)
            {
                return;
            }
            Vector2 finalOffset = new(player.direction * -64f, -64f * player.gravDir);
            float rotation = target.SwordOfTheCosmosFlyRotation;
            if (target.SwordOfTheCosmosFlyTimer > 0)
            {
                Vector2 lerp = player.direction == -1 ? new Vector2(32, -32) : new Vector2(-32, -32);
                target.SwordOfTheCosmosFlyOffset = target.SwordOfTheCosmosFlyOffset.Distance(lerp) < 1f ? lerp : Vector2.Lerp(target.SwordOfTheCosmosFlyOffset, lerp, 0.2f);
                rotation = player.direction == -1 ? -rotation : rotation;
            }
            else
            {
                target.SwordOfTheCosmosFlyOffset = target.SwordOfTheCosmosFlyOffset.Length() < 1f ? finalOffset : Vector2.Lerp(target.SwordOfTheCosmosFlyOffset, finalOffset, 0.05f);
            }
            Vector2 offset = target.SwordOfTheCosmosFlyTimer > 0 ? new Vector2(finalOffset.X / 2f + target.SwordOfTheCosmosFlyOffset.X, finalOffset.Y / 2f + target.SwordOfTheCosmosFlyOffset.Y) : target.SwordOfTheCosmosFlyOffset;
            Vector2 position = drawInfo.Center + offset - Main.screenPosition;
            if (target.SwordOfTheCosmosFlyTimer <= 0)
            {
                position -= new Vector2(0, Main.GameUpdateCount.TriangleWave(112) / 7f);
            }
            Vector2 origin = new(texture.Width / 2f, texture.Height - 4f);
            Color color = drawInfo.colorArmorBody;
            SpriteEffects effects = player.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Effect effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/CosmicShader", AssetRequestMode.ImmediateLoad).Value;
            effect.Parameters["uScreenOffset"].SetValue(drawInfo.Center / 256f);
            effect.Parameters["uMaskTexture"].SetValue(SwordOfTheCosmos.MaskFrameTexture?.GetCurrentFrame());
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
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(texture, position + new Vector2(0, 2f).RotatedBy(rotation), null, color * 0.75f, rotation, origin, 1.15f, effects, 0f);
            float scale = 1.15f + Main.GameUpdateCount % 120 / 240f;
            Main.spriteBatch.Draw(texture, position + new Vector2(0, scale * 3f).RotatedBy(rotation), null, color * (1f - Main.GameUpdateCount % 120 / 120f) * 0.75f, rotation, origin, scale, effects, 0f);
            DrawData data = new(texture, position, null, color, rotation, origin, 1, effects);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
            data.Draw(Main.spriteBatch);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            data.texture = SwordOfTheCosmos.FrameTexture1?.GetCurrentFrame();
            data.Draw(Main.spriteBatch);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            data.texture = SwordOfTheCosmos.FrameTexture2?.GetCurrentFrame();
            data.position += new Vector2(58, 94).RotatedBy(rotation);
            data.Draw(Main.spriteBatch);
            data.texture = SwordOfTheCosmos.FrameTexture3?.GetCurrentFrame();
            data.position += new Vector2(4, 26).RotatedBy(rotation);
            data.Draw(Main.spriteBatch);
            if (target.SwordOfTheCosmosFlyTimer > 0)
            {
                target.SwordOfTheCosmosFlyTimer--;
            }
        }
    }
}