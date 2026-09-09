namespace AvaritiaMod.Common.Players
{
    public sealed class AvaritiaPlayer : ModPlayer
    {
        public bool HasSeenCosmicSphereIntro { get; private set; }
        internal bool CosmicSphereActive { get; set; }
        internal int CosmicSphereStartTime { get; set; }
        internal CosmicParticle? CosmicParticles { get; private set; }
        internal CosmicLightning? SphereLightning { get; private set; }
        internal sbyte SwordOfTheCosmosFlyTimer { get; set; }
        internal float SwordOfTheCosmosFlyRotation { get; set; }
        internal bool SwordOfTheCosmosFlyHolding { get; set; }
        internal Vector2 SwordOfTheCosmosFlyOffset { get; set; }
        internal sbyte SwordOfTheCosmosBackTimer { get; set; }
        internal bool SwordOfTheCosmosBackHolding { get; set; }
        internal Vector2 SwordOfTheCosmosBackOffset { get; set; }
        internal float SwordOfTheCosmosBackRotation { get; set; }
        internal ushort CosmicSphereTimer { get; set; }
        internal byte SwordOfTheCosmosMarkTimer { get; set; }
        internal ushort LongbowOfTheHeavensMarkTimer { get; set; }
        internal bool CosmicSphereSuit { get; set; }
        internal bool SwordOfTheCosmosAttack { get; set; }
        private bool _lastSuit;
        public override void Initialize()
        {
            CosmicParticles = new CosmicParticle();
            SphereLightning = new CosmicLightning();
        }
        public override void OnEnterWorld()
        {
            CosmicParticles = new CosmicParticle();
            SphereLightning = new CosmicLightning();
            _lastSuit = false;

            // 客户端请求服务器发送所有在线玩家的星空状态
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)AvaritiaMod.SyncMessageType.RequestCosmicSphereStates);
                packet.Send();
            }
        }
        public override void ResetEffects()
        {
            if (Player.whoAmI == Main.myPlayer)
            {
                if (SwordOfTheCosmosAttack && Player.statLife == Player.statLifeMax2)
                {
                    SwordOfTheCosmosAttack = false;
                }
                bool hasSuit = Player.armor[0].ModItem is InfinityHelmet && Player.armor[1].ModItem is InfinityChestPlate && Player.armor[2].ModItem is InfinityBoots;
                CosmicSphereSuit = hasSuit;
                CosmicSphereActive = hasSuit;
                if (hasSuit)
                {
                    if (CosmicSphereTimer < 540)
                    {
                        CosmicSphereTimer++;
                    }
                    if (CosmicSphereTimer > 360)
                    {
                        HasSeenCosmicSphereIntro = true;
                    }
                }
                else
                {
                    CosmicSphereTimer = 0;
                }
                if (hasSuit != _lastSuit)
                {
                    if (hasSuit)
                    {
                        CosmicSphereStartTime = (int)Main.GameUpdateCount;
                        CosmicSphereTimer = HasSeenCosmicSphereIntro ? (ushort)360 : (ushort)0;
                    }
                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        SendSyncPacket(hasSuit, CosmicSphereStartTime, CosmicSphereTimer);
                    }
                    _lastSuit = hasSuit;
                }
                if (!hasSuit)
                {
                    CosmicParticles?.Clear();
                    SphereLightning?.Clear();
                }
            }
            else
            {
                if (!CosmicSphereActive)
                {
                    CosmicSphereTimer = 0;
                    CosmicParticles?.Clear();
                    SphereLightning?.Clear();
                }
            }
            if (LongbowOfTheHeavensMarkTimer > 0)
            {
                LongbowOfTheHeavensMarkTimer--;
            }
            if (SwordOfTheCosmosMarkTimer > 0)
            {
                SwordOfTheCosmosMarkTimer--;
            }
        }
        public override void PreUpdate()
        {
            if (CosmicSphereSuit && !SwordOfTheCosmosAttack)
            {
                Player.statLife = Player.statLifeMax2;
            }
            if (Player.whoAmI == Main.myPlayer || !CosmicSphereActive)
            {
                return;
            }
            int elapsed = (int)(Main.GameUpdateCount - CosmicSphereStartTime);
            CosmicSphereTimer = (ushort)MathHelper.Clamp(elapsed, 0, 540);
        }
        public override void PostUpdate()
        {
            if (CosmicSphereSuit && !SwordOfTheCosmosAttack)
            {
                Player.statLife = Player.statLifeMax2;
            }
        }
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
            => (!CosmicSphereSuit || damageSource.SourceProjectileType == ModContent.ProjectileType<SwordOfTheCosmosProj>())
            && base.PreKill(damage, hitDirection, pvp, ref playSound, ref genDust, ref damageSource);
        public override void SaveData(TagCompound tag)
        {
            tag["HasSeenCosmicSphereIntro"] = HasSeenCosmicSphereIntro;
        }
        public override void LoadData(TagCompound tag)
        {
            HasSeenCosmicSphereIntro = tag.GetBool("HasSeenCosmicSphereIntro");
        }
        internal void SendSyncPacket(bool active, int startTime, ushort timer)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)AvaritiaMod.SyncMessageType.RequestCosmicSphere);
            packet.Write(Player.whoAmI);
            packet.Write(CosmicSphereSuit);
            packet.Write(active);
            packet.Write(startTime);
            packet.Write(timer);
            packet.Write(SwordOfTheCosmosAttack);
            packet.Send();
        }
    }
}