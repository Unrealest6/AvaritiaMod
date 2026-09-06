namespace AvaritiaMod.Content.Projectiles
{
    public class AvaritiaMarkProj : ModProjectile
    {
        public override string Texture => "AvaritiaMod/Content/Projectiles/SwordOfTheCosmosMark";
        private static FrameTexture? HeavenArrowMarkFrame { get; set; }
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
            if (Projectile.ai[1] > 0)
            {
                if (Projectile.owner == Main.myPlayer)
                {
                    Projectile.netUpdate = true;
                    Player player = Main.player[Projectile.owner];
                    NPC? npc = Main.npc.Where(npc => npc.active && npc is { immortal: false, friendly: false, lifeMax: > 1 } && npc.DistanceSQ(player.Center) < 1048576).GetRecent(Projectile.Center, 768);
                    Projectile.Center = npc?.Center ?? Projectile.Center;
                }
            }
        }
        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[1] > 0)
            {
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
            if (texture is not null)
            {
                DrawRainbowRing(texture, Projectile.Center, 4f);
            }
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
            float width = texture.Width;
            float height = texture.Height;
            Vector2 half = new(width / 2f, height / 2f);
            RainbowVertex[] vertices =
            [
                new(center + new Vector2(-half.X, -half.Y), new Vector2(0, 0)),
                new(center + new Vector2(half.X, -half.Y), new Vector2(1, 0)),
                new(center + new Vector2(-half.X, half.Y), new Vector2(0, 1)),
                new(center + new Vector2(half.X, half.Y), new Vector2(1, 1))
            ];
            short[] indices = [0, 1, 2, 2, 1, 3];
            Matrix ortho = Matrix.CreateOrthographicOffCenter(0f, Main.screenWidth, Main.screenHeight, 0f, 0f, 1f);
            Matrix view = Matrix.CreateTranslation(new Vector3(-Main.screenPosition.X, -Main.screenPosition.Y, 0f)) * Main.GameViewMatrix.ZoomMatrix;
            Matrix transform = view * ortho;
            Effect effect = ModContent.Request<Effect>("AvaritiaMod/Assets/Effects/RainbowCycle", AssetRequestMode.ImmediateLoad).Value;
            effect.CurrentTechnique = effect.Techniques["RainbowCycle"];
            effect.Parameters["MatrixTransform"].SetValue(transform);
            effect.Parameters["tex0"].SetValue(texture);
            effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["CycleSpeed"].SetValue(cycleSpeed);
            GraphicsDevice gd = Main.graphics.GraphicsDevice;
            gd.BlendState = BlendState.AlphaBlend;
            gd.DepthStencilState = DepthStencilState.None;
            gd.RasterizerState = RasterizerState.CullNone;
            effect.CurrentTechnique.Passes[0].Apply();
            gd.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
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