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
            BroadcastCosmicSphereStates
        }
        public override void Load()
        {
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
            ColorGradient.Register("Rainbow",
                [
                    new Color(255, 85, 85),
                    new Color(255, 170, 0),
                    new Color(255, 255, 85),
                    new Color(85, 255, 85),
                    new Color(85, 255, 255),
                    new Color(85, 85, 255),
                    new Color(255, 85, 255)
                ], 80, GradientDirection.Right);
            ColorGradient.Register("SANIC",
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
        private void HandleBroadcastWholeTable(BinaryReader reader)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            if (!TileEntity.ByPosition.TryGetValue(pos, out TileEntity? te) || te is not CraftingTableTileEntity table)
            {
                return;
            }
            table.NetReceive(reader);
            if (table.Items is null)
            {
                return;
            }
            for (int x = 0; x < table.Size; x++)
            {
                for (int y = 0; y < table.Size; y++)
                {
                    table.CraftingTableUI?.Slots?[x, y].SetItemSilently(table.Items[x, y]);
                }
            }
        }
        private void HandleSyncSlot(BinaryReader reader, int whoAmI)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            byte x = reader.ReadByte();
            byte y = reader.ReadByte();
            Item item = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            if (!TileEntity.ByPosition.TryGetValue(pos, out TileEntity? te) || te is not CraftingTableTileEntity table)
            {
                return;
            }
            if (Main.netMode == NetmodeID.Server)
            {
                table.Items?[x, y] = item.Clone();
                ModPacket packet = GetPacket();
                packet.Write((byte)SyncMessageType.SyncSlot);
                packet.Write(pos.X);
                packet.Write(pos.Y);
                packet.Write(x);
                packet.Write(y);
                ItemIO.Send(item, packet, writeStack: true, writeFavorite: true);
                packet.Send(ignoreClient: whoAmI);
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                table.Items?[x, y] = item.Clone();
                if (table.Items is null)
                {
                    return;
                }
                for (int i = 0; i < table.Size; i++)
                {
                    for (int j = 0; j < table.Size; j++)
                    {
                        table.CraftingTableUI?.Slots?[i, j].SetItemSilently(table.Items[i, j]);
                    }
                }
            }
        }
        private void HandleRequestCompressorInput(BinaryReader reader, int whoAmI)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            Item inputItem = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            if (!TileEntity.ByPosition.TryGetValue(pos, out TileEntity? te) ||
                te is not NeutroniumCompressorTileEntity compressor)
            {
                return;
            }
            compressor.InputItem = inputItem.Clone();
            ModPacket packet = GetPacket();
            packet.Write((byte)SyncMessageType.BroadcastCompressor);
            packet.Write(pos.X);
            packet.Write(pos.Y);
            compressor.NetSend(packet);
            packet.Send(ignoreClient: whoAmI);
        }
        private void HandleRequestCompressorOutput(BinaryReader reader, int whoAmI)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            Item outputItem = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            if (!TileEntity.ByPosition.TryGetValue(pos, out TileEntity? te) ||
                te is not NeutroniumCompressorTileEntity compressor)
            {
                return;
            }
            compressor.OutputItem = outputItem.Clone();
            ModPacket packet = GetPacket();
            packet.Write((byte)SyncMessageType.BroadcastCompressor);
            packet.Write(pos.X);
            packet.Write(pos.Y);
            compressor.NetSend(packet);
            packet.Send(ignoreClient: whoAmI);
        }
        private void HandleBroadcastCompressorInput(BinaryReader reader)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            if (!TileEntity.ByPosition.TryGetValue(pos, out TileEntity? te) ||
                te is not NeutroniumCompressorTileEntity compressor)
            {
                return;
            }
            compressor.NetReceive(reader);
            compressor.NeutroniumCompressorUI?.InputSlot?.SetItemSilently(compressor.InputItem);
            compressor.NeutroniumCompressorUI?.OutputSlot?.SetItemSilently(compressor.OutputItem);
        }
        private void HandleRequestCollectorOutput(BinaryReader reader, int whoAmI)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            Item outputItem = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            if (!TileEntity.ByPosition.TryGetValue(pos, out TileEntity? te) ||
                te is not NeutronCollectorTileEntity compressor)
            {
                return;
            }
            compressor.OutputItem = outputItem.Clone();
            ModPacket packet = GetPacket();
            packet.Write((byte)SyncMessageType.BroadcastCollector);
            packet.Write(pos.X);
            packet.Write(pos.Y);
            compressor.NetSend(packet);
            packet.Send(ignoreClient: whoAmI);
        }
        private void HandleBroadcastCollectorInput(BinaryReader reader)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            if (!TileEntity.ByPosition.TryGetValue(pos, out TileEntity? te) ||
                te is not NeutronCollectorTileEntity compressor)
            {
                return;
            }
            compressor.NetReceive(reader);
            compressor.NeutronCollectorUI?.OutputSlot?.SetItemSilently(compressor.OutputItem);
        }
        private void HandleRequestCosmicSphere(BinaryReader reader, int whoAmI)
        {
            int playerIndex = reader.ReadInt32();
            bool suit = reader.ReadBoolean();
            bool active = reader.ReadBoolean();
            int startTime = reader.ReadInt32();
            ushort timer = reader.ReadUInt16();
            bool attack = reader.ReadBoolean();
            if (playerIndex is >= 0 and < Main.maxPlayers)
            {
                Player player = Main.player[playerIndex];
                if (player.TryGetModPlayer(out AvaritiaPlayer modPlayer))
                {
                    modPlayer.CosmicSphereSuit = suit;
                    modPlayer.CosmicSphereActive = active;
                    modPlayer.CosmicSphereStartTime = startTime;
                    modPlayer.CosmicSphereTimer = timer;
                    modPlayer.SwordOfTheCosmosAttack = true;
                }
            }
            if (Main.netMode == NetmodeID.Server && whoAmI != -1)
            {
                ModPacket packet = GetPacket();
                packet.Write((byte)SyncMessageType.RequestCosmicSphere);
                packet.Write(playerIndex);
                packet.Write(suit);
                packet.Write(active);
                packet.Write(startTime);
                packet.Write(timer);
                packet.Write(attack);
                packet.Send(-1, whoAmI);
            }
        }
        private void HandleRequestKillPlayer(BinaryReader reader, int whoAmI)
        {
            int playerIndex = reader.ReadInt32();
            int projectileIndex = reader.ReadInt32();
            int damage = reader.ReadInt32();
            Player player = Main.player[playerIndex];
            Projectile projectile = Main.projectile[projectileIndex];
            if (player is not { active: true } || projectile is not { active: true })
            {
                return;
            }
            ModPacket packet = GetPacket();
            packet.Write((byte)SyncMessageType.BroadcastKillPlayer);
            packet.Write(playerIndex);
            packet.Write(projectileIndex);
            packet.Write(damage);
            packet.Send();
        }
        private void HandleRequestKillNPC(BinaryReader reader, int whoAmI)
        {
            int npcIndex = reader.ReadInt32();
            NPC npc = Main.npc[npcIndex];
            if (npc is not { active: true })
            {
                return;
            }
            npc.NPCLoot();
            npc.life = 0;
            ModPacket packet = GetPacket();
            packet.Write((byte)SyncMessageType.BroadcastKillNPC);
            packet.Write(npcIndex);
            packet.Send();
        }
        private void HandleRequestHurtPlayer(BinaryReader reader, int whoAmI)
        {
            int playerIndex = reader.ReadInt32();
            int projectileIndex = reader.ReadInt32();
            int damage = reader.ReadInt32();
            int hitDirection = reader.ReadInt32();
            bool pvp = reader.ReadBoolean();
            Player player = Main.player[playerIndex];
            Projectile projectile = Main.projectile[projectileIndex];
            if (player is not { active: true } || projectile is not { active: true })
            {
                return;
            }
            ModPacket packet = GetPacket();
            packet.Write((byte)SyncMessageType.BroadcastHurtPlayer);
            packet.Write(playerIndex);
            packet.Write(projectileIndex);
            packet.Write(damage);
            packet.Write(hitDirection);
            packet.Write(pvp);
            packet.Send();
        }
        private void HandleRequestCosmicSphereStates(BinaryReader reader, int whoAmI)
        {
            List<(int playerIndex, bool active, int startTime, ushort timer)> states = [];
            foreach (Player plr in Main.player)
            {
                if (plr.active && plr.TryGetModPlayer(out AvaritiaPlayer mp) && mp.CosmicSphereActive)
                {
                    states.Add((plr.whoAmI, mp.CosmicSphereActive, mp.CosmicSphereStartTime, mp.CosmicSphereTimer));
                }
            }
            ModPacket response = GetPacket();
            response.Write((byte)SyncMessageType.BroadcastCosmicSphereStates);
            response.Write(states.Count);
            foreach ((int playerIndex, bool active, int startTime, ushort timer) s in states)
            {
                response.Write(s.playerIndex);
                response.Write(s.active);
                response.Write(s.startTime);
                response.Write(s.timer);
            }
            response.Send(whoAmI);
        }
        private void HandleBroadcastKillPlayer(BinaryReader reader)
        {
            int playerIndex = reader.ReadInt32();
            int projectileIndex = reader.ReadInt32();
            int damage = reader.ReadInt32();
            Player player = Main.player[playerIndex];
            Projectile projectile = Main.projectile[projectileIndex];
            if (player is not { active: true } || projectile is not { active: true })
            {
                return;
            }
            player.creativeGodMode = false;
            player.KillMe(PlayerDeathReason.ByProjectile(playerIndex, projectileIndex), damage, 0, true);
        }
        private void HandleBroadcastKillNPC(BinaryReader reader)
        {
            int npcIndex = reader.ReadInt32();
            NPC npc = Main.npc[npcIndex];
            if (npc is not { active: true })
            {
                return;
            }
            if (npc.HitSound != null)
            {
                SoundEngine.PlaySound(npc.HitSound, npc.position);
            }
            npc.life = 0;
            npc.HitEffect(0, 0, true);
            SoundStyle? legacySoundStyle = npc.DeathSound;
            if (npc is { type: NPCID.Pirate, IsShimmerVariant: true })
            {
                legacySoundStyle = SoundID.NPCDeath6;
            }
            if (legacySoundStyle != null)
            {
                SoundEngine.PlaySound(legacySoundStyle, npc.position);
            }
        }
        private void HandleBroadcastHurtPlayer(BinaryReader reader)
        {
            int playerIndex = reader.ReadInt32();
            int projectileIndex = reader.ReadInt32();
            int damage = reader.ReadInt32();
            int hitDirection = reader.ReadInt32();
            bool pvp = reader.ReadBoolean();
            Player player = Main.player[playerIndex];
            Projectile projectile = Main.projectile[projectileIndex];
            if (player is not { active: true } || projectile is not { active: true })
            {
                return;
            }
            player.Hurt(PlayerDeathReason.ByProjectile(playerIndex, projectileIndex), damage, hitDirection, pvp);
        }
        private void HandleBroadcastCosmicSphereStates(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int idx = reader.ReadInt32();
                bool active = reader.ReadBoolean();
                int startTime = reader.ReadInt32();
                ushort timer = reader.ReadUInt16();
                if (idx is < 0 or >= Main.maxPlayers || !Main.player[idx].TryGetModPlayer(out AvaritiaPlayer mp))
                {
                    continue;
                }
                mp.CosmicSphereActive = active;
                mp.CosmicSphereStartTime = startTime;
                mp.CosmicSphereTimer = timer;
            }
        }
        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            SyncMessageType msgType = (SyncMessageType)reader.ReadByte();
            if (Main.netMode == NetmodeID.Server)
            {
                if (msgType == SyncMessageType.RequestKillPlayer)
                {
                    HandleRequestKillPlayer(reader, whoAmI);
                }
                else if (msgType == SyncMessageType.RequestKillNPC)
                {
                    HandleRequestKillNPC(reader, whoAmI);
                }
                else if (msgType == SyncMessageType.RequestHurtPlayer)
                {
                    HandleRequestHurtPlayer(reader, whoAmI);
                }
                else if (msgType == SyncMessageType.ServerKillTile)
                {
                    int x = reader.ReadInt32();
                    int y = reader.ReadInt32();
                    WorldGen.KillWall(x, y);
                    WorldGen.KillTile(x, y);
                }
                else if (msgType == SyncMessageType.SyncSlot)
                {
                    HandleSyncSlot(reader, whoAmI);
                }
                else if (msgType == SyncMessageType.RequestCompressorInput)
                {
                    HandleRequestCompressorInput(reader, whoAmI);
                }
                else if (msgType == SyncMessageType.RequestCompressorOutput)
                {
                    HandleRequestCompressorOutput(reader, whoAmI);
                }
                else if (msgType == SyncMessageType.RequestCollectorOutput)
                {
                    HandleRequestCollectorOutput(reader, whoAmI);
                }
                else if (msgType == SyncMessageType.RequestCosmicSphere)
                {
                    HandleRequestCosmicSphere(reader, whoAmI);
                }
                else if (msgType == SyncMessageType.RequestCosmicSphereStates)
                {
                    HandleRequestCosmicSphereStates(reader, whoAmI);
                }
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                if (msgType == SyncMessageType.BroadcastKillPlayer)
                {
                    HandleBroadcastKillPlayer(reader);
                }
                else if (msgType == SyncMessageType.BroadcastKillNPC)
                {
                    HandleBroadcastKillNPC(reader);
                }
                else if (msgType == SyncMessageType.BroadcastHurtPlayer)
                {
                    HandleBroadcastHurtPlayer(reader);
                }
                else if (msgType == SyncMessageType.BroadcastWholeTable)
                {
                    HandleBroadcastWholeTable(reader);
                }
                else if (msgType == SyncMessageType.SyncSlot)
                {
                    HandleSyncSlot(reader, whoAmI);
                }
                else if (msgType == SyncMessageType.BroadcastCompressor)
                {
                    HandleBroadcastCompressorInput(reader);
                }
                else if (msgType == SyncMessageType.BroadcastCollector)
                {
                    HandleBroadcastCollectorInput(reader);
                }
                else if (msgType == SyncMessageType.RequestCosmicSphere)
                {
                    HandleRequestCosmicSphere(reader, whoAmI);
                }
                else if (msgType == SyncMessageType.BroadcastCosmicSphereStates)
                {
                    HandleBroadcastCosmicSphereStates(reader);
                }
            }
        }
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
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    ModPacket packet = GetPacket();
                    packet.Write((byte)SyncMessageType.RequestKillPlayer);
                    packet.Write(self.whoAmI);
                    packet.Write(projectile.whoAmI);
                    packet.Write(self.statLifeMax2);
                    packet.Send();
                }
                return 0.0;
            }
            return orig.Invoke(self, damageSource, Damage, hitDirection, out info, pvp, quiet, cooldownCounter, dodgeable, armorPenetration, scalingArmorPenetration, knockback);
        }
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
