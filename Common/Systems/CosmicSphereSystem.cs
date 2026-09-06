namespace AvaritiaMod.Common.Systems
{
    public sealed class CosmicSphereSystem : ModSystem
    {
        internal const float SphereRadius = 384f;
        private static Effect? BlackHoleEffect { get; set; }
        private static Effect? SphereEffect { get; set; }
        private static VertexBuffer? vertexBuffer;
        private static IndexBuffer? indexBuffer;
        private static int indexCount;
        public override void Load()
        {
            if (Main.dedServ)
            {
                return;
            }
            SphereEffect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/CosmicShader3D", AssetRequestMode.ImmediateLoad).Value;
            BlackHoleEffect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/BlackHole", AssetRequestMode.ImmediateLoad).Value;
            SphereVertex[] vertices = SphereMeshGenerator.GenerateSphere(32, 32, SphereRadius);
            int[] indices = SphereMeshGenerator.GenerateSphereIndices(32, 32);
            indexCount = indices.Length;
            Main.QueueMainThreadAction(() =>
            {
                GraphicsDevice? device = Main.graphics.GraphicsDevice;
                if (device == null)
                {
                    return;
                }
                vertexBuffer = new VertexBuffer(device, SphereVertex.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
                vertexBuffer.SetData(vertices);
                indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
                indexBuffer.SetData(indices);
            });
            On_FilterManager.EndCapture += OnFilterManagerEndCapture;
        }
        public override void Unload()
        {
            On_FilterManager.EndCapture -= OnFilterManagerEndCapture;
            Main.QueueMainThreadAction(() =>
            {
                vertexBuffer?.Dispose();
                indexBuffer?.Dispose();
                vertexBuffer = null;
                indexBuffer = null;
            });
        }
        private void OnFilterManagerEndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            foreach (Player player in Main.player)
            {
                if (player.active && !player.dead && player.TryGetModPlayer(out AvaritiaPlayer modPlayer))
                {
                    if (modPlayer.CosmicSphereActive)
                    {
                        DrawSphereForPlayer(player, modPlayer);
                    }
                }
            }
            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
            if (Main.LocalPlayer.TryGetModPlayer(out AvaritiaPlayer localPlayer) && localPlayer.CosmicSphereSuit)
            {
                SpriteBatch spriteBatch = Main.spriteBatch;
                if (!(localPlayer.CosmicSphereTimer is > 0 and <= 360))
                {
                    return;
                }
                float blackHoleScale;
                switch (localPlayer.CosmicSphereTimer)
                {
                    case <= 0:
                        {
                            blackHoleScale = 0f;
                            break;
                        }
                    case <= 300:
                        {
                            float t = localPlayer.CosmicSphereTimer / 300f;
                            blackHoleScale = t * t * (3f - 2f * t);
                            break;
                        }
                    case <= 360:
                        {
                            float t = (localPlayer.CosmicSphereTimer - 300) / 60f;
                            blackHoleScale = (1f - t) * (1f - t);
                            break;
                        }
                    default:
                        {
                            blackHoleScale = 0f;
                            break;
                        }
                }
                if (blackHoleScale <= 0.001f)
                {
                    return;
                }
                if (screenTarget1.IsDisposed)
                {
                    return;
                }
                int targetW = screenTarget1.Width;
                int targetH = screenTarget1.Height;
                Vector2 centerUV = new((Main.LocalPlayer.Center.X - Main.screenPosition.X) / Main.screenWidth, (Main.LocalPlayer.Center.Y - Main.screenPosition.Y) / Main.screenHeight);
                centerUV.X = MathHelper.Clamp(centerUV.X, 0f, 1f);
                centerUV.Y = MathHelper.Clamp(centerUV.Y, 0f, 1f);
                float zoom = Main.GameViewMatrix.ZoomMatrix.M11;
                float baseRange = 0.3f * zoom;
                float baseBlackHole = 0.2f * zoom;
                BlackHoleEffect?.Parameters["uScreenResolution"]?.SetValue(new Vector2(targetW, targetH));
                BlackHoleEffect?.Parameters["uCenter"]?.SetValue(centerUV);
                BlackHoleEffect?.Parameters["uStrength"]?.SetValue(0.2f * blackHoleScale);
                BlackHoleEffect?.Parameters["uRange"]?.SetValue(baseRange * blackHoleScale);
                BlackHoleEffect?.Parameters["uBlackHoleRange"]?.SetValue(baseBlackHole * blackHoleScale);
                BlackHoleEffect?.Parameters["uTime"]?.SetValue(localPlayer.CosmicSphereTimer * 0.01f);
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, BlackHoleEffect, Main.GameViewMatrix.ZoomMatrix);
                spriteBatch.Draw(screenTarget1, new Vector2(Main.screenWidth, Main.screenHeight) / (2f + 2f / (zoom - 1f)), new Rectangle(0, 0, targetW, targetH),
                    Color.White, 0, Vector2.Zero, 1f / zoom, SpriteEffects.None, 0);
                spriteBatch.End();
            }
            else
            {
                orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
            }
        }
        private void DrawSphereForPlayer(Player player, AvaritiaPlayer modPlayer)
        {
            if (modPlayer.CosmicSphereTimer < 300)
            {
                return;
            }
            float alpha = (modPlayer.CosmicSphereTimer - 300) / 240f;
            DrawSphere(player, alpha);
            Vector2 screenPos = player.Center - Main.screenPosition;
            SpriteBatch spriteBatch = Main.spriteBatch;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            modPlayer.CosmicParticles?.UpdateAndDraw(spriteBatch, screenPos, SphereRadius, Main.GameUpdateCount);
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            Texture2D texture = ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/Halo", AssetRequestMode.ImmediateLoad).Value;
            for (int i = 0; i < 3; i++)
            {
                float pulse = MathF.Sin(Main.GameUpdateCount * 0.05f + i * 2.094f) * 0.05f + 1f;
                float radius = SphereRadius * (1.1f + i * 0.08f) * pulse;
                float rotation = Main.GameUpdateCount * (0.01f + i * 0.005f) * (i % 2 == 0 ? 1 : -1);
                Color color = Color.Lerp(new Color(0, 255, 255), new Color(128, 0, 255), i / 3f) * 0.6f;
                spriteBatch.Draw(texture, screenPos, null, color, rotation, texture.Size() / 2f, radius / (texture.Width / 2f), SpriteEffects.None, 0);
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            modPlayer.SphereLightning?.UpdateAndDraw(spriteBatch, screenPos, SphereRadius);
            spriteBatch.End();
        }
        private void DrawSphere(Player player, float alpha = 1f)
        {
            if (vertexBuffer == null || indexBuffer == null)
            {
                return;
            }
            if (!player.active || player.dead)
            {
                return;
            }
            Effect? effect = SphereEffect;
            if (effect?.IsDisposed != false)
            {
                return;
            }
            float gameZoom = Main.GameViewMatrix.Zoom.X * 0.75f;
            Vector2 screenPos = Vector2.Transform(player.Center - Main.screenPosition, Main.GameViewMatrix.ZoomMatrix);
            Matrix world = Matrix.CreateRotationX(player.position.Y / 256f)
                           * Matrix.CreateRotationY(player.position.X / 256f)
                           * Matrix.CreateRotationZ(Main.GameUpdateCount / 1024f)
                           * Matrix.CreateScale(gameZoom)
                           * Matrix.CreateTranslation(new Vector3(screenPos, 0f));
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -SphereRadius * gameZoom, SphereRadius * gameZoom);
            effect.Parameters["uWorldViewProj"]?.SetValue(world * projection);
            effect.Parameters["uTime"]?.SetValue(Main.GameUpdateCount);
            effect.Parameters["uAlpha"]?.SetValue(MathHelper.Clamp(alpha, 0f, 1f));
            effect.Parameters["uBrightness"]?.SetValue(1.3f);
            effect.Parameters["uSpeed"]?.SetValue(0.005f);
            effect.Parameters["uStarDensity"]?.SetValue(0.04f);
            effect.Parameters["externalScale"]?.SetValue(0.4f);
            effect.Parameters["uLayers"]?.SetValue(16f);
            effect.Parameters["uYaw"]?.SetValue(0f);
            effect.Parameters["uPitch"]?.SetValue(0f);
            effect.Parameters["uScreenOffset"]?.SetValue(Vector2.Zero);
            for (int i = 0; i < 10; i++)
            {
                effect.Parameters[$"uTexture{i + 1}"]?.SetValue(AvaritiaFrameSystem.CosmicTextures[i]?.GetCurrentFrame());
            }
            GraphicsDevice device = Main.graphics.GraphicsDevice;
            BlendState oldBlend = device.BlendState;
            DepthStencilState oldDepth = device.DepthStencilState;
            RasterizerState oldRaster = device.RasterizerState;
            device.BlendState = new BlendState
            {
                ColorSourceBlend = Blend.SourceColor,
                ColorDestinationBlend = Blend.DestinationColor,
                ColorBlendFunction = BlendFunction.Add
            };
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullCounterClockwise;
            device.SetVertexBuffer(vertexBuffer);
            device.Indices = indexBuffer;
            effect.CurrentTechnique.Passes[0].Apply();
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertexBuffer.VertexCount, 0, indexCount / 3);
            device.SetVertexBuffer(null);
            device.Indices = null;
            device.BlendState = oldBlend;
            device.DepthStencilState = oldDepth;
            device.RasterizerState = oldRaster;
        }
    }
}