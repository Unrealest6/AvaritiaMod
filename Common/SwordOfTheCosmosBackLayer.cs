namespace AvaritiaMod.Common
{
    /// <summary>
    /// 处理玩家背着无尽剑的绘制层
    /// </summary>
    public sealed class SwordOfTheCosmosBackLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.BackAcc);
        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (!player.TryGetModPlayer(out AvaritiaPlayer target) || player is { active: false } or { dead: true } or { sleeping.isSleeping: true })
            {
                return;
            }
            if (player.ownedProjectileCounts[ModContent.ProjectileType<SwordOfTheCosmosProj>()] > 0)
            {
                target.SwordOfTheCosmosBackTimer = 60;
                target.SwordOfTheCosmosBackOffset = new Vector2(32, 32);
                return;
            }
            if (player.HeldItem.type != ModContent.ItemType<SwordOfTheCosmos>())
            {
                if (player.inventory[..50].All(item => item.type != ModContent.ItemType<SwordOfTheCosmos>()))
                {
                    return;
                }
                if (target.SwordOfTheCosmosBackHolding)
                {
                    target.SwordOfTheCosmosBackHolding = false;
                }
            }
            else
            {
                if (!target.SwordOfTheCosmosBackHolding && target.SwordOfTheCosmosBackTimer <= -12)
                {
                    target.SwordOfTheCosmosBackTimer = 72;
                    target.SwordOfTheCosmosBackOffset = new Vector2(32, 32);
                }
                target.SwordOfTheCosmosBackHolding = true;
            }
            if (target.SwordOfTheCosmosBackTimer is <= 60 and > -12)
            {
                if (target.SwordOfTheCosmosBackTimer == 45)
                {
                    SoundEngine.PlaySound(new SoundStyle("AvaritiaMod/Assets/Sounds/Sheath"), player.Center);
                }
                Texture2D? texture = SwordOfTheCosmos.FrameTexture?.GetCurrentFrame();
                if (texture is null)
                {
                    return;
                }
                Vector2 finalOffset = new(player.direction * -32f, -32f * player.gravDir);
                if (target.SwordOfTheCosmosBackTimer >= 0)
                {
                    target.SwordOfTheCosmosBackOffset = target.SwordOfTheCosmosBackOffset.Length() < 1f ? Vector2.Zero : Vector2.Lerp(target.SwordOfTheCosmosBackOffset, Vector2.Zero, 1f / MathF.Pow(target.SwordOfTheCosmosBackTimer, 1.15f));
                }
                else
                {
                    Vector2 vector2 = finalOffset - new Vector2(0, Main.GameUpdateCount.TriangleWave(112) / 7f);
                    target.SwordOfTheCosmosBackOffset = target.SwordOfTheCosmosBackOffset.Distance(vector2) < 1f ? vector2 : Vector2.Lerp(target.SwordOfTheCosmosBackOffset, vector2, 0.2f);
                }
                Vector2 offset = new(finalOffset.X + (player.direction == -1 ? target.SwordOfTheCosmosBackOffset.X : -target.SwordOfTheCosmosBackOffset.X), finalOffset.Y - target.SwordOfTheCosmosBackOffset.Y);
                if (target.SwordOfTheCosmosBackTimer < 0)
                {
                    offset = finalOffset + target.SwordOfTheCosmosBackOffset;
                    target.SwordOfTheCosmosBackRotation = float.Lerp(target.SwordOfTheCosmosBackRotation, MathHelper.Pi, 0.2f);
                }
                else
                {
                    target.SwordOfTheCosmosBackRotation = player.direction == -1 ? MathHelper.Pi + MathHelper.PiOver4 : MathHelper.PiOver2 + MathHelper.PiOver4;
                }
                Vector2 position = drawInfo.Center + offset - Main.screenPosition;
                Vector2 origin = new(texture.Width / 2f, texture.Height - 4f);
                float rotation = target.SwordOfTheCosmosBackRotation;
                Color color = drawInfo.colorArmorBody;
                SpriteEffects effects = player.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
                Effect effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/CosmicShader", AssetRequestMode.ImmediateLoad).Value;
                effect.Parameters["uScreenOffset"].SetValue((drawInfo.Center.RotatedBy(player.direction == -1 ? rotation - MathHelper.Pi : rotation - MathHelper.PiOver4) + offset) / 256f);
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
                DrawData data = new(texture, position, null, color, rotation, origin, 1f, effects);
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
            }
            if (target.SwordOfTheCosmosBackHolding)
            {
                if (target.SwordOfTheCosmosBackTimer > 0)
                {
                    target.SwordOfTheCosmosBackTimer--;
                }
            }
            else
            {
                if (target.SwordOfTheCosmosBackTimer > -12)
                {
                    target.SwordOfTheCosmosBackTimer--;
                }
            }
        }
    }
}