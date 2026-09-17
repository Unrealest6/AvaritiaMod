namespace AvaritiaMod.Common.Systems
{
    /// <summary>
    /// 宇宙球体系统
    /// </summary>
    public sealed class CosmicSphereSystem : ModSystem
    {
        /// <summary>
        /// 球体半径常量
        /// </summary>
        public const float SphereRadius = 384f;
        /// <summary>
        /// 黑洞着色器实例
        /// </summary>
        public static Effect? BlackHoleEffect { get; private set; }
        /// <summary>
        /// 宇宙球体着色器实例
        /// </summary>
        public static Effect? CosmicSphereEffect { get; private set; }
        /// <summary>
        /// 顶点缓冲区实例
        /// </summary>
        private static VertexBuffer? _vertexBuffer;
        /// <summary>
        /// 索引缓冲区实例
        /// </summary>
        private static IndexBuffer? _indexBuffer;
        /// <summary>
        /// 索引数量
        /// </summary>
        private static int _indexCount;
        //球体着色器参数句柄（加载时取一次，避免每帧按名字查找）
        private const int SphereWorldViewProj = 0;
        private const int SphereTime = 1;
        private const int SphereAlpha = 2;
        private const int SphereBrightness = 3;
        private const int SphereSpeed = 4;
        private const int SphereStarDensity = 5;
        private const int SphereExternalScale = 6;
        private const int SphereLayers = 7;
        private const int SphereYaw = 8;
        private const int SpherePitch = 9;
        private const int SphereScreenOffset = 10;
        private const int SphereParamCount = 11;
        private static readonly EffectParameter?[] _sphereParams = new EffectParameter?[SphereParamCount];
        private static readonly EffectParameter?[] _sphereTextures = new EffectParameter?[10];
        //黑洞着色器参数句柄
        private const int BlackHoleResolution = 0;
        private const int BlackHoleCenter = 1;
        private const int BlackHoleStrength = 2;
        private const int BlackHoleRange = 3;
        private const int BlackHoleCoreRange = 4;
        private const int BlackHoleTime = 5;
        private static readonly EffectParameter?[] _blackHoleParams = new EffectParameter?[6];
        /// <summary>
        /// 球体光晕贴图（加载时取一次，原实现每帧 ModContent.Request）
        /// </summary>
        private static Texture2D? _haloTexture;
        /// <summary>
        /// 球体绘制用的混合状态（复数）——原实现每次绘制都 <c>new BlendState</c>。
        /// </summary>
        private static BlendState? _sphereBlend;
        public override void Load()
        {
            if (Main.dedServ)
            {
                return;
            }
            //加载两个着色器以及初始化顶点和索引
            CosmicSphereEffect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/CosmicShader3D", AssetRequestMode.ImmediateLoad).Value;
            BlackHoleEffect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/BlackHole", AssetRequestMode.ImmediateLoad).Value;
            _haloTexture = ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/Halo", AssetRequestMode.ImmediateLoad).Value;
            _sphereBlend = new BlendState
            {
                ColorSourceBlend = Blend.SourceColor,
                ColorDestinationBlend = Blend.DestinationColor,
                ColorBlendFunction = BlendFunction.Add
            };
            CacheSphereParameters();
            CacheBlackHoleParameters();
            SphereVertex[] vertices = SphereMeshGenerator.GenerateSphere(32, 32, SphereRadius);
            int[] indices = SphereMeshGenerator.GenerateSphereIndices(32, 32);
            _indexCount = indices.Length;
            Main.QueueMainThreadAction(() =>
            {
                GraphicsDevice? device = Main.graphics.GraphicsDevice;
                if (device == null)
                {
                    return;
                }
                _vertexBuffer = new VertexBuffer(device, SphereVertex.VertexDeclaration, vertices.Length, BufferUsage.WriteOnly);
                _vertexBuffer.SetData(vertices);
                _indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
                _indexBuffer.SetData(indices);
            });
            //使用该hook以实现后处理绘制的效果
            On_FilterManager.EndCapture += OnFilterManagerEndCapture;
        }
        public override void Unload()
        {
            //释放资源
            On_FilterManager.EndCapture -= OnFilterManagerEndCapture;
            //静态 Effect / 参数句柄 / 贴图引用必须在 Unload 清空，
            //否则模组重载后仍会被绘制路径当作有效资源使用。
            BlackHoleEffect = null;
            CosmicSphereEffect = null;
            _haloTexture = null;
            Array.Clear(_sphereParams);
            Array.Clear(_sphereTextures);
            Array.Clear(_blackHoleParams);
            Main.QueueMainThreadAction(() =>
            {
                _vertexBuffer?.Dispose();
                _indexBuffer?.Dispose();
                _vertexBuffer = null;
                _indexBuffer = null;
                _sphereBlend?.Dispose();
                _sphereBlend = null;
            });
        }
        /// <summary>缓存球体着色器的参数句柄。</summary>
        private static void CacheSphereParameters()
        {
            if (CosmicSphereEffect is null)
            {
                return;
            }
            _sphereParams[SphereWorldViewProj] = CosmicSphereEffect.Parameters["uWorldViewProj"];
            _sphereParams[SphereTime] = CosmicSphereEffect.Parameters["uTime"];
            _sphereParams[SphereAlpha] = CosmicSphereEffect.Parameters["uAlpha"];
            _sphereParams[SphereBrightness] = CosmicSphereEffect.Parameters["uBrightness"];
            _sphereParams[SphereSpeed] = CosmicSphereEffect.Parameters["uSpeed"];
            _sphereParams[SphereStarDensity] = CosmicSphereEffect.Parameters["uStarDensity"];
            _sphereParams[SphereExternalScale] = CosmicSphereEffect.Parameters["externalScale"];
            _sphereParams[SphereLayers] = CosmicSphereEffect.Parameters["uLayers"];
            _sphereParams[SphereYaw] = CosmicSphereEffect.Parameters["uYaw"];
            _sphereParams[SpherePitch] = CosmicSphereEffect.Parameters["uPitch"];
            _sphereParams[SphereScreenOffset] = CosmicSphereEffect.Parameters["uScreenOffset"];
            for (int i = 0; i < _sphereTextures.Length; i++)
            {
                _sphereTextures[i] = CosmicSphereEffect.Parameters[$"uTexture{i + 1}"];
            }
        }
        /// <summary>缓存黑洞着色器的参数句柄。</summary>
        private static void CacheBlackHoleParameters()
        {
            if (BlackHoleEffect is null)
            {
                return;
            }
            _blackHoleParams[BlackHoleResolution] = BlackHoleEffect.Parameters["uScreenResolution"];
            _blackHoleParams[BlackHoleCenter] = BlackHoleEffect.Parameters["uCenter"];
            _blackHoleParams[BlackHoleStrength] = BlackHoleEffect.Parameters["uStrength"];
            _blackHoleParams[BlackHoleRange] = BlackHoleEffect.Parameters["uRange"];
            _blackHoleParams[BlackHoleCoreRange] = BlackHoleEffect.Parameters["uBlackHoleRange"];
            _blackHoleParams[BlackHoleTime] = BlackHoleEffect.Parameters["uTime"];
        }
        private void OnFilterManagerEndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            //服务端不绘制任何东西（原实现没有这层保护）
            if (Main.dedServ)
            {
                orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
                return;
            }
            //前处理绘制宇宙球体效果
            foreach (Player player in Main.player)
            {
                if (player.active && !player.dead && player.TryGetModPlayer(out AvaritiaPlayer modPlayer) && modPlayer.CosmicSphereActive)
                {
                    DrawCosmicSphere(player, modPlayer);
                }
            }
            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
            //后处理绘制黑洞衔接动画
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
                if (screenTarget1.IsDisposed || BlackHoleEffect is null)
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
                _blackHoleParams[BlackHoleResolution]?.SetValue(new Vector2(targetW, targetH));
                _blackHoleParams[BlackHoleCenter]?.SetValue(centerUV);
                _blackHoleParams[BlackHoleStrength]?.SetValue(0.2f * blackHoleScale);
                _blackHoleParams[BlackHoleRange]?.SetValue(baseRange * blackHoleScale);
                _blackHoleParams[BlackHoleCoreRange]?.SetValue(baseBlackHole * blackHoleScale);
                _blackHoleParams[BlackHoleTime]?.SetValue(localPlayer.CosmicSphereTimer * 0.01f);
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, BlackHoleEffect, Main.GameViewMatrix.ZoomMatrix);
                spriteBatch.Draw(screenTarget1, new Vector2(Main.screenWidth, Main.screenHeight) / (2f + 2f / (zoom - 1f)), new Rectangle(0, 0, targetW, targetH),
                    Color.White, 0, Vector2.Zero, 1f / zoom, SpriteEffects.None, 0);
                spriteBatch.End();
            }
        }
        /// <summary>
        /// 绘制宇宙球体
        /// </summary>
        /// <param name="player">玩家实例</param>
        /// <param name="modPlayer">ModPlayer实例</param>
        private void DrawCosmicSphere(Player player, AvaritiaPlayer modPlayer)
        {
            if (modPlayer.CosmicSphereTimer < 300)
            {
                return;
            }
            float alpha = (modPlayer.CosmicSphereTimer - 300) / 240f;
            DrawCosmicSphere(player, alpha);
            Vector2 screenPos = player.Center - Main.screenPosition;
            SpriteBatch spriteBatch = Main.spriteBatch;
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            modPlayer.CosmicParticles?.UpdateAndDrawAll(spriteBatch, screenPos, SphereRadius);
            spriteBatch.End();
            if (_haloTexture is null)
            {
                return;
            }
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            for (int i = 0; i < 3; i++)
            {
                float pulse = MathF.Sin(Main.GameUpdateCount * 0.05f + i * 2.094f) * 0.05f + 1f;
                float radius = SphereRadius * (1.1f + i * 0.08f) * pulse;
                float rotation = Main.GameUpdateCount * (0.01f + i * 0.005f) * (i % 2 == 0 ? 1 : -1);
                Color color = Color.Lerp(new Color(0, 255, 255), new Color(128, 0, 255), i / 3f) * 0.6f;
                spriteBatch.Draw(_haloTexture, screenPos, null, color, rotation, _haloTexture.Size() / 2f, radius / (_haloTexture.Width / 2f), SpriteEffects.None, 0);
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            modPlayer.CosmicLightning?.UpdateAndDrawAll(spriteBatch, screenPos, SphereRadius);
            spriteBatch.End();
        }
        /// <summary>
        /// 绘制宇宙球体
        /// </summary>
        /// <param name="player">玩家实例</param>
        /// <param name="alpha">透明度</param>
        private void DrawCosmicSphere(Player player, float alpha = 1f)
        {
            if (_vertexBuffer == null || _indexBuffer == null || _sphereBlend is null)
            {
                return;
            }
            if (!player.active || player.dead)
            {
                return;
            }
            Effect? effect = CosmicSphereEffect;
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
            _sphereParams[SphereWorldViewProj]?.SetValue(world * projection);
            _sphereParams[SphereTime]?.SetValue(Main.GameUpdateCount);
            _sphereParams[SphereAlpha]?.SetValue(MathHelper.Clamp(alpha, 0f, 1f));
            _sphereParams[SphereBrightness]?.SetValue(1.3f);
            _sphereParams[SphereSpeed]?.SetValue(0.005f);
            _sphereParams[SphereStarDensity]?.SetValue(0.04f);
            _sphereParams[SphereExternalScale]?.SetValue(0.4f);
            _sphereParams[SphereLayers]?.SetValue(16f);
            _sphereParams[SphereYaw]?.SetValue(0f);
            _sphereParams[SpherePitch]?.SetValue(0f);
            _sphereParams[SphereScreenOffset]?.SetValue(Vector2.Zero);
            for (int i = 0; i < _sphereTextures.Length; i++)
            {
                _sphereTextures[i]?.SetValue(AvaritiaFrameSystem.CosmicTextures[i]?.GetCurrentFrame());
            }
            GraphicsDevice device = Main.graphics.GraphicsDevice;
            BlendState oldBlend = device.BlendState;
            DepthStencilState oldDepth = device.DepthStencilState;
            RasterizerState oldRaster = device.RasterizerState;
            device.BlendState = _sphereBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullCounterClockwise;
            device.SetVertexBuffer(_vertexBuffer);
            device.Indices = _indexBuffer;
            effect.CurrentTechnique.Passes[0].Apply();
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vertexBuffer.VertexCount, 0, _indexCount / 3);
            device.SetVertexBuffer(null);
            device.Indices = null;
            device.BlendState = oldBlend;
            device.DepthStencilState = oldDepth;
            device.RasterizerState = oldRaster;
        }
    }
}