namespace AvaritiaMod
{
    /// <summary>
    /// 无尽贪婪的网络层：本模组自己的 <see cref="ModPacket"/> 类型分发、字段读写与发送入口。
    /// <para>通用的物块破坏 / 抹墙 / 结算请求与手持物形态同步由库负责（<see cref="EternalNet"/> / <c>BreakNet</c> / <c>FrameNet</c>），这里只留本模组自己的消息。</para>
    /// </summary>
    public static class AvaritiaNet
    {
        /// <summary>构造一个已写好消息类型与协议版本的包（必须在模组已加载时调用）。</summary>
        private static ModPacket NewPacket(AvaritiaMod.SyncMessageType type)
        {
            ModPacket packet = ModContent.GetInstance<AvaritiaMod>().GetPacket();
            packet.Write((byte)type);
            packet.Write(AvaritiaMod.ProtocolVersion);
            return packet;
        }
        private static bool ValidPlayerIndex(int index) => index is >= 0 and < Main.maxPlayers;
        private static bool ValidNPCIndex(int index) => index >= 0 && index < Main.maxNPCs;
        private static bool ValidProjectileIndex(int index) => index >= 0 && index < Main.maxProjectiles;
        /// <summary>服务端接受“写入方块内数据”请求的最大距离（像素，≈40 格，留足 UI 交互余量）。</summary>
        private const float MaxDataEditReach = 640f;
        /// <summary>取发包含法玩家（无效或未激活时返回 null）。</summary>
        private static Player? GetRequestPlayer(int whoAmI)
            => ValidPlayerIndex(whoAmI) && Main.player[whoAmI] is { active: true } player ? player : null;
        /// <summary>
        /// 请求方是否有权改写该坐标上的方块数据。
        /// <para>没有这道校验时，任何客户端都能改写别人的机器 / 工作台内容物（可用来复制物品）。</para>
        /// </summary>
        private static bool CanEditBlockData(int whoAmI, Point16 pos)
        {
            if (GetRequestPlayer(whoAmI) is not { } player)
            {
                return false;
            }
            float dx = Math.Abs(player.Center.X - (pos.X * 16f + 8f));
            float dy = Math.Abs(player.Center.Y - (pos.Y * 16f + 8f));
            return dx <= MaxDataEditReach && dy <= MaxDataEditReach;
        }
        /// <summary>
        /// 上传工作台内容物。
        /// <para>放置带物品的工作台时，内容物只存在于放置端的物品里（服务端与其它客户端都没有），
        /// 由服务端写入自己的实体后广播给其它客户端；否则只有放置端打开界面能看到物品。</para>
        /// </summary>
        public static void RequestWholeTable(Point16 position, Item[,]? items, int size)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || size <= 0)
            {
                return;
            }
            int sourceWidth = items?.GetLength(0) ?? 0;
            int sourceHeight = items?.GetLength(1) ?? 0;
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.RequestWholeTable);
            packet.Write(position.X);
            packet.Write(position.Y);
            //写入顺序必须与实体 NetReceive 的读取顺序完全一致（先 x 后 y），数组较小时补空物品
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Item entry = x < sourceWidth && y < sourceHeight ? items![x, y] : new Item();
                    ItemIO.Send(entry, packet, writeStack: true, writeFavorite: true);
                }
            }
            packet.Send();
        }
        /// <summary>请求服务端回传指定工作台的内容物（打开界面前刷新用）。</summary>
        public static void RequestTableData(Point16 position)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.RequestTableData);
            packet.Write(position.X);
            packet.Write(position.Y);
            packet.Send();
        }
        /// <summary>请求服务端同步“玩家被无尽剑击杀”。</summary>
        public static void RequestKillPlayer(int playerIndex, int projectileIndex, int damage)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.RequestKillPlayer);
            packet.Write(playerIndex);
            packet.Write(projectileIndex);
            packet.Write(damage);
            packet.Send();
        }
        /// <summary>客户端上报工作台某个槽位的内容（服务端写入实体后广播给其它客户端）。</summary>
        public static void RequestSyncSlot(Point16 position, int x, int y, Item item)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.SyncSlot);
            packet.Write(position.X);
            packet.Write(position.Y);
            packet.Write((byte)x);
            packet.Write((byte)y);
            ItemIO.Send(item, packet, writeStack: true, writeFavorite: true);
            packet.Send();
        }
        /// <summary>请求服务端同步“NPC 被无尽剑击杀”。</summary>
        public static void RequestKillNPC(int npcIndex)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.RequestKillNPC);
            packet.Write(npcIndex);
            packet.Send();
        }
        /// <summary>请求服务端同步“玩家被末影珍珠伤害”。</summary>
        public static void RequestHurtPlayer(int playerIndex, int projectileIndex, int damage, int hitDirection, bool pvp)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.RequestHurtPlayer);
            packet.Write(playerIndex);
            packet.Write(projectileIndex);
            packet.Write(damage);
            packet.Write(hitDirection);
            packet.Write(pvp);
            packet.Send();
        }
        /// <summary>请求写入中子态素压缩机的输入/输出槽。</summary>
        public static void RequestCompressorSlot(Point16 position, Item item, bool output)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }
            ModPacket packet = NewPacket(output
                ? AvaritiaMod.SyncMessageType.RequestCompressorOutput
                : AvaritiaMod.SyncMessageType.RequestCompressorInput);
            packet.Write(position.X);
            packet.Write(position.Y);
            ItemIO.Send(item, packet, writeStack: true, writeFavorite: true);
            packet.Send();
        }
        /// <summary>请求写入中子态素收集器的输出槽。</summary>
        public static void RequestCollectorOutput(Point16 position, Item item)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.RequestCollectorOutput);
            packet.Write(position.X);
            packet.Write(position.Y);
            ItemIO.Send(item, packet, writeStack: true, writeFavorite: true);
            packet.Send();
        }
        /// <summary>请求服务端广播本地玩家的宇宙球体状态。</summary>
        public static void RequestCosmicSphere(int playerIndex, bool suit, bool active, int startTime, ushort timer, bool attack)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.RequestCosmicSphere);
            packet.Write(playerIndex);
            packet.Write(suit);
            packet.Write(active);
            packet.Write(startTime);
            packet.Write(timer);
            packet.Write(attack);
            packet.Send();
        }
        /// <summary>请求服务端回传所有玩家的宇宙球体状态（进入世界时使用）。</summary>
        public static void RequestCosmicSphereStates()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }
            NewPacket(AvaritiaMod.SyncMessageType.RequestCosmicSphereStates).Send();
        }
        /// <summary>
        /// 服务端把某个物块实体的整份状态发出去。
        /// <para>载荷由该实体自己的 <see cref="ModTileEntity.NetSend"/> 决定（与它的
        /// <see cref="ModTileEntity.NetReceive"/> 成对），包体只额外携带实体坐标。</para>
        /// </summary>
        /// <param name="entity">要广播的物块实体（必须是服务端自己持有的实例）。</param>
        /// <param name="type">对应的广播消息类型。</param>
        /// <param name="toClient">-1 表示广播给所有客户端，否则只发给指定客户端。</param>
        public static void BroadcastTileEntity(ModTileEntity entity, AvaritiaMod.SyncMessageType type, int toClient = -1)
        {
            if (Main.netMode != NetmodeID.Server)
            {
                return;
            }
            ModPacket packet = NewPacket(type);
            packet.Write(entity.Position.X);
            packet.Write(entity.Position.Y);
            entity.NetSend(packet);
            if (toClient == -1)
            {
                packet.Send();
            }
            else
            {
                packet.Send(toClient);
            }
        }
        // ---------------------------------------------------------------- 分发
        public static void Handle(BinaryReader reader, int whoAmI)
        {
            AvaritiaMod.SyncMessageType msgType = (AvaritiaMod.SyncMessageType)reader.ReadByte();
            byte version = reader.ReadByte();
            if (version != AvaritiaMod.ProtocolVersion)
            {
                EternalLog.Warn($"Dropped a packet with protocol version {version} (this build uses {AvaritiaMod.ProtocolVersion}); "
                    + "the other side is running a different AvaritiaMod build.");
                return;
            }
            if (Main.netMode == NetmodeID.Server)
            {
                HandleOnServer(msgType, reader, whoAmI);
            }
            else if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                HandleOnClient(msgType, reader, whoAmI);
            }
        }
        private static void HandleOnServer(AvaritiaMod.SyncMessageType msgType, BinaryReader reader, int whoAmI)
        {
            switch (msgType)
            {
                case AvaritiaMod.SyncMessageType.RequestKillPlayer:
                    HandleRequestKillPlayer(reader, whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.RequestKillNPC:
                    HandleRequestKillNPC(reader, whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.RequestHurtPlayer:
                    HandleRequestHurtPlayer(reader, whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.SyncSlot:
                    HandleSyncSlot(reader, whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.RequestCompressorInput:
                    HandleCompressorSlotRequest(reader, whoAmI, output: false);
                    break;
                case AvaritiaMod.SyncMessageType.RequestCompressorOutput:
                    HandleCompressorSlotRequest(reader, whoAmI, output: true);
                    break;
                case AvaritiaMod.SyncMessageType.RequestCollectorOutput:
                    HandleCollectorOutputRequest(reader, whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.RequestCosmicSphere:
                    HandleRequestCosmicSphere(reader, whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.RequestCosmicSphereStates:
                    HandleRequestCosmicSphereStates(whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.RequestWholeTable:
                    HandleRequestWholeTable(reader, whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.RequestTableData:
                    HandleRequestTableData(reader, whoAmI);
                    break;
            }
        }
        private static void HandleOnClient(AvaritiaMod.SyncMessageType msgType, BinaryReader reader, int whoAmI)
        {
            switch (msgType)
            {
                case AvaritiaMod.SyncMessageType.BroadcastKillPlayer:
                    HandleBroadcastKillPlayer(reader);
                    break;
                case AvaritiaMod.SyncMessageType.BroadcastKillNPC:
                    HandleBroadcastKillNPC(reader);
                    break;
                case AvaritiaMod.SyncMessageType.BroadcastHurtPlayer:
                    HandleBroadcastHurtPlayer(reader);
                    break;
                case AvaritiaMod.SyncMessageType.BroadcastWholeTable:
                    HandleBroadcastWholeTable(reader);
                    break;
                case AvaritiaMod.SyncMessageType.SyncSlot:
                    HandleSyncSlot(reader, whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.BroadcastCompressor:
                    HandleBroadcastCompressor(reader);
                    break;
                case AvaritiaMod.SyncMessageType.BroadcastCollector:
                    HandleBroadcastCollector(reader);
                    break;
                case AvaritiaMod.SyncMessageType.RequestCosmicSphere:
                    HandleRequestCosmicSphere(reader, whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.BroadcastCosmicSphereStates:
                    HandleBroadcastCosmicSphereStates(reader);
                    break;
            }
        }
        // ---------------------------------------------------------------- 合成台
        /// <summary>服务端：写入客户端上传的工作台内容物，并转发给其它客户端。</summary>
        private static void HandleRequestWholeTable(BinaryReader reader, int whoAmI)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            if (!CanEditBlockData(whoAmI, pos) || GetTable(pos) is not { } table)
            {
                return;
            }
            table.NetReceive(reader);
            //请求方本地已经写入同一份数据，不必回传
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.BroadcastWholeTable);
            packet.Write(pos.X);
            packet.Write(pos.Y);
            table.NetSend(packet);
            packet.Send(ignoreClient: whoAmI);
        }
        /// <summary>服务端：把指定工作台的内容物回传给请求的客户端。</summary>
        private static void HandleRequestTableData(BinaryReader reader, int whoAmI)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            if (!CanEditBlockData(whoAmI, pos) || GetTable(pos) is not { } table)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.BroadcastWholeTable);
            packet.Write(pos.X);
            packet.Write(pos.Y);
            table.NetSend(packet);
            packet.Send(whoAmI);
        }
        private static void HandleBroadcastWholeTable(BinaryReader reader)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            if (GetTable(pos) is not { } table)
            {
                return;
            }
            table.NetReceive(reader);
            SyncTableToUI(table);
        }
        private static void HandleSyncSlot(BinaryReader reader, int whoAmI)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            byte x = reader.ReadByte();
            byte y = reader.ReadByte();
            Item item = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            if (Main.netMode == NetmodeID.Server && !CanEditBlockData(whoAmI, pos))
            {
                return;
            }
            if (GetTable(pos) is not { } table || table.Items is null || x >= table.Size || y >= table.Size)
            {
                return;
            }
            table.Items[x, y] = item.Clone();
            if (Main.netMode == NetmodeID.Server)
            {
                ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.SyncSlot);
                packet.Write(pos.X);
                packet.Write(pos.Y);
                packet.Write(x);
                packet.Write(y);
                ItemIO.Send(item, packet, writeStack: true, writeFavorite: true);
                packet.Send(ignoreClient: whoAmI);
                return;
            }
            SyncTableToUI(table);
        }
        private static CraftingTableTileEntity? GetTable(Point16 pos)
            //用原版 TryGet 而不是直接查 ByPosition：它内部会按 TileObjectData.TopLeft 归一化坐标，否则上报坐标不是左上角时找不到实体
            => TileEntity.TryGet(pos.X, pos.Y, out CraftingTableTileEntity table) ? table : null;
        private static void SyncTableToUI(CraftingTableTileEntity table)
        {
            if (table.Items is null || table.CraftingTableUI?.Slots is null)
            {
                return;
            }
            for (int i = 0; i < table.Size; i++)
            {
                for (int j = 0; j < table.Size; j++)
                {
                    table.CraftingTableUI.Slots[i, j].SetItemSilently(table.Items[i, j]);
                }
            }
        }
        // ---------------------------------------------------------------- 压缩机 / 收集器
        private static void HandleCompressorSlotRequest(BinaryReader reader, int whoAmI, bool output)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            Item item = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            if (!CanEditBlockData(whoAmI, pos) || GetCompressor(pos) is not { } compressor)
            {
                return;
            }
            if (output)
            {
                compressor.OutputItem = item.Clone();
            }
            else
            {
                compressor.InputItem = item.Clone();
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.BroadcastCompressor);
            packet.Write(pos.X);
            packet.Write(pos.Y);
            compressor.NetSend(packet);
            packet.Send(ignoreClient: whoAmI);
        }
        private static NeutroniumCompressorTileEntity? GetCompressor(Point16 pos)
            => TileEntity.ByPosition.TryGetValue(pos, out TileEntity? te) && te is NeutroniumCompressorTileEntity compressor ? compressor : null;
        private static NeutronCollectorTileEntity? GetCollector(Point16 pos)
            => TileEntity.ByPosition.TryGetValue(pos, out TileEntity? te) && te is NeutronCollectorTileEntity collector ? collector : null;
        private static void HandleCollectorOutputRequest(BinaryReader reader, int whoAmI)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            Item item = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            if (!CanEditBlockData(whoAmI, pos) || GetCollector(pos) is not { } collector)
            {
                return;
            }
            collector.OutputItem = item.Clone();
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.BroadcastCollector);
            packet.Write(pos.X);
            packet.Write(pos.Y);
            collector.NetSend(packet);
            packet.Send(ignoreClient: whoAmI);
        }
        private static void HandleBroadcastCompressor(BinaryReader reader)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            if (GetCompressor(pos) is not { } compressor)
            {
                return;
            }
            compressor.NetReceive(reader);
            compressor.NeutroniumCompressorUI?.InputSlot?.SetItemSilently(compressor.InputItem);
            compressor.NeutroniumCompressorUI?.OutputSlot?.SetItemSilently(compressor.OutputItem);
        }
        private static void HandleBroadcastCollector(BinaryReader reader)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            if (GetCollector(pos) is not { } collector)
            {
                return;
            }
            collector.NetReceive(reader);
            collector.NeutronCollectorUI?.OutputSlot?.SetItemSilently(collector.OutputItem);
        }
        // ---------------------------------------------------------------- 玩家 / NPC
        private static void HandleRequestKillPlayer(BinaryReader reader, int whoAmI)
        {
            int playerIndex = reader.ReadInt32();
            int projectileIndex = reader.ReadInt32();
            int damage = reader.ReadInt32();
            if (!ValidPlayerIndex(playerIndex) || !ValidProjectileIndex(projectileIndex)
                || Main.player[playerIndex] is not { active: true } || Main.projectile[projectileIndex] is not { active: true })
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.BroadcastKillPlayer);
            packet.Write(playerIndex);
            packet.Write(projectileIndex);
            packet.Write(damage);
            // 请求方已经在本地执行过击杀，排除它可避免重复结算。
            packet.Send(ignoreClient: whoAmI);
        }
        private static void HandleRequestKillNPC(BinaryReader reader, int whoAmI)
        {
            int npcIndex = reader.ReadInt32();
            if (!ValidNPCIndex(npcIndex) || Main.npc[npcIndex] is not { active: true } npc)
            {
                return;
            }
            npc.NPCLoot();
            npc.life = 0;
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.BroadcastKillNPC);
            packet.Write(npcIndex);
            packet.Send(ignoreClient: whoAmI);
        }
        private static void HandleRequestHurtPlayer(BinaryReader reader, int whoAmI)
        {
            int playerIndex = reader.ReadInt32();
            int projectileIndex = reader.ReadInt32();
            int damage = reader.ReadInt32();
            int hitDirection = reader.ReadInt32();
            bool pvp = reader.ReadBoolean();
            if (!ValidPlayerIndex(playerIndex) || !ValidProjectileIndex(projectileIndex)
                || Main.player[playerIndex] is not { active: true } || Main.projectile[projectileIndex] is not { active: true })
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.BroadcastHurtPlayer);
            packet.Write(playerIndex);
            packet.Write(projectileIndex);
            packet.Write(damage);
            packet.Write(hitDirection);
            packet.Write(pvp);
            packet.Send(ignoreClient: whoAmI);
        }
        private static void HandleBroadcastKillPlayer(BinaryReader reader)
        {
            int playerIndex = reader.ReadInt32();
            int projectileIndex = reader.ReadInt32();
            int damage = reader.ReadInt32();
            if (!ValidPlayerIndex(playerIndex) || !ValidProjectileIndex(projectileIndex))
            {
                return;
            }
            Player player = Main.player[playerIndex];
            if (player is not { active: true } || Main.projectile[projectileIndex] is not { active: true })
            {
                return;
            }
            player.creativeGodMode = false;
            player.KillMe(PlayerDeathReason.ByProjectile(playerIndex, projectileIndex), damage, 0, true);
        }
        private static void HandleBroadcastKillNPC(BinaryReader reader)
        {
            int npcIndex = reader.ReadInt32();
            if (!ValidNPCIndex(npcIndex) || Main.npc[npcIndex] is not { active: true } npc)
            {
                return;
            }
            if (npc.HitSound != null)
            {
                SoundEngine.PlaySound(npc.HitSound, npc.position);
            }
            npc.life = 0;
            npc.HitEffect(0, 0, true);
            SoundStyle? deathSound = npc is { type: NPCID.Pirate, IsShimmerVariant: true } ? SoundID.NPCDeath6 : npc.DeathSound;
            if (deathSound != null)
            {
                SoundEngine.PlaySound(deathSound, npc.position);
            }
        }
        private static void HandleBroadcastHurtPlayer(BinaryReader reader)
        {
            int playerIndex = reader.ReadInt32();
            int projectileIndex = reader.ReadInt32();
            int damage = reader.ReadInt32();
            int hitDirection = reader.ReadInt32();
            bool pvp = reader.ReadBoolean();
            if (!ValidPlayerIndex(playerIndex) || !ValidProjectileIndex(projectileIndex))
            {
                return;
            }
            Player player = Main.player[playerIndex];
            if (player is not { active: true } || Main.projectile[projectileIndex] is not { active: true })
            {
                return;
            }
            player.Hurt(PlayerDeathReason.ByProjectile(playerIndex, projectileIndex), damage, hitDirection, pvp);
        }
        // ---------------------------------------------------------------- 宇宙球体
        private static void HandleRequestCosmicSphere(BinaryReader reader, int whoAmI)
        {
            int playerIndex = reader.ReadInt32();
            bool suit = reader.ReadBoolean();
            bool active = reader.ReadBoolean();
            int startTime = reader.ReadInt32();
            ushort timer = reader.ReadUInt16();
            bool attack = reader.ReadBoolean();
            //服务端只认发包含法玩家：包里的下标由客户端提供，不能作为权威
            if (Main.netMode == NetmodeID.Server)
            {
                if (GetRequestPlayer(whoAmI) is null)
                {
                    return;
                }
                playerIndex = whoAmI;
            }
            if (ValidPlayerIndex(playerIndex) && Main.player[playerIndex].TryGetModPlayer(out AvaritiaPlayer modPlayer))
            {
                modPlayer.CosmicSphereSuit = suit;
                modPlayer.CosmicSphereActive = active;
                modPlayer.CosmicSphereStartTime = startTime;
                modPlayer.CosmicSphereTimer = timer;
                modPlayer.SwordOfTheCosmosAttack = attack;
            }
            if (Main.netMode == NetmodeID.Server && whoAmI != -1)
            {
                ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.RequestCosmicSphere);
                packet.Write(playerIndex);
                packet.Write(suit);
                packet.Write(active);
                packet.Write(startTime);
                packet.Write(timer);
                packet.Write(attack);
                packet.Send(ignoreClient: whoAmI);
            }
        }
        private static void HandleRequestCosmicSphereStates(int whoAmI)
        {
            if (!ValidPlayerIndex(whoAmI))
            {
                return;
            }
            List<(int playerIndex, bool active, int startTime, ushort timer)> states = [];
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player plr = Main.player[i];
                if (plr is { active: true } && plr.TryGetModPlayer(out AvaritiaPlayer mp) && mp.CosmicSphereActive)
                {
                    states.Add((plr.whoAmI, mp.CosmicSphereActive, mp.CosmicSphereStartTime, mp.CosmicSphereTimer));
                }
            }
            ModPacket response = NewPacket(AvaritiaMod.SyncMessageType.BroadcastCosmicSphereStates);
            response.Write(states.Count);
            foreach ((int playerIndex, bool active, int startTime, ushort timer) state in states)
            {
                response.Write(state.playerIndex);
                response.Write(state.active);
                response.Write(state.startTime);
                response.Write(state.timer);
            }
            response.Send(whoAmI);
        }
        private static void HandleBroadcastCosmicSphereStates(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            //上界校验：伪造包里的 count 会让读取越界
            if (count is <= 0 or > Main.maxPlayers)
            {
                return;
            }
            for (int i = 0; i < count; i++)
            {
                int idx = reader.ReadInt32();
                bool active = reader.ReadBoolean();
                int startTime = reader.ReadInt32();
                ushort timer = reader.ReadUInt16();
                if (!ValidPlayerIndex(idx) || !Main.player[idx].TryGetModPlayer(out AvaritiaPlayer mp))
                {
                    continue;
                }
                mp.CosmicSphereActive = active;
                mp.CosmicSphereStartTime = startTime;
                mp.CosmicSphereTimer = timer;
            }
        }
    }
}
