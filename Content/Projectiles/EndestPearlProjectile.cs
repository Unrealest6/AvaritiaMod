namespace AvaritiaMod.Content.Projectiles
{
    public sealed class EndestPearlProjectile : ModProjectile
    {
        private enum Phase
        {
            Flying,
            Void,
            Exploding
        }
        private Phase _phase = Phase.Flying;
        private uint _startFrame;
        private float _currentRadius;
        private float _nomRadius;
        private uint _age;
        private static Texture2D? _voidTexture;
        private Color drawColor;
        private static double GetVoidScale(double age)
        {
            double life = age / 558;
            double curve;
            if (life < 0.95)
            {
                double t = 1.0 - (0.95 - life) / 0.95;
                curve = 0.005 + Ease(t) * 0.995;
            }
            else
            {
                double t = 1.0 - (life - 0.95) / (1.0 - 0.95);
                curve = Ease(t);
            }
            return 10.0 * curve;
        }
        private static double Ease(double t)
        {
            double adjusted = t - 1.0;
            return Math.Sqrt(1.0 - adjusted * adjusted);
        }
        private static float GaussianRandom()
        {
            float u1 = 1f - Main.rand.NextFloat();
            float u2 = 1f - Main.rand.NextFloat();
            return (float)(Math.Sqrt(-2f * Math.Log(u1)) * Math.Cos(2f * Math.PI * u2));
        }
        public override void SetStaticDefaults()
        {
            if (Main.dedServ)
            {
                return;
            }
            _voidTexture = ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/Voidhalo", AssetRequestMode.ImmediateLoad).Value;
        }
        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.DamageType = DamageClass.Default;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 900;
            Projectile.tileCollide = true;
            Projectile.aiStyle = -1;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (_phase == Phase.Flying)
            {
                EnterVoid();
            }
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (_phase != Phase.Flying || !projHitbox.Intersects(targetHitbox))
            {
                return false;
            }
            EnterVoid();
            return true;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
            behindNPCs.Add(index);
            behindProjectiles.Add(index);
            overPlayers.Add(index);
        }
        public override void AI()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            Projectile.netUpdate = true;
            if (_phase == Phase.Flying)
            {
                return;
            }
            _age = Projectile.owner == Main.myPlayer ? Main.GameUpdateCount - _startFrame : _age;
            if (_age >= 558)
            {
                Explode();
                Projectile.Kill();
                return;
            }
            if (Projectile.owner == Main.myPlayer)
            {
                double scaleUnits = GetVoidScale(_age);
                _currentRadius = (float)(scaleUnits * 18f);
                _nomRadius = _currentRadius;
            }
            if (_age == 0)
            {
                SoundEngine.PlaySound(SoundID.Roar, Projectile.Center);
            }
            ApplyVoidPhysics(_age);
            SpawnAmbientParticles(_age);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return false;
            }
            if (_phase != Phase.Void)
            {
                return _phase != Phase.Void && base.PreDraw(ref lightColor);
            }
            DrawBlackhole(Main.spriteBatch);
            return false;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)_phase);
            writer.Write(Projectile.timeLeft);
            writer.WriteRGB(drawColor);
            writer.Write(_currentRadius);
            writer.Write(_nomRadius);
            writer.Write(_age);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _phase = (Phase)reader.ReadByte();
            Projectile.timeLeft = reader.ReadInt32();
            drawColor = reader.ReadRGB();
            _currentRadius = reader.ReadSingle();
            _nomRadius = reader.ReadSingle();
            _age = reader.ReadUInt32();
        }
        internal void DrawBlackhole(SpriteBatch sb)
        {
            if (_phase == Phase.Flying)
            {
                return;
            }
            _age = Projectile.owner == Main.myPlayer ? Main.GameUpdateCount - _startFrame : _age;
            if (_age >= 558)
            {
                return;
            }
            Vector2 screenPos = Projectile.Center - Main.screenPosition;
            float damageRadius = _currentRadius * 0.95f;
            float scale = damageRadius / 64f;
            double life = _age / 558d;
            if (Projectile.owner == Main.myPlayer)
            {
                if (life < 0.95)
                {
                    drawColor = Color.Black;
                    if (life < 0.01)
                    {
                        drawColor = Color.White;
                    }
                }
                else
                {
                    double collapseProgress = (life - 0.95) / 0.005;
                    drawColor = Color.Lerp(Color.Black, Color.White, (float)collapseProgress);
                }
            }
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            sb.Draw(_voidTexture, screenPos, null, drawColor, 0f, _voidTexture.Size() / 2f, scale, SpriteEffects.None, 0f);
            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        private void ApplyVoidPhysics(uint age)
        {
            float suckRange = Math.Min(_currentRadius * 4f, 320f);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float dist = Vector2.Distance(Projectile.Center, npc.Center);
                if (dist > suckRange || dist < 8f)
                {
                    continue;
                }
                Vector2 dir = Projectile.Center - npc.Center;
                double t = dist / suckRange;
                double strength = (1.0 - t) * (1.0 - t);
                double power = 0.003 * _currentRadius;
                Vector2 targetVel = dir.SafeNormalize(Vector2.Zero) * (float)(strength * power);
                npc.velocity += targetVel;
                if (npc.velocity.Length() > 8f)
                {
                    npc.velocity = npc.velocity.SafeNormalize(Vector2.Zero) * 8f;
                }
                if (dist < 16f)
                {
                    npc.velocity *= 0.95f;
                }
                npc.netUpdate = true;
            }
            if (age % 30 != 0)
            {
                return;
            }
            float breakRadius = _nomRadius * 0.8f;
            int radiusBlocks = (int)(breakRadius / 16f) + 1;
            int cx = (int)(Projectile.Center.X / 16f);
            int cy = (int)(Projectile.Center.Y / 16f);
            for (int x = cx - radiusBlocks; x <= cx + radiusBlocks; x++)
            {
                for (int y = cy - radiusBlocks; y <= cy + radiusBlocks; y++)
                {
                    if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
                    {
                        continue;
                    }
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile)
                    {
                        continue;
                    }
                    Vector2 blockCenter = new(x * 16 + 8, y * 16 + 8);
                    float dist = Vector2.Distance(Projectile.Center, blockCenter);
                    if (dist > breakRadius)
                    {
                        continue;
                    }
                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        ModPacket packet = Mod.GetPacket();
                        packet.Write((byte)AvaritiaMod.SyncMessageType.ServerKillTile);
                        packet.Write(x);
                        packet.Write(y);
                        packet.Send();
                    }
                    WorldGen.KillWall(x, y);
                    WorldGen.KillTile(x, y);
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendTileSquare(-1, x, y, 1);
                    }
                }
            }
            float damageRange = _nomRadius * 0.95f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float dist = Vector2.Distance(Projectile.Center, npc.Center);
                if (!(dist <= damageRange))
                {
                    continue;
                }
                NPC.HitInfo info = new()
                {
                    Damage = 75,
                    DamageType = DamageClass.Default
                };
                npc.StrikeNPC(info);
                NetMessage.SendStrikeNPC(npc, info);
            }
            foreach (Player player in Main.player.Where(player => player is { active: true, dead: false, ghost: false }))
            {
                float dist = Vector2.Distance(Projectile.Center, player.Center);
                if (dist <= damageRange)
                {
                    Player.HurtInfo info = new()
                    {
                        DamageSource = PlayerDeathReason.ByProjectile(player.whoAmI, Projectile.whoAmI),
                        Damage = 75
                    };
                    player.Hurt(info);
                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        ModPacket packet = Mod.GetPacket();
                        packet.Write((byte)AvaritiaMod.SyncMessageType.RequestHurtPlayer);
                        packet.Write(player.whoAmI);
                        packet.Write(Projectile.whoAmI);
                        packet.Write(75);
                        packet.Write(0);
                        packet.Write(false);
                        packet.Send();
                    }
                }
            }
        }
        private void SpawnAmbientParticles(uint age)
        {
            if (_currentRadius < 5f)
            {
                return;
            }
            float size = _currentRadius;
            float angleOffset = age * 0.03f;
            for (int i = 0; i < 10; i++)
            {
                float angle = angleOffset + Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = Main.rand.NextFloat(size * 0.7f, size);
                Vector2 pos = Projectile.Center + new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);
                Vector2 outward = (pos - Projectile.Center).SafeNormalize(Vector2.Zero);
                Vector2 vel = outward * Main.rand.NextFloat(2f, 6f);
                Dust d = Dust.NewDustPerfect(pos, DustID.PortalBoltTrail, vel, 100, Color.Purple, Main.rand.NextFloat(0.8f, 1.5f));
                d.noGravity = true;
            }
            for (int i = 0; i < 3; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float radius = Main.rand.NextFloat(_currentRadius * 0.2f, _currentRadius * 0.5f);
                Vector2 pos = Projectile.Center + new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);
                Vector2 vel = (Projectile.Center - pos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1f, 3f);
                Dust d = Dust.NewDustPerfect(pos, DustID.PortalBoltTrail, vel, 100, Color.Purple, Main.rand.NextFloat(0.6f, 1f));
                d.noGravity = true;
            }
        }
        private void Explode()
        {
            _phase = Phase.Exploding;
            int radiusBlocks = (int)(_nomRadius * 1.2f / 16f) + 1;
            int cx = (int)(Projectile.Center.X / 16f);
            int cy = (int)(Projectile.Center.Y / 16f);
            for (int x = cx - radiusBlocks; x <= cx + radiusBlocks; x++)
            {
                for (int y = cy - radiusBlocks; y <= cy + radiusBlocks; y++)
                {
                    if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
                    {
                        continue;
                    }
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile)
                    {
                        continue;
                    }
                    Vector2 blockCenter = new(x * 16 + 8, y * 16 + 8);
                    float dist = Vector2.Distance(Projectile.Center, blockCenter);
                    if (dist > _nomRadius * 1.2f)
                    {
                        continue;
                    }
                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        ModPacket packet = Mod.GetPacket();
                        packet.Write((byte)AvaritiaMod.SyncMessageType.ServerKillTile);
                        packet.Write(x);
                        packet.Write(y);
                        packet.Send();
                    }
                    WorldGen.KillWall(x, y);
                    WorldGen.KillTile(x, y);
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendTileSquare(-1, x, y, 1);
                    }
                }
            }
        }
        private void EnterVoid()
        {
            _phase = Phase.Void;
            _startFrame = Main.GameUpdateCount;
            Projectile.tileCollide = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = false;
            Projectile.timeLeft = 618;
            SpawnImpactParticles();
            SoundEngine.PlaySound(SoundID.Item117, Projectile.Center);
        }
        private void SpawnImpactParticles()
        {
            for (int i = 0; i < 100; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float speed = Math.Abs(GaussianRandom()) * 4.5f;
                Vector2 vel = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.PortalBoltTrail,
                    vel, 100, Color.Purple, Main.rand.NextFloat(1f, 2f));
                dust.noGravity = true;
            }
        }
    }
}