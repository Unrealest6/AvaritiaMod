namespace AvaritiaMod.Common.Systems
{
    /// <summary>
    /// 扭曲系统
    /// </summary>
    public sealed class WarpSystem : ModSystem
    {
        /// <summary>
        /// 扭曲采样效果
        /// </summary>
        internal static Effect? KExEffect { get; private set; }
        /// <summary>
        /// 扭曲后处理效果
        /// </summary>
        private static Effect? KScreen0Effect { get; set; }
        /// <summary>
        /// 屏幕RT
        /// </summary>
        private static RenderTarget2D? _screenRT;
        /// <summary>
        /// 扭曲RT
        /// </summary>
        private static RenderTarget2D? _warpRT;
        public override void Load()
        {
            //添加屏幕后处理hook
            On_FilterManager.EndCapture += OnFilterManagerEndCapture;
        }
        public override void Unload()
        {
            //释放hook及其他资源
            On_FilterManager.EndCapture -= OnFilterManagerEndCapture;
            Main.QueueMainThreadAction(() =>
            {
                _screenRT?.Dispose();
                _screenRT = null;
                _warpRT?.Dispose();
                _warpRT = null;
            });
        }
        public override void PostSetupContent()
        {
            if (Main.dedServ)
            {
                return;
            }
            //加载着色器
            KExEffect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/KEx", AssetRequestMode.ImmediateLoad).Value;
            KScreen0Effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/KScreen0", AssetRequestMode.ImmediateLoad).Value;
        }
        public override void PostUpdatePlayers()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                //应用屏幕震动效果
                ScreenShakeManager.Update();
            }
        }
        public override void ModifyScreenPosition()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                //屏幕震动效果实现
                Main.screenPosition += ScreenShakeManager.GetShakeOffset();
            }
        }
        private void OnFilterManagerEndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
        {
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            //进行屏幕后处理扭曲绘制处理
            if (Main.projectile.Any(p => p.active && p.ModProjectile is SwordOfTheCosmosProj))
            {
                if (screenTarget1.IsDisposed)
                {
                    orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
                    return;
                }
                EnsureRT(gd);
                gd.SetRenderTarget(_screenRT);
                gd.Clear(Color.Black);
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                Main.spriteBatch.Draw(screenTarget1, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
                gd.SetRenderTarget(_warpRT);
                gd.Clear(Color.Black);
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                foreach (Projectile proj in Main.ActiveProjectiles)
                {
                    if (proj.active && proj.ModProjectile is SwordOfTheCosmosProj warp)
                    {
                        warp.DrawWarp();
                    }
                }
                Main.spriteBatch.End();
                gd.SetRenderTarget(Main.screenTarget);
                gd.Clear(Color.Black);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                KScreen0Effect?.CurrentTechnique.Passes[0].Apply();
                KScreen0Effect?.Parameters["tex0"].SetValue(_warpRT);
                KScreen0Effect?.Parameters["i"].SetValue(0.02f);
                Main.spriteBatch.Draw(_screenRT, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
            }
            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
        /// <summary>
        /// 确保RT能够正常使用
        /// </summary>
        /// <param name="graphicsDevice">图形设备实例</param>
        private void EnsureRT(GraphicsDevice graphicsDevice)
        {
            int w = Main.screenWidth;
            int h = Main.screenHeight;
            if (_screenRT == null || _screenRT.Width != w || _screenRT.Height != h)
            {
                _screenRT?.Dispose();
                _screenRT = new RenderTarget2D(graphicsDevice, w, h, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None);
            }
            if (_warpRT != null && _warpRT.Width == w && _warpRT.Height == h)
            {
                return;
            }
            _warpRT?.Dispose();
            _warpRT = new RenderTarget2D(graphicsDevice, w, h, false, graphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.None);
        }
    }
}