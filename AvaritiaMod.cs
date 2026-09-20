global using AvaritiaMod.Common.AvaritiaUtils;
global using AvaritiaMod.Common.Players;
global using AvaritiaMod.Common.Systems;
global using AvaritiaMod.Common.UI;
global using AvaritiaMod.Content.Buffs;
global using AvaritiaMod.Content.Items;
global using AvaritiaMod.Content.Items.Armor;
global using AvaritiaMod.Content.Items.Consumables;
global using AvaritiaMod.Content.Items.Placeable;
global using AvaritiaMod.Content.Items.Tools;
global using AvaritiaMod.Content.Items.Weapons;
global using AvaritiaMod.Content.Projectiles;
global using AvaritiaMod.Content.Rarities;
global using AvaritiaMod.Content.TileEntities;
global using AvaritiaMod.Content.Tiles;
global using EternalLib;
global using Microsoft.Xna.Framework;
global using Microsoft.Xna.Framework.Graphics;
global using Microsoft.Xna.Framework.Input;
global using ReLogic.Content;
global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using Terraria;
global using Terraria.Audio;
global using Terraria.DataStructures;
global using Terraria.Enums;
global using Terraria.GameContent;
global using Terraria.GameContent.ItemDropRules;
global using Terraria.GameContent.UI.Elements;
global using Terraria.Graphics.Effects;
global using Terraria.ID;
global using Terraria.Localization;
global using Terraria.ModLoader;
global using Terraria.ModLoader.IO;
global using Terraria.ObjectData;
global using Terraria.UI;
global using Terraria.UI.Chat;
global using static EternalLib.EternalLog;
global using Color = Microsoft.Xna.Framework.Color;
global using Item = Terraria.Item;
global using Main = Terraria.Main;
global using Player = Terraria.Player;
global using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace AvaritiaMod
{
    public sealed class AvaritiaMod : Mod
    {
        /// <summary>
        /// 模组自有协议版本：随每个包写入，接收时校验，不一致的包直接丢弃。
        /// <para>改包格式（增删字段、改变字段宽度）时必须递增，否则新旧版本混用会静默错位。</para>
        /// </summary>
        public const byte ProtocolVersion = 2;
        /// <summary>
        /// 模组自定义消息类型。<b>数值即协议</b>：每个成员都写死数值，不要复用或重排既有数值
        /// （新增消息请追加新数值）。通用的物块破坏 / 抹墙 / 结算与手持物形态同步在 <see cref="EternalNet"/>。
        /// </summary>
        public enum SyncMessageType : byte
        {
            RequestKillPlayer = 0,
            BroadcastKillPlayer = 1,
            RequestHurtPlayer = 2,
            BroadcastHurtPlayer = 3,
            BroadcastWholeTable = 4,
            SyncSlot = 5,
            RequestCompressorInput = 6,
            BroadcastCompressor = 7,
            RequestCompressorOutput = 8,
            BroadcastCollector = 9,
            RequestCollectorOutput = 10,
            RequestCosmicSphere = 11,
            RequestKillNPC = 12,
            BroadcastKillNPC = 13,
            RequestCosmicSphereStates = 14,
            BroadcastCosmicSphereStates = 15,
            /// <summary>客户端上传工作台内容物（放置带物品的工作台时使用），服务端写入实体并广播</summary>
            RequestWholeTable = 16,
            /// <summary>客户端请求服务端回传指定工作台的内容物</summary>
            RequestTableData = 17,
            /// <summary>客户端请求对压缩机 / 收集器槽位执行一次动作（放入 / 取出 / 丢弃 / 交换），数量由服务端结算</summary>
            RequestCompressorAction = 18,
            /// <summary>服务端回发“客户端应如何调整鼠标上的物品”（放入时扣除、取出 / 合成时交付）</summary>
            SyncCompressorCursor = 19,
            /// <summary>客户端请求合成（数量与材料消耗由服务端按自己的槽位结算，产物回发到鼠标）</summary>
            RequestCraft = 20
        }
        public override void Load()
        {
            //先解绑再绑定，保证热重载时不会重复订阅
            Unload();
            On_Player.Hurt_PlayerDeathReason_int_int_refHurtInfo_bool_bool_int_bool_float_float_float += PlayerHurtHook;
            On_Player.Hurt_HurtInfo_bool += PlayerHurtInfoHook;
        }
        public override void Unload()
        {
            On_Player.Hurt_PlayerDeathReason_int_int_refHurtInfo_bool_bool_int_bool_float_float_float -= PlayerHurtHook;
            On_Player.Hurt_HurtInfo_bool -= PlayerHurtInfoHook;
        }
        /// <summary>
        /// 罐子随机奖励的兜底（原版 <c>WorldGen.SpawnThingsFromPot</c>）由库的 <c>EternalLib.SpawnThingsFromPotHook</c> 负责：
        /// 库按责任人是否持有 <see cref="IAoeMiningTool"/> 判定，命中掉落交给该工具的 <see cref="IAoeMiningTool.DeliverDrops"/>
        /// （本模组即合并成物质团），本模组不再注册静态钩子。
        /// </summary>
        public override void PostSetupContent()
        {
            ColorGradient.Register(Name + "Rainbow",
                [
                    new Color(255, 85, 85),
                    new Color(255, 170, 0),
                    new Color(255, 255, 85),
                    new Color(85, 255, 85),
                    new Color(85, 255, 255),
                    new Color(85, 85, 255),
                    new Color(255, 85, 255)
                ], 80, GradientDirection.Right);
            ColorGradient.Register(Name + "SANIC",
                [
                    new Color(85, 85, 255),
                    new Color(85, 85, 255),
                    new Color(85, 85, 255),
                    new Color(85, 85, 255),
                    Color.White,
                    new Color(85, 85, 255),
                    Color.White,
                    Color.White,
                    new Color(85, 85, 255),
                    Color.White,
                    Color.White,
                    new Color(85, 85, 255),
                    new Color(255, 85, 85),
                    Color.White,
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170),
                    new Color(170, 170, 170)
                ], 50, GradientDirection.Right);
        }
        public override void HandlePacket(BinaryReader reader, int whoAmI) => AvaritiaNet.Handle(reader, whoAmI);
        /// <summary>
        /// 无尽剑命中处理：穿戴无尽全套时只记录一次“被无尽剑攻击”，否则直接击杀目标玩家。
        /// </summary>
        private double PlayerHurtHook(On_Player.orig_Hurt_PlayerDeathReason_int_int_refHurtInfo_bool_bool_int_bool_float_float_float orig, Player self, PlayerDeathReason damageSource
            , int Damage, int hitDirection, out Player.HurtInfo info, bool pvp, bool quiet, int cooldownCounter, bool dodgeable, float armorPenetration, float scalingArmorPenetration, float knockback)
        {
            info = default;
            if (damageSource.TryGetCausingEntity(out Entity entity) && entity is Projectile { ModProjectile: SwordOfTheCosmosProj } projectile)
            {
                if (self.TryGetModPlayer(out AvaritiaPlayer player) && player.CosmicSphereSuit)
                {
                    player.SwordOfTheCosmosAttack = true;
                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        player.SendSyncPacket(player.CosmicSphereActive, player.CosmicSphereStartTime, player.CosmicSphereTimer);
                    }
                    return orig.Invoke(self, damageSource, Damage, hitDirection, out info, pvp, quiet, cooldownCounter, dodgeable, armorPenetration, scalingArmorPenetration, knockback);
                }
                self.creativeGodMode = false;
                self.KillMe(damageSource, self.statLifeMax2, 0, true);
                AvaritiaNet.RequestKillPlayer(self.whoAmI, projectile.whoAmI, self.statLifeMax2);
                return 0.0;
            }
            return orig.Invoke(self, damageSource, Damage, hitDirection, out info, pvp, quiet, cooldownCounter, dodgeable, armorPenetration, scalingArmorPenetration, knockback);
        }
        /// <summary>
        /// 无尽全套免伤：非无尽剑来源的伤害直接吞掉；无尽剑伤害固定为 75。
        /// </summary>
        private void PlayerHurtInfoHook(On_Player.orig_Hurt_HurtInfo_bool orig, Player self, Player.HurtInfo info, bool quiet)
        {
            if (self.TryGetModPlayer(out AvaritiaPlayer player) && player.CosmicSphereSuit && info.DamageSource.SourceProjectileType != ModContent.ProjectileType<SwordOfTheCosmosProj>())
            {
                return;
            }
            if (info.DamageSource.SourceProjectileType == ModContent.ProjectileType<SwordOfTheCosmosProj>())
            {
                info.Damage = 75;
            }
            orig.Invoke(self, info, quiet);
        }
    }
}