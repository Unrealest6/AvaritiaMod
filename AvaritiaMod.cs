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
global using System.Reflection;
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
        /// 模组自定义消息类型。
        /// <para><b>顺序即协议</b>：新增类型只能追加到末尾，否则会让新旧版本之间的包错位。</para>
        /// </summary>
        public enum SyncMessageType : byte
        {
            RequestKillPlayer,
            BroadcastKillPlayer,
            RequestHurtPlayer,
            BroadcastHurtPlayer,
            BroadcastWholeTable,
            SyncSlot,
            ServerKillTile,
            RequestCompressorInput,
            BroadcastCompressor,
            RequestCompressorOutput,
            BroadcastCollector,
            RequestCollectorOutput,
            RequestCosmicSphere,
            RequestKillNPC,
            BroadcastKillNPC,
            RequestCosmicSphereStates,
            BroadcastCosmicSphereStates,
            /// <summary>物品形态：客户端→服务端（itemType, mode）／服务端→客户端（whoAmI, itemType, mode）</summary>
            SyncItemMode,
            /// <summary>客户端请求服务端回传所有玩家的物品形态</summary>
            RequestItemModes,
            /// <summary>服务端回传所有玩家的物品形态快照</summary>
            BroadcastItemModes,
            /// <summary>客户端请求服务端清空并破坏箱子（范围挖掘用）</summary>
            RequestChestBreak,
            /// <summary>客户端上传工作台内容物（放置带物品的工作台时使用），服务端写入实体并广播</summary>
            RequestWholeTable,
            /// <summary>客户端请求服务端回传指定工作台的内容物</summary>
            RequestTableData,
            /// <summary>客户端批量请求服务端破坏物块（范围挖掘用，一次可携带多格坐标）</summary>
            ServerKillTiles
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