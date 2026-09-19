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
        /// 后处理效果的通道与参数句柄（加载时取一次，避免每帧按名字查找）
        /// </summary>
        private static EffectPass? _screenPass;
        private static EffectParameter? _screenTextureParam;
        private static EffectParameter? _screenIntensityParam;
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
            On_FilterManager.EndCapture -= OnFilterManagerEndCapture;
            //静态 Effect / 参数句柄必须在 Unload 清空，否则模组重载后仍会提交已失效的效果。
            KExEffect = null;
            KScreen0Effect = null;
            _screenPass = null;
            _screenTextureParam = null;
            _screenIntensityParam = null;
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
            //专用服务端没有 GraphicsDevice，无法加载 Effect
            if (Main.dedServ)
            {
                return;
            }
            KExEffect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/KEx", AssetRequestMode.ImmediateLoad).Value;
            KScreen0Effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/KScreen0", AssetRequestMode.ImmediateLoad).Value;
            _screenPass = KScreen0Effect?.CurrentTechnique is { Passes.Count: > 0 } technique ? technique.Passes[0] : null;
            _screenTextureParam = KScreen0Effect?.Parameters["tex0"];
            _screenIntensityParam = KScreen0Effect?.Parameters["i"];
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
            //服务端或着色器未就绪时直接交还原版，下面会触碰 Main.spriteBatch
            if (Main.dedServ || _screenPass is null || _screenTextureParam is null || _screenIntensityParam is null)
            {
                orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
                return;
            }
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            //屏幕后处理：把扭曲图层按强度混合回主画面
            if (HasActiveWarp() && !screenTarget1.IsDisposed)
            {
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
                //参数必须在通道 Apply 之前写好，否则本帧提交到设备的仍是上一帧的参数
                _screenTextureParam.SetValue(_warpRT);
                _screenIntensityParam.SetValue(0.02f);
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                _screenPass.Apply();
                Main.spriteBatch.Draw(_screenRT, Vector2.Zero, Color.White);
                Main.spriteBatch.End();
            }
            orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
        }
        /// <summary>
        /// 是否存在需要扭曲的剑；单次遍历即得出结论
        /// </summary>
        private static bool HasActiveWarp()
        {
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.active && proj.ModProjectile is SwordOfTheCosmosProj)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 确保两个 RT 的尺寸与当前屏幕一致，不一致则重建
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