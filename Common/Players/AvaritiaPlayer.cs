namespace AvaritiaMod.Common.Players
{
    public sealed class AvaritiaPlayer : ModPlayer
    {
        /// <summary>
        /// 宇宙球体活跃状态
        /// </summary>
        internal bool CosmicSphereActive { get; set; }
        /// <summary>
        /// 宇宙球体开始时间
        /// </summary>
        internal int CosmicSphereStartTime { get; set; }
        /// <summary>
        /// 宇宙粒子实例，用于在宇宙球体系统中进行绘制
        /// </summary>
        internal CosmicParticle? CosmicParticles { get; private set; }
        /// <summary>
        /// 宇宙闪电实例，用于在宇宙球体系统中进行绘制
        /// </summary>
        internal CosmicLightning? CosmicLightning { get; private set; }
        /// <summary>
        /// 无尽剑飞行状态计数器
        /// </summary>
        internal sbyte SwordOfTheCosmosFlyTimer { get; set; }
        /// <summary>
        /// 无尽剑飞行状态旋转量
        /// </summary>
        internal float SwordOfTheCosmosFlyRotation { get; set; }
        /// <summary>
        /// 无尽剑飞行状态是否手持武器
        /// </summary>
        internal bool SwordOfTheCosmosFlyHolding { get; set; }
        /// <summary>
        /// 无尽剑飞行状态绘制偏移量
        /// </summary>
        internal Vector2 SwordOfTheCosmosFlyOffset { get; set; }
        /// <summary>
        /// 无尽剑背剑状态计数器
        /// </summary>
        internal sbyte SwordOfTheCosmosBackTimer { get; set; }
        /// <summary>
        /// 无尽剑背剑状态是否手持武器
        /// </summary>
        internal bool SwordOfTheCosmosBackHolding { get; set; }
        /// <summary>
        /// 无尽剑背剑状态绘制偏移量
        /// </summary>
        internal Vector2 SwordOfTheCosmosBackOffset { get; set; }
        /// <summary>
        /// 无尽剑背剑状态旋转量
        /// </summary>
        internal float SwordOfTheCosmosBackRotation { get; set; }
        /// <summary>
        /// 宇宙球体计数器
        /// </summary>
        internal ushort CosmicSphereTimer { get; set; }
        /// <summary>
        /// 无尽剑标记攻击计数器
        /// </summary>
        internal byte SwordOfTheCosmosMarkTimer { get; set; }
        /// <summary>
        /// 天堂弓标记攻击计数器
        /// </summary>
        internal ushort LongbowOfTheHeavensMarkTimer { get; set; }
        /// <summary>
        /// 宇宙球体中玩家是否穿戴无尽全套
        /// </summary>
        internal bool CosmicSphereSuit { get; set; }
        /// <summary>
        /// 玩家是否被无尽剑攻击
        /// </summary>
        internal bool SwordOfTheCosmosAttack { get; set; }
        /// <summary>
        /// 玩家是否看过第一次穿戴无尽全套的动画
        /// </summary>
        private bool _hasSeenCosmicSphereIntro;
        /// <summary>
        /// 最后穿戴无尽全套的判定，防止玩家频繁切换全套效果导致异常
        /// </summary>
        private bool _lastSuit;
        public override void Initialize()
        {
            //在ModPlayer实例化后创建两个宇宙特效的实例
            CosmicParticles = new CosmicParticle();
            CosmicLightning = new CosmicLightning();
        }
        public override void OnEnterWorld()
        {
            //进入世界时再创建一遍，避免为空
            CosmicParticles = new CosmicParticle();
            CosmicLightning = new CosmicLightning();
            _lastSuit = false;
            //向服务端发送玩家星空球体状态
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
                //当玩家被无尽剑攻击但生命值达到最大时则取消无尽剑攻击的特判
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
                    //当计数器大于360时判定玩家已经看过动画效果
                    if (CosmicSphereTimer > 360)
                    {
                        _hasSeenCosmicSphereIntro = true;
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
                        CosmicSphereTimer = _hasSeenCosmicSphereIntro ? (ushort)360 : (ushort)0;
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
                    CosmicLightning?.Clear();
                }
            }
            else
            {
                if (!CosmicSphereActive)
                {
                    CosmicSphereTimer = 0;
                    CosmicParticles?.Clear();
                    CosmicLightning?.Clear();
                }
            }
            //处理两个标记攻击的计数器
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
            if (Player.whoAmI == Main.myPlayer || !CosmicSphereActive)
            {
                return;
            }
            int elapsed = (int)(Main.GameUpdateCount - CosmicSphereStartTime);
            CosmicSphereTimer = (ushort)MathHelper.Clamp(elapsed, 0, 540);
        }
        /// <summary>
        /// 玩家死亡前处理，在玩家穿戴无尽全套并且没有被无尽剑攻击时返回false阻止死亡
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="hitDirection"></param>
        /// <param name="pvp"></param>
        /// <param name="playSound"></param>
        /// <param name="genDust"></param>
        /// <param name="damageSource"></param>
        /// <returns></returns>
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource)
            => (!CosmicSphereSuit || damageSource.SourceProjectileType == ModContent.ProjectileType<SwordOfTheCosmosProj>())
            && base.PreKill(damage, hitDirection, pvp, ref playSound, ref genDust, ref damageSource);
        public override void SaveData(TagCompound tag)
        {
            //保存玩家是否看过动画的数据到玩家数据中
            tag["HasSeenCosmicSphereIntro"] = _hasSeenCosmicSphereIntro;
        }
        public override void LoadData(TagCompound tag)
        {
            //读取玩家是否看过动画的数据
            _hasSeenCosmicSphereIntro = tag.GetBool("HasSeenCosmicSphereIntro");
        }
        /// <summary>
        /// 发送宇宙球体的数据包到服务端
        /// </summary>
        /// <param name="active">宇宙球体是否活跃</param>
        /// <param name="startTime">宇宙球体开始时间</param>
        /// <param name="timer">计数器</param>
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