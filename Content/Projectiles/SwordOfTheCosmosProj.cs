namespace AvaritiaMod.Content.Projectiles
{
    public sealed class SwordOfTheCosmosProj : ModProjectile
    {
        private const float DisFromPlayer = 6f;
        private const float SwordLength = 140f;
        private const float ViewZ = 300f;
        private const int TrailLength = 120;
        private static readonly int[] PrepareTimes = [8, 6, 6];
        private static readonly int[] SwingTimes = [8, 8, 14];
        private static readonly int[] UnwindTimes = [10, 10, 12];
        private Vector2 _drawVector2;
        private Vector2 _lastEndVector2;
        private Vector2[]? _trailVector2s;
        private int _attackType;
        private int _timer;
        private float _currentAngle;
        private float _currentTilt;
        private Vector2 _vertexVector2;
        private bool _isAttacking;
        private float _lockedMouseAngle;
        private bool _hasLockedMouse;
        private bool _isCosmicPhase;
        private Vector2 _cosmicCenter;
        private float _cosmicRadius;
        private float MouseAngle => (Main.MouseWorld - Main.player[Projectile.owner].MountedCenter).ToRotation();
        public override string Texture => "AvaritiaMod/Content/Items/Weapons/SwordOfTheCosmos";
        public override void SetDefaults()
        {
            Projectile.width = 240;
            Projectile.height = 240;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 60;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
            Projectile.ownerHitCheck = true;
            Projectile.DamageType = DamageClass.Melee;
            _trailVector2s = new Vector2[TrailLength];
        }
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            Projectile.spriteDirection = Main.MouseWorld.X > player.Center.X ? 1 : -1;
            player.direction = Projectile.spriteDirection;
            int initialAttack = (int)Projectile.ai[0];
            if (initialAttack < 0 || initialAttack > 2)
            {
                initialAttack = 0;
            }
            _attackType = initialAttack;
            switch (initialAttack)
            {
                case 0:
                    _currentAngle = MathHelper.Pi * 0.75f;
                    break;
                case 1:
                    _currentAngle = -MathHelper.Pi * 0.75f;
                    break;
                case 2:
                    _currentAngle = MathHelper.PiOver2 + MathHelper.PiOver4;
                    break;
            }
            _timer = 0;
            _currentTilt = 0f;
            _currentAngle = MathHelper.Pi * 0.75f;
        }
        private static Vector2 EllipticalProjection(float radius, float angle, float tilt, float rotation = 0f, float viewZ = 500f)
        {
            float cosR = float.Cos(rotation);
            float sinR = float.Sin(rotation);
            float x0 = radius * float.Cos(-angle);
            float y0 = radius * float.Sin(-angle);
            float tiltY = y0 * float.Cos(tilt);
            float tiltZ = -y0 * float.Sin(tilt);
            float k = -viewZ / (tiltZ - viewZ);
            return new Vector2(k * (x0 * cosR + tiltY * sinR), k * (-x0 * sinR + tiltY * cosR));
        }
        private void NextAttackType()
        {
            _lastEndVector2 = _vertexVector2;
            if (_trailVector2s is not null)
            {
                Array.Clear(_trailVector2s, 0, _trailVector2s.Length);
            }
            _timer = 0;
            _attackType++;
            if (_attackType > 2)
            {
                Projectile.Kill();
                return;
            }
            Player player = Main.player[Projectile.owner];
            Projectile.spriteDirection = Main.MouseWorld.X > player.Center.X ? 1 : -1;
            player.direction = Projectile.spriteDirection;
            _hasLockedMouse = false;
        }
        public override void AI()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            Projectile.netUpdate = true;
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems)
            {
                Projectile.Kill();
                return;
            }
            if (Projectile.owner == Main.myPlayer)
            {
                player.itemAnimation = 2;
                player.itemTime = 2;
                player.heldProj = Projectile.whoAmI;
                Projectile.timeLeft = 2;
                int prepare = PrepareTimes[_attackType];
                int swing = SwingTimes[_attackType];
                int unwind = UnwindTimes[_attackType];
                _timer++;
                float targetTilt = _attackType switch
                {
                    0 => -1.2f,
                    1 => 1.2f,
                    _ => 0.2f
                };
                float rotStart, rotEnd, rotPull;
                switch (_attackType)
                {
                    case 0:
                        rotStart = MathHelper.Pi * 0.75f;
                        rotEnd = -MathHelper.Pi * 0.75f;
                        rotPull = MathHelper.Pi;
                        break;
                    case 1:
                        rotStart = -MathHelper.Pi * 0.75f;
                        rotEnd = MathHelper.Pi * 0.75f;
                        rotPull = rotStart;
                        break;
                    default:
                        rotPull = MathHelper.Pi;
                        rotStart = MathHelper.PiOver2 + MathHelper.PiOver4;
                        rotEnd = -MathHelper.Pi + MathHelper.PiOver4;
                        break;
                }
                if (_timer < prepare)
                {
                    float t = _timer / (float)prepare;
                    t = t * t * (3f - 2f * t);
                    _currentAngle = MathHelper.Lerp(rotPull, rotStart, t);
                    _currentTilt = MathHelper.Lerp(_currentTilt, targetTilt, t);
                    _isAttacking = false;
                    if (_timer == prepare - 1)
                    {
                        _lockedMouseAngle = MouseAngle;
                        _hasLockedMouse = true;
                    }
                }
                else if (_timer == prepare)
                {
                    SoundEngine.PlaySound(_attackType is 0 or 1 ? new SoundStyle("AvaritiaMod/Assets/Sounds/Swing") { Pitch = 0.6f } : new SoundStyle("AvaritiaMod/Assets/Sounds/Swing"), player.Center);
                }
                else if (_timer < prepare + swing)
                {
                    if (!_hasLockedMouse)
                    {
                        _lockedMouseAngle = MouseAngle;
                        _hasLockedMouse = true;
                    }
                    float t = (_timer - prepare) / (float)swing;
                    t = t * t * (3f - 2f * t);
                    _currentAngle = MathHelper.Lerp(rotStart, rotEnd, t);
                    _currentTilt = targetTilt;
                    _isAttacking = true;
                }
                else if (_timer < prepare + swing + unwind)
                {
                    float shakeIntensity = _attackType switch
                    {
                        2 => 6f,
                        _ => 2f
                    };
                    ScreenShakeManager.AddShake(shakeIntensity, 10);
                    _currentAngle = rotEnd;
                    _currentTilt = targetTilt;
                    _isAttacking = false;
                }
                else
                {
                    if (_attackType < 2 && player.controlUseItem)
                    {
                        NextAttackType();
                    }
                    else
                    {
                        Projectile.Kill();
                    }
                    return;
                }
                float mouseAngle = MouseAngle;
                float effectiveMouseAngle = mouseAngle;
                float effectiveLockedAngle = _lockedMouseAngle;
                if (Projectile.spriteDirection == -1)
                {
                    effectiveMouseAngle = MathHelper.Pi - mouseAngle;
                    while (effectiveMouseAngle > MathHelper.Pi)
                    {
                        effectiveMouseAngle -= MathHelper.TwoPi;
                    }
                    while (effectiveMouseAngle < -MathHelper.Pi)
                    {
                        effectiveMouseAngle += MathHelper.TwoPi;
                    }
                    effectiveLockedAngle = MathHelper.Pi - _lockedMouseAngle;
                    while (effectiveLockedAngle > MathHelper.Pi)
                    {
                        effectiveLockedAngle -= MathHelper.TwoPi;
                    }
                    while (effectiveLockedAngle < -MathHelper.Pi)
                    {
                        effectiveLockedAngle += MathHelper.TwoPi;
                    }
                }
                float rot2 = _hasLockedMouse ? -effectiveLockedAngle : -effectiveMouseAngle;
                _vertexVector2 = EllipticalProjection(SwordLength * Projectile.scale, _currentAngle, _currentTilt, rot2, ViewZ);
                Vector2 holdOffset = EllipticalProjection(DisFromPlayer, _currentAngle, _currentTilt, rot2, ViewZ);
                if (Projectile.spriteDirection == -1)
                {
                    _vertexVector2.X = -_vertexVector2.X;
                    holdOffset.X = -holdOffset.X;
                }
                Projectile.Center = player.MountedCenter + holdOffset;
                Projectile.rotation = _vertexVector2.ToRotation();
                if (_timer < prepare && _attackType > 0)
                {
                    float t = _timer / (float)prepare;
                    t = t * t * (3f - 2f * t);
                    _drawVector2 = Vector2.Lerp(_lastEndVector2, _vertexVector2, t);
                }
                else
                {
                    _drawVector2 = _vertexVector2;
                }
                if (_trailVector2s is not null)
                {
                    for (int i = _trailVector2s.Length - 1; i > 0; i--)
                    {
                        _trailVector2s[i] = _trailVector2s[i - 1];
                    }
                    _trailVector2s[0] = _isAttacking ? _vertexVector2 : Vector2.Zero;
                    if (_attackType == 2)
                    {
                        if (_isAttacking)
                        {
                            _isCosmicPhase = true;
                        }
                        else if (_isCosmicPhase)
                        {
                            bool any = _trailVector2s.Any(t => t != Vector2.Zero);
                            if (!any)
                            {
                                _isCosmicPhase = false;
                            }
                        }
                    }
                    else
                    {
                        _isCosmicPhase = false;
                    }
                    if (_isCosmicPhase)
                    {
                        _cosmicCenter = Projectile.Center;
                        float max = 0f;
                        for (int i = 0; i < _trailVector2s.Length; i++)
                        {
                            if (_trailVector2s[i] != Vector2.Zero)
                            {
                                max = float.Max(max, _trailVector2s[i].Length());
                            }
                        }
                        _cosmicRadius = max * Projectile.scale;
                    }
                }
                SpawnParticles(prepare, swing, unwind);
            }
            float armRot = MathHelper.WrapAngle(_vertexVector2.ToRotation() - MathHelper.PiOver2);
            player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, 0);
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot);
            player.direction = Projectile.spriteDirection;
            if (_isCosmicPhase)
            {
                WarpSystem.RenderCosmicRT(_cosmicCenter, _cosmicRadius);
            }
        }
        private void SpawnParticles(int prepare, int swing, int unwind)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            Color trailColor = new(30, 70, 120);
            Color cosmicColor = new(30, 70, 80);
            Color activeColor = _isCosmicPhase ? cosmicColor : trailColor;
            if (_timer < prepare)
            {
                Vector2 tip = Projectile.Center + _vertexVector2;
                float progress = _timer / (float)prepare;
                if (_timer % 2 == 0)
                {
                    float radius = 60f * (1f - progress);
                    Vector2 spawn = tip + Main.rand.NextVector2Circular(radius, radius);
                    Vector2 toTip = (tip - spawn).SafeNormalize(Vector2.Zero);
                    float speed = 2f + (1f - Vector2.Distance(spawn, tip) / radius) * 4f;
                    Dust d = Dust.NewDustPerfect(spawn, DustID.PurificationPowder, toTip * speed, 0, activeColor, 0.8f + progress * 0.5f);
                    d.noGravity = true;
                    d.fadeIn = 1.2f;
                }
                if (progress > 0.6f && _timer % 3 == 0)
                {
                    Dust d = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(8, 8),
                        DustID.PurificationPowder, Vector2.Zero, 0, Color.White, 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 0.3f;
                }
            }
            else if (_timer < prepare + swing)
            {
                Vector2 tip = Projectile.Center + _vertexVector2;
                Vector2 swordDir = _vertexVector2.SafeNormalize(Vector2.UnitX);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 vel = swordDir.RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(2f, 6f);
                    vel -= swordDir * Main.rand.NextFloat(0.5f, 2f);
                    Dust d = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(6, 6),
                        DustID.Electric, vel, 0, Color.White, 0.7f);
                    d.noGravity = true;
                    d.fadeIn = 0.8f;
                }
                if (Main.rand.NextBool(2))
                {
                    Vector2 mid = Projectile.Center + _vertexVector2 * Main.rand.NextFloat(0.2f, 0.8f);
                    Dust d = Dust.NewDustPerfect(mid, DustID.PurificationPowder,
                        Main.rand.NextVector2Circular(0.5f, 0.5f), 0, activeColor, 0.7f);
                    d.noGravity = true;
                    d.fadeIn = 1f;
                }
            }
            else if (_timer < prepare + swing + unwind)
            {
                if (_isCosmicPhase && Main.rand.NextBool(2))
                {
                    Vector2 pos = Projectile.Center + _vertexVector2 * Main.rand.NextFloat(0.3f, 1.0f);
                    Dust d = Dust.NewDustPerfect(pos, DustID.PurificationPowder,
                        -Vector2.UnitY.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.3f, 1.5f),
                        0, cosmicColor, 1.2f);
                    d.noGravity = true;
                    d.fadeIn = 1.2f;
                }
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                target.StrikeInstantKill();
                if (Main.netMode == NetmodeID.MultiplayerClient && Projectile.owner == Main.myPlayer)
                {
                    ModPacket packet = ModContent.GetInstance<AvaritiaMod>().GetPacket();
                    packet.Write((byte)AvaritiaMod.SyncMessageType.RequestKillNPC);
                    packet.Write(target.whoAmI);
                    packet.Send();
                }
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Color activeColor = _isCosmicPhase ? new Color(30, 70, 80) : new Color(30, 70, 120);
            for (int i = 0; i < 25; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(6f, 14f);
                Dust d = Dust.NewDustPerfect(target.Center, DustID.PurificationPowder, speed, 0, activeColor, 1.4f);
                d.noGravity = Main.rand.NextBool(2);
                d.fadeIn = 1f;
            }
            for (int i = 0; i < 16; i++)
            {
                float angle = MathHelper.TwoPi / 16 * i;
                Vector2 speed = angle.ToRotationVector2() * Main.rand.NextFloat(8f, 16f);
                Dust d = Dust.NewDustPerfect(target.Center, DustID.PurificationPowder, speed, 0, Color.White, 1.2f);
                d.noGravity = true;
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Color activeColor = _isCosmicPhase ? new Color(30, 70, 80) : new Color(30, 70, 120);
            for (int i = 0; i < 25; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(6f, 14f);
                Dust d = Dust.NewDustPerfect(target.Center, DustID.PurificationPowder, speed, 0, activeColor, 1.4f);
                d.noGravity = Main.rand.NextBool(2);
                d.fadeIn = 1f;
            }
            for (int i = 0; i < 16; i++)
            {
                float angle = MathHelper.TwoPi / 16 * i;
                Vector2 speed = angle.ToRotationVector2() * Main.rand.NextFloat(8f, 16f);
                Dust d = Dust.NewDustPerfect(target.Center, DustID.PurificationPowder, speed, 0, Color.White, 1.2f);
                d.noGravity = true;
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!_isAttacking)
            {
                return false;
            }
            Vector2 start = Projectile.Center + Vector2.Normalize(_vertexVector2) * 20f;
            Vector2 end = Projectile.Center + _vertexVector2;
            float cp = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 36f * Projectile.scale, ref cp);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return false;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            DrawSword(lightColor);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, _isCosmicPhase ? BlendState.AlphaBlend : BlendState.Additive, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone);
            if (_isCosmicPhase)
            {
                DrawCosmicTrail();
            }
            else
            {
                DrawTrailLayer(0.15f, 1.0f);
                DrawTrailLayer(0.6f, 1.15f);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        private void DrawSword(Color lightColor)
        {
            Texture2D? texture = SwordOfTheCosmos.FrameTexture?.GetCurrentFrame();
            if (texture is null)
            {
                return;
            }
            Vector2 origin = new(texture.Width / 2f, texture.Height - 4f);
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float baseLength = texture.Height - 4f;
            float drawRot = _drawVector2.ToRotation() + MathHelper.PiOver2;
            float lengthScale = _drawVector2.Length() / baseLength;
            if (lengthScale < 0.01f || float.IsNaN(lengthScale))
            {
                lengthScale = 0.01f;
            }
            Vector2 drawScale = new(Projectile.scale, lengthScale);
            Effect effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/CosmicShader", AssetRequestMode.ImmediateLoad).Value;
            effect.Parameters["uScreenOffset"].SetValue(pos * 6f);
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
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(texture, pos, null, Projectile.GetAlpha(lightColor), drawRot, origin, drawScale, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(SwordOfTheCosmos.FrameTexture1?.GetCurrentFrame(), pos, null, Projectile.GetAlpha(lightColor), drawRot, origin, drawScale, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(SwordOfTheCosmos.FrameTexture2?.GetCurrentFrame(), pos + (new Vector2(58, 94) * drawScale).RotatedBy(drawRot), null, Projectile.GetAlpha(lightColor), drawRot, origin, drawScale, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(SwordOfTheCosmos.FrameTexture3?.GetCurrentFrame(), pos + (new Vector2(62, 120) * drawScale).RotatedBy(drawRot), null, Projectile.GetAlpha(lightColor), drawRot, origin, drawScale, SpriteEffects.None, 0f);
        }
        private List<Vector2> GetSmoothTrailTips(int segmentsPerEdge = 4)
        {
            List<Vector2> tips = [.. from t in _trailVector2s where t != Vector2.Zero select Projectile.Center + t * Projectile.scale];
            if (tips.Count < 2)
            {
                return tips;
            }
            List<Vector2> smooth = [];
            for (int i = 0; i < tips.Count - 1; i++)
            {
                Vector2 p0 = i > 0 ? tips[i - 1] : tips[i];
                Vector2 p1 = tips[i];
                Vector2 p2 = tips[i + 1];
                Vector2 p3 = i < tips.Count - 2 ? tips[i + 2] : tips[i + 1];
                smooth.Add(p1);
                for (int j = 1; j <= segmentsPerEdge; j++)
                {
                    float t = j / (float)(segmentsPerEdge + 1);
                    float t2 = t * t;
                    float t3 = t2 * t;
                    smooth.Add(p0 * (-0.5f * t + t2 - 0.5f * t3) + p1 * (1f - 2.5f * t2 + 1.5f * t3) + p2 * (0.5f * t + 2f * t2 - 1.5f * t3) + p3 * (-0.5f * t2 + 0.5f * t3));
                }
            }
            smooth.Add(tips[tips.Count - 1]);
            return smooth;
        }
        private void DrawTrailLayer(float rootFactor, float alphaMul)
        {
            List<Vector2> smoothTips = GetSmoothTrailTips(segmentsPerEdge: 6);
            if (smoothTips.Count < 2)
            {
                return;
            }
            List<TrailVertex> vertices = [];
            int total = smoothTips.Count;
            for (int i = 0; i < total; i++)
            {
                float factor = 1f - (float)i / (total - 1);
                float alpha = factor <= 0.5f
                    ? MathHelper.Lerp(0f, 0.5f, factor * 2f)
                    : MathHelper.Lerp(0.5f, 0.7f, (factor - 0.5f) * 2f);
                alpha *= alphaMul * 1.2f;
                alpha *= MathHelper.Min(300f / (Math.Max(i, 1) * Math.Max(i, 1)) * (2f / Math.Max(_timer - 17, 1)), 1f);
                Vector2 tipPos = smoothTips[i];
                Vector2 dir = tipPos - Projectile.Center;
                Vector2 rootPos = Projectile.Center + dir * rootFactor;
                Color swordColor = SampleSwordColor(factor);
                byte r = (byte)MathHelper.Clamp(swordColor.R * alpha, 0, 255);
                byte g = (byte)MathHelper.Clamp(swordColor.G * alpha, 0, 255);
                byte b = (byte)MathHelper.Clamp(swordColor.B * alpha, 0, 255);
                byte alphaByte = (byte)MathHelper.Clamp(alpha * 255, 0, 255);
                Color vertexColor = new(r, g, b, alphaByte);
                vertices.Add(new TrailVertex(rootPos, new Vector3(factor, 1f, alpha), vertexColor));
                vertices.Add(new TrailVertex(tipPos, new Vector3(factor, 0f, alpha), vertexColor));
            }
            if (vertices.Count < 3)
            {
                return;
            }
            Matrix ortho = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, 0f, 1f);
            Matrix view = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0f)) * Main.GameViewMatrix.ZoomMatrix;
            Effect effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/KnifeLight", AssetRequestMode.ImmediateLoad).Value;
            effect.Parameters["uTransform"].SetValue(view * ortho);
            effect.Parameters["tex0"].SetValue(ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/KnifeLight").Value);
            effect.CurrentTechnique.Passes["Trail"].Apply();
            Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, [.. vertices], 0, vertices.Count - 2);
        }
        private void DrawCosmicTrail()
        {
            List<Vector2> smoothTips = GetSmoothTrailTips();
            if (smoothTips.Count < 2)
            {
                return;
            }
            List<TrailVertex> vertices = [];
            int total = smoothTips.Count;
            for (int i = 0; i < total; i++)
            {
                float factor = 1f - (float)i / (total - 1);
                float alpha = factor <= 0.5f ? MathHelper.Lerp(0f, 0.5f, factor * 2f) : MathHelper.Lerp(0.5f, 0.7f, (factor - 0.5f) * 2f);
                alpha *= 1.2f * MathHelper.Min(300f / (Math.Max(i, 1) * Math.Max(i, 1)) * (2f / Math.Max(_timer - 24, 1)), 1f);
                Vector2 tipPos = smoothTips[i];
                Vector2 dir = tipPos - Projectile.Center;
                Vector2 rootPos = Projectile.Center + dir * 0.15f;
                byte alphaByte = (byte)MathHelper.Clamp(alpha * 255, 0, 255);
                Color vertexColor = new(255, 255, 255, alphaByte);
                vertices.Add(new TrailVertex(rootPos, new Vector3(factor, 0.8f, alpha), vertexColor));
                vertices.Add(new TrailVertex(tipPos, new Vector3(factor, 0f, alpha), vertexColor));
            }
            if (vertices.Count < 3)
            {
                return;
            }
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            gd.SetVertexBuffer(null);
            gd.Indices = null;
            gd.BlendState = BlendState.AlphaBlend;
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = RasterizerState.CullNone;
            Matrix ortho = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, 0f, 1f);
            Matrix view = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0f)) * Main.GameViewMatrix.ZoomMatrix;
            Effect effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/CosmicTrail", AssetRequestMode.ImmediateLoad).Value;
            effect.Parameters["uTransform"].SetValue(view * ortho);
            effect.Parameters["uScreenSize"].SetValue(new Vector2(Main.screenWidth, Main.screenHeight));
            float zoom = Main.GameViewMatrix.ZoomMatrix.M11;
            Vector2 screenCenter = new Vector2(Main.screenWidth, Main.screenHeight) / 2f;
            Vector2 screenCenterPos = (_cosmicCenter - Main.screenPosition) * zoom + screenCenter * (1f - zoom);
            float? screenRadius = WarpSystem.CosmicRT?.Width / 2f;
            effect.Parameters["uCosmicCenterScreen"].SetValue(screenCenterPos);
            effect.Parameters["uCosmicRTRadiusPixels"].SetValue(screenRadius ?? 0f);
            effect.Parameters["tex0"].SetValue(ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/KnifeLight").Value);
            effect.Parameters["tex1"].SetValue(WarpSystem.CosmicRT);
            effect.CurrentTechnique.Passes["CosmicTrail"].Apply();
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, [.. vertices], 0, vertices.Count - 2);
        }
        public void DrawWarp()
        {
            if (_trailVector2s is null)
            {
                return;
            }
            if (_attackType is < 0 or >= 2 || _trailVector2s.All(v => v == Vector2.Zero))
            {
                return;
            }
            float fade = 1f;
            int prepare = PrepareTimes[_attackType];
            int swing = SwingTimes[_attackType];
            int unwind = UnwindTimes[_attackType];
            int totalTime = prepare + swing + unwind;
            if (_timer >= prepare + swing && _timer < totalTime)
            {
                fade = MathHelper.Clamp((totalTime - _timer) / (float)unwind, 0f, 1f);
            }
            else if (_timer >= totalTime)
            {
                fade = 0f;
            }
            List<Vector2> originalTips = [.. _trailVector2s.Where(v => v != Vector2.Zero)];
            if (originalTips.Count < 2)
            {
                return;
            }
            List<(Vector2 tip, float strength)> smoothPoints = [];
            for (int i = 0; i < originalTips.Count - 1; i++)
            {
                Vector2 p0 = originalTips[i];
                Vector2 p1 = originalTips[i + 1];
                float s0 = 1f - (float)i / originalTips.Count;
                float s1 = 1f - (float)(i + 1) / originalTips.Count;
                s0 *= fade;
                s1 *= fade;
                smoothPoints.Add((Projectile.Center + p0 * Projectile.scale * 1.1f, s0));
                for (int j = 1; j <= 4; j++)
                {
                    float t = j / (float)(4 + 1);
                    Vector2 midTip = Vector2.Lerp(p0, p1, t);
                    float midStrength = MathHelper.Lerp(s0, s1, t);
                    smoothPoints.Add((Projectile.Center + midTip * Projectile.scale * 1.1f, midStrength));
                }
            }
            smoothPoints.Add((Projectile.Center + originalTips[^1] * Projectile.scale * 1.1f,
                (1f - (float)(originalTips.Count - 1) / originalTips.Count) * fade));
            if (smoothPoints.Count < 3)
            {
                return;
            }
            List<TrailVertex> vertices = [];
            float? lastAngle = null;
            foreach ((Vector2 tip, float strength) in smoothPoints)
            {
                Vector2 rootPos = Projectile.Center;
                Vector2 dir = tip - rootPos;
                float angle = dir.ToRotation() + MathHelper.PiOver2;
                if (lastAngle.HasValue)
                {
                    while (angle - lastAngle.Value > MathHelper.Pi)
                    {
                        angle -= MathHelper.TwoPi;
                    }
                    while (angle - lastAngle.Value < -MathHelper.Pi)
                    {
                        angle += MathHelper.TwoPi;
                    }
                }
                lastAngle = angle;
                float cosA = MathF.Cos(angle);
                float sinA = MathF.Sin(angle);
                byte r = (byte)MathHelper.Clamp((cosA * 0.5f + 0.5f) * 255, 0, 255);
                byte g = (byte)MathHelper.Clamp((sinA * 0.5f + 0.5f) * 255, 0, 255);
                byte b = (byte)(MathHelper.Clamp(strength * 2f, 0f, 1f) * 255);
                vertices.Add(new TrailVertex(rootPos, new Vector3(strength, 1f, 1f), new Color(r, g, b, 255)));
                vertices.Add(new TrailVertex(tip, new Vector3(strength, 0f, 1f), new Color(r, g, b, 255)));
            }
            if (vertices.Count < 3)
            {
                return;
            }
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            gd.RasterizerState = RasterizerState.CullNone;
            Matrix ortho = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, 0f, 1f);
            Matrix view = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0f)) * Main.GameViewMatrix.ZoomMatrix;
            Effect? effect = WarpSystem.KExEffect;
            effect?.Parameters["uTransform"].SetValue(view * ortho);
            Texture2D extraTex = ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/Warp", AssetRequestMode.ImmediateLoad).Value;
            gd.Textures[0] = extraTex;
            gd.SamplerStates[0] = SamplerState.PointWrap;
            effect?.CurrentTechnique.Passes[0].Apply();
            gd.DrawUserPrimitives(PrimitiveType.TriangleStrip, [.. vertices], 0, vertices.Count - 2);
            gd.RasterizerState = RasterizerState.CullCounterClockwise;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_attackType);
            writer.Write(_timer);
            writer.Write(_isAttacking);
            writer.Write(Projectile.spriteDirection);
            writer.Write(_isCosmicPhase);
            writer.Write(Projectile.Center.X);
            writer.Write(Projectile.Center.Y);
            writer.Write(Projectile.rotation);
            writer.Write(_drawVector2.X);
            writer.Write(_drawVector2.Y);
            writer.Write(_vertexVector2.X);
            writer.Write(_vertexVector2.Y);
            writer.Write(_cosmicCenter.X);
            writer.Write(_cosmicCenter.Y);
            writer.Write(_cosmicRadius);
            writer.Write((byte)(_trailVector2s?.Length ?? 0));
            for (int i = 0; i < _trailVector2s?.Length; i++)
            {
                Vector2 v = _trailVector2s?[i] ?? Vector2.Zero;
                writer.Write(v.X);
                writer.Write(v.Y);
            }
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _attackType = reader.ReadInt32();
            _timer = reader.ReadInt32();
            _isAttacking = reader.ReadBoolean();
            Projectile.spriteDirection = reader.ReadInt32();
            _isCosmicPhase = reader.ReadBoolean();
            Projectile.Center = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            Projectile.rotation = reader.ReadSingle();
            _drawVector2 = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _vertexVector2 = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _cosmicCenter = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _cosmicRadius = reader.ReadSingle();
            int trailLen = reader.ReadByte();
            _trailVector2s = new Vector2[trailLen];
            for (int i = 0; i < trailLen; i++)
            {
                _trailVector2s[i] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }
        }
        private Color SampleSwordColor(float factor)
        {
            Color lightBlue = new(0, 30, 150);
            Color deepBlue = Color.SkyBlue;
            float t = MathHelper.Clamp(factor, 0f, 1f);
            t = t * t * (3f - 2f * t);
            return Color.Lerp(deepBlue, lightBlue, t);
        }
        public struct TrailVertex : IVertexType
        {
            private static readonly VertexDeclaration _vertexDeclaration = new(
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0),
                new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0)
            );
            public Vector2 Position;
            public Color Color;
            public Vector3 TexCoord;
            public TrailVertex(Vector2 position, Vector3 texCoord, Color color)
            {
                Position = position;
                TexCoord = texCoord;
                Color = color;
            }
            public VertexDeclaration VertexDeclaration => _vertexDeclaration;
        }
    }
}