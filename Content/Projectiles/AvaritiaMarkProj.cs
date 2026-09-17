namespace AvaritiaMod.Content.Projectiles
{
    public class AvaritiaMarkProj : ModProjectile
    {
        public override string Texture => "AvaritiaMod/Content/Projectiles/SwordOfTheCosmosMark";
        private static FrameTexture? HeavenArrowMarkFrame { get; set; }
        /// <summary>彩虹环 Effect 与其技术/通道/参数句柄（惰性加载后复用）。</summary>
        private static Effect? _rainbowEffect;
        private static EffectTechnique? _rainbowTechnique;
        private static EffectPass? _rainbowPass;
        private static EffectParameter? _matrixParam;
        private static EffectParameter? _textureParam;
        private static EffectParameter? _timeParam;
        private static EffectParameter? _cycleSpeedParam;
        private static bool _effectRequested;
        /// <summary>复用的顶点/索引缓冲，避免每帧分配。</summary>
        private static readonly RainbowVertex[] RingVertices = new RainbowVertex[4];
        private static readonly short[] RingIndices = [0, 1, 2, 2, 1, 3];
        public override void SetStaticDefaults()
        {
            HeavenArrowMarkFrame = FrameTextureSystem.Register("HeavenArrowMark", "AvaritiaMod/Content/Projectiles/HeavenArrowMark", 9, 6);
        }
        public override void SetDefaults()
        {
            Projectile.width = 0;
            Projectile.height = 0;
            Projectile.timeLeft = 1;
            Projectile.aiStyle = -1;
        }
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.timeLeft = (int)Projectile.ai[0];
            Projectile.netUpdate = true;
        }
        public override void AI()
        {
            if (Projectile.ai[1] > 0 && Projectile.owner == Main.myPlayer)
            {
                //原先这里每 tick 都 netUpdate = true。追踪目标完全由“已同步的 NPC 位置 + 本地位置”推导，
                //各端跑同一套 AI 会得到同一结果，重复发包只会造成同步流量浪费。
                Player player = Main.player[Projectile.owner];
                NPC? npc = Main.npc.Where(npc => npc.active && npc is { immortal: false, friendly: false, lifeMax: > 1 } && npc.DistanceSQ(player.Center) < 1048576).GetRecent(Projectile.Center, 768);
                Projectile.Center = npc?.Center ?? Projectile.Center;
            }
        }
        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[1] > 0 && Projectile.owner == Main.myPlayer)
            {
                //传送与生成无尽剑必须由拥有者执行：每个客户端都跑一遍会让本地玩家被“其它客户端算出来的位置”反复传送，
                //服务端也会凭空多出无尽剑。拥有者传送后由原版传送包同步给其它端。
                Player player = Main.player[Projectile.owner];
                NPC? npc = Main.npc.Where(npc => npc.active && npc is { immortal: false, friendly: false, lifeMax: > 1 } && npc.DistanceSQ(player.Center) < 1048576).GetRecent(Projectile.Center, 768);
                Projectile.Center = npc?.Center ?? Projectile.Center;
                player.immune = true;
                player.immuneTime = 30;
                player.moveSpeed = 0;
                Vector2 position = Projectile.Center + new Vector2((player.direction == -1 ? npc?.Size.X / 2f + player.Size.X / 2f : -npc?.Size.X / 2f - player.Size.X / 2f) ?? 0f, 0f) - player.Size / 2f;
                player.Teleport(position, TeleportationStyleID.ShellphoneSpawn);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendData(MessageID.TeleportEntity, number2: player.whoAmI, number3: position.X, number4: position.Y, number5: TeleportationStyleID.ShellphoneSpawn);
                }
                if (player.ownedProjectileCounts[ModContent.ProjectileType<SwordOfTheCosmosProj>()] < 1)
                {
                    Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), player.MountedCenter, Vector2.Zero,
                        ModContent.ProjectileType<SwordOfTheCosmosProj>(), 1, 6, player.whoAmI, Main.rand.Next(3));
                }
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D? texture = Projectile.ai[1] == 0 ? HeavenArrowMarkFrame?.GetCurrentFrame() : TextureAssets.Projectile[Type].Value;
            if (texture is null)
            {
                //没有贴图时交回原版绘制：直接 return false 会让弹幕彻底不可见。
                return true;
            }
            DrawRainbowRing(texture, Projectile.Center, 4f);
            return false;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.timeLeft);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.timeLeft = reader.ReadInt32();
        }
        private void DrawRainbowRing(Texture2D texture, Vector2 center, float cycleSpeed)
        {
            EnsureRainbowEffect();
            if (_rainbowTechnique is null || _rainbowPass is null)
            {
                return;
            }
            float width = texture.Width;
            float height = texture.Height;
            Vector2 half = new(width / 2f, height / 2f);
            //复用静态顶点缓冲：原实现每次绘制都 new 两个数组（每帧 6 次分配）。
            RingVertices[0] = new RainbowVertex(center + new Vector2(-half.X, -half.Y), new Vector2(0, 0));
            RingVertices[1] = new RainbowVertex(center + new Vector2(half.X, -half.Y), new Vector2(1, 0));
            RingVertices[2] = new RainbowVertex(center + new Vector2(-half.X, half.Y), new Vector2(0, 1));
            RingVertices[3] = new RainbowVertex(center + new Vector2(half.X, half.Y), new Vector2(1, 1));
            Matrix ortho = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, 0f, 1f);
            Matrix view = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0f)) * Main.GameViewMatrix.ZoomMatrix;
            Matrix transform = view * ortho;
            _rainbowEffect!.CurrentTechnique = _rainbowTechnique;
            _matrixParam?.SetValue(transform);
            _textureParam?.SetValue(texture);
            _timeParam?.SetValue(Main.GlobalTimeWrappedHourly);
            _cycleSpeedParam?.SetValue(cycleSpeed);
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            //直接改 GraphicsDevice 状态必须还原，否则会影响同一帧后续的绘制。
            BlendState previousBlend = gd.BlendState;
            DepthStencilState previousDepth = gd.DepthStencilState;
            RasterizerState previousRasterizer = gd.RasterizerState;
            gd.BlendState = BlendState.AlphaBlend;
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = RasterizerState.CullNone;
            _rainbowPass.Apply();
            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, RingVertices, 0, 4, RingIndices, 0, 2);
            gd.BlendState = previousBlend;
            gd.DepthStencilState = previousDepth;
            gd.RasterizerState = previousRasterizer;
        }
        /// <summary>
        /// 惰性加载彩虹环 Effect 并缓存技术/通道/参数句柄。
        /// <para>原实现每次绘制都做一次 <c>ModContent.Request&lt;Effect&gt;</c> 加多次名称查找。</para>
        /// </summary>
        private static void EnsureRainbowEffect()
        {
            if (_effectRequested || Main.dedServ)
            {
                return;
            }
            _effectRequested = true;
            Effect effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/RainbowCycle", AssetRequestMode.ImmediateLoad).Value;
            _rainbowEffect = effect;
            _rainbowTechnique = effect.Techniques["RainbowCycle"];
            _rainbowPass = _rainbowTechnique?.Passes[0];
            _matrixParam = effect.Parameters["MatrixTransform"];
            _textureParam = effect.Parameters["tex0"];
            _timeParam = effect.Parameters["Time"];
            _cycleSpeedParam = effect.Parameters["CycleSpeed"];
        }
        public struct RainbowVertex : IVertexType
        {
            public Vector2 Position;
            public Vector2 TexCoord;
            private static readonly VertexDeclaration VertexDeclaration = new(
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
            );
            public RainbowVertex(Vector2 position, Vector2 texCoord)
            {
                Position = position;
                TexCoord = texCoord;
            }
            VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
        }
    }
}