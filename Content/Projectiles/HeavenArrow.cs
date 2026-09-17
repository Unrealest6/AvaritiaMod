namespace AvaritiaMod.Content.Projectiles
{
    public sealed class HeavenArrow : ModProjectile
    {
        internal bool IsTrack { get; set; }
        internal Vector2? MarkCenter { get; set; }
        internal int? SerialNum { get; set; }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.arrow = true;
            Projectile.timeLeft = 300;
            Projectile.aiStyle = ProjAIStyleID.Arrow;
            Projectile.scale = Main.rand.NextFloat(1.25f, 2f);
        }
        public override void AI()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            //原先这里每 tick 无条件 netUpdate = true（等于每帧一个完整的弹幕同步包）。
            //现在只由拥有者发包，而且只在“本地随机的速度需要同步”或“螺旋阶段定期纠偏”时下发。
            bool ownerIsLocal = Projectile.owner == Main.myPlayer;
            Player player = Main.player[Projectile.owner];
            if (MarkCenter is not null)
            {
                Projectile.scale = 1.25f;
                if (Projectile.timeLeft > 300)
                {
                    Vector2 radiusVector = Projectile.Center - MarkCenter.Value;
                    float currentRadius = radiusVector.Length();
                    Vector2 tangent = new(-radiusVector.Y, radiusVector.X);
                    tangent.Normalize();
                    float speed = currentRadius * 0.005f;
                    Projectile.velocity = tangent * speed;
                    Projectile.rotation = (MarkCenter.Value - Projectile.Center).ToRotation() + MathHelper.PiOver2;
                }
                else
                {
                    Projectile.velocity = Projectile.timeLeft is <= 300 and > 284
                        ? Vector2.Lerp(Projectile.velocity, (MarkCenter.Value - Projectile.Center) / 16f, 0.1f)
                        : Projectile.oldVelocity;
                }
                Projectile.position += Projectile.velocity;
                if (ownerIsLocal && Projectile.timeLeft % 3 == 0)
                {
                    //螺旋轨迹由已同步的 MarkCenter 与位置推导，其它客户端可以自己算，
                    //因此只需要定期纠偏，不必每 tick 同步。
                    Projectile.netUpdate = true;
                }
                if (Projectile.timeLeft > 300 || !Main.rand.NextBool(2))
                {
                    return;
                }
                Vector2 velocity = Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.5f, 0.5f);
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueFlare, velocity.X, velocity.Y, 0
                    , Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat()), Main.rand.NextFloat(1.5f, 3f));
                dust.noGravity = true;
                return;
            }
            if (IsTrack && Main.npc.Where(npc1 => npc1.active && npc1 is { immortal: false, friendly: false, lifeMax: > 1 } && npc1.DistanceSQ(player.Center) < 1048576).GetRecent(Projectile, 512) is { } npc)
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, (npc.Center - Projectile.Center) / 16f, 0.15f);
            }
            bool randomSpinUp = Projectile.timeLeft == 301;
            Projectile.velocity = Projectile.timeLeft switch
            {
                301 => new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(32f, 48f)),
                > 301 => new Vector2(0, 0.0000000001f),
                _ => Projectile.velocity
            };
            if (ownerIsLocal && randomSpinUp)
            {
                //这一帧的速度来自 Main.rand，各端算出来不一样，必须由拥有者同步一次。
                Projectile.netUpdate = true;
            }
            Projectile.position += Projectile.velocity;
            if (Main.rand.NextBool(2))
            {
                Vector2 velocity = Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.5f, 0.5f);
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueFlare, velocity.X, velocity.Y
                    , newColor: Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat()), Scale: Main.rand.NextFloat(1.5f, 3f));
                dust.noGravity = true;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return false;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone
                , null, Main.GameViewMatrix.TransformationMatrix);
            float a = 1f;
            if (Projectile.timeLeft >= 306 + (SerialNum ?? 0) * 3 && Projectile.timeLeft < 336 + (SerialNum ?? 0) * 3)
            {
                a = a - (Projectile.timeLeft - (306f + (SerialNum ?? 0) * 3)) / 30f;
            }
            else if (Projectile.timeLeft >= 336 + (SerialNum ?? 0) * 3)
            {
                a = 0f;
            }
            Projectile.light = a;
            lightColor *= a;
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int trailLength = Projectile.oldPos.Length;
            if (Projectile.timeLeft <= 300 && MarkCenter is null || Projectile.timeLeft <= 292)
            {
                for (int i = 1; i < trailLength; i++)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                    {
                        continue;
                    }
                    float alpha = 1f - (float)i / trailLength;
                    float scale = Projectile.scale * (1f - 0.05f * i);
                    Color color = Color.Lerp(Color.White, Color.Cyan, alpha / 2f) * alpha * a;
                    Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
                    Main.spriteBatch.Draw(texture, drawPos, null, color, Projectile.oldRot[i], new Vector2(texture.Width / 2f, texture.Height * 0.1f)
                        , scale, SpriteEffects.None, 0);
                }
            }
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation,
                new Vector2(texture.Width / 2f, texture.Height * 0.1f), Projectile.scale, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone
                , null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Projectile.timeLeft > 301 ? false : base.Colliding(projHitbox, targetHitbox);
        public override void OnKill(int timeLeft)
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return;
            }
            for (int i = 0; i < 15; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueCrystalShard, Main.rand.NextFloat(-2f, 2f)
                    , Main.rand.NextFloat(-2f, 2f), newColor: Color.Lerp(Color.White, Color.Cyan, Main.rand.NextFloat()), Scale: Main.rand.NextFloat(1f, 1.5f));
                dust.noGravity = true;
            }
            if (!IsTrack)
            {
                return;
            }
            //箭雨只能由拥有者生成：每个客户端都执行会各自生成 10 发本地弹幕（同屏重复，且服务端拿到多份生成请求）。
            if (Projectile.owner != Main.myPlayer)
            {
                return;
            }
            for (byte i = 0; i < 10; i++)
            {
                Projectile projectile = Projectile.NewProjectileDirect(Projectile.GetSource_Death(), Projectile.position - new Vector2(Main.rand.NextFloat(-128f, 128f), 720f),
                    new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(32f, 48f)), Type, Projectile.damage, Projectile.knockBack, Projectile.owner);
                projectile.timeLeft += Main.rand.Next(0, 33);
                //同步“被随机加长的存活时间”：原实现写的是 Projectile.netUpdate（正在死亡的父弹幕），等于什么都没同步。
                projectile.netUpdate = true;
            }
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(MarkCenter.HasValue);
            if (MarkCenter.HasValue)
            {
                writer.Write(MarkCenter.Value.X);
                writer.Write(MarkCenter.Value.Y);
            }
            writer.Write(SerialNum ?? -1);
            writer.Write(IsTrack);
            writer.Write(Projectile.timeLeft);
            writer.Write(Projectile.penetrate);
            writer.Write(Projectile.tileCollide);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            bool hasMark = reader.ReadBoolean();
            MarkCenter = hasMark ? new Vector2(reader.ReadSingle(), reader.ReadSingle()) : null;
            int serial = reader.ReadInt32();
            SerialNum = serial == -1 ? null : serial;
            IsTrack = reader.ReadBoolean();
            Projectile.timeLeft = reader.ReadInt32();
            Projectile.penetrate = reader.ReadInt32();
            Projectile.tileCollide = reader.ReadBoolean();
        }
    }
}