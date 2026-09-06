namespace AvaritiaMod.Common.Systems;

public sealed class WarpSystem : ModSystem
{
    internal static RenderTarget2D? CosmicRT { get; private set; }
    internal static Effect? KExEffect { get; private set; }
    private static Effect? KScreen0Effect { get; set; }
    private RenderTarget2D? _screenRT;
    private static RenderTarget2D? _warpRT;
    public override void Load()
    {
        On_FilterManager.EndCapture += OnFilterManagerEndCapture;
        Main.OnResolutionChanged += OnResolutionChanged;
    }
    public static void RenderCosmicRT(Vector2 cosmicCenter, float cosmicRadius)
    {
        GraphicsDevice gd = Main.graphics.GraphicsDevice;
        float zoom = Main.GameViewMatrix.ZoomMatrix.M11;
        int rtSize = (int)(cosmicRadius * 2 * zoom);
        rtSize = (int)MathHelper.Clamp(rtSize, 128, 2048);
        if (CosmicRT == null || CosmicRT.Width != rtSize || CosmicRT.Height != rtSize)
        {
            CosmicRT?.Dispose();
            CosmicRT = new RenderTarget2D(gd, rtSize, rtSize, false, gd.PresentationParameters.BackBufferFormat, DepthFormat.None);
        }
        gd.SetRenderTarget(CosmicRT);
        gd.Clear(Color.Transparent);
        Effect cosmicEffect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/CosmicShader", AssetRequestMode.ImmediateLoad).Value;
        cosmicEffect.Parameters["uScreenOffset"].SetValue(Main.player[Main.myPlayer].position / 256f);
        cosmicEffect.Parameters["uMaskTexture"].SetValue(TextureAssets.MagicPixel.Value);
        cosmicEffect.Parameters["uTime"].SetValue(Main.GlobalTimeWrappedHourly * 1f);
        cosmicEffect.Parameters["uAlpha"].SetValue(1f);
        cosmicEffect.Parameters["uSpeed"].SetValue(0.006f);
        cosmicEffect.Parameters["uStarDensity"].SetValue(0.1f);
        cosmicEffect.Parameters["uBrightness"].SetValue(1.3f);
        cosmicEffect.Parameters["externalScale"].SetValue(0.3f);
        cosmicEffect.Parameters["uLayers"].SetValue(16);
        for (int i = 0; i < 10; i++)
        {
            string texName = "uTexture" + (i + 1);
            Texture2D? starTex = AvaritiaFrameSystem.CosmicTextures[i]?.GetCurrentFrame();
            cosmicEffect.Parameters[texName].SetValue(starTex);
        }
        SpriteBatch sb = new(gd);
        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, cosmicEffect, Matrix.Identity);
        cosmicEffect.CurrentTechnique.Passes[0].Apply();
        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, rtSize, rtSize), Color.White);
        sb.End();
        sb.Dispose();
        gd.SetRenderTarget(Main.screenTarget);
    }
    public override void Unload()
    {
        On_FilterManager.EndCapture -= OnFilterManagerEndCapture;
        Main.OnResolutionChanged -= OnResolutionChanged;
        Main.QueueMainThreadAction(() =>
        {
            _screenRT?.Dispose();
            _screenRT = null;
            CosmicRT?.Dispose();
            CosmicRT = null;
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
        KExEffect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/KEx", AssetRequestMode.ImmediateLoad).Value;
        KScreen0Effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/KScreen0", AssetRequestMode.ImmediateLoad).Value;
    }
    public override void PostUpdatePlayers()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            ScreenShakeManager.Update();
        }
    }
    public override void ModifyScreenPosition()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            Main.screenPosition += ScreenShakeManager.GetShakeOffset();
        }
    }
    private void OnResolutionChanged(Vector2 obj)
    {
        _screenRT?.Dispose();
        _screenRT = null;
        _warpRT?.Dispose();
        _warpRT = null;
    }
    private void OnFilterManagerEndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
    {
        GraphicsDevice gd = Main.instance.GraphicsDevice;
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
    private void EnsureRT(GraphicsDevice gd)
    {
        int w = Main.screenWidth;
        int h = Main.screenHeight;
        if (_screenRT == null || _screenRT.Width != w || _screenRT.Height != h)
        {
            _screenRT?.Dispose();
            _screenRT = new RenderTarget2D(gd, w, h, false, gd.PresentationParameters.BackBufferFormat, DepthFormat.None);
        }
        if (_warpRT != null && _warpRT.Width == w && _warpRT.Height == h)
        {
            return;
        }
        _warpRT?.Dispose();
        _warpRT = new RenderTarget2D(gd, w, h, false, gd.PresentationParameters.BackBufferFormat, DepthFormat.None);
    }
}