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
                //追踪目标由已同步的 NPC 位置推导，各端结果一致，无需每 tick 发包。
                Player player = Main.player[Projectile.owner];
                NPC? npc = FindTarget(player);
                Projectile.Center = npc?.Center ?? Projectile.Center;
            }
        }
        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[1] > 0 && Projectile.owner == Main.myPlayer)
            {
                //传送与生成无尽剑只能由拥有者执行，否则各端会互相反复传送、服务端还会凭空多出无尽剑；位置由原版传送包同步。
                Player player = Main.player[Projectile.owner];
                NPC? npc = FindTarget(player);
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
        /// <summary>取离玩家 64 格内、最近的可追踪 NPC（数组版本，避免每 tick 开 LINQ 迭代器）。</summary>
        private NPC? FindTarget(Player player)
            => Main.npc.GetNearest(Projectile.Center,
                npc => npc is { immortal: false, friendly: false, lifeMax: > 1 } && npc.DistanceSQ(player.Center) < 1048576,
                768);
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
            //复用静态顶点缓冲，避免每帧绘制重复分配数组。
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
        /// <summary>惰性加载彩虹环 Effect，并缓存技术与参数句柄，避免每次绘制重复请求与查找。</summary>
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