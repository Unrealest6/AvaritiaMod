namespace AvaritiaMod
{
    /// <summary>
    /// 无尽贪婪的网络层：所有 <see cref="ModPacket"/> 的类型分发、字段读写与发送入口都集中在这里。
    /// </summary>
    public static class AvaritiaNet
    {
        /// <summary>构造一个已写好消息类型的包（必须在模组已加载时调用）。</summary>
        private static ModPacket NewPacket(AvaritiaMod.SyncMessageType type)
        {
            ModPacket packet = ModContent.GetInstance<AvaritiaMod>().GetPacket();
            packet.Write((byte)type);
            return packet;
        }
        private static bool ValidPlayerIndex(int index) => index is >= 0 and < Main.maxPlayers;
        /// <summary>单个批量破坏包最多携带的坐标数（400 × 8 字节 ≈ 3.2KB，远低于包长度上限）。</summary>
        private const int MaxBatchKillTiles = 400;
        /// <summary>服务端接受破坏请求的最大距离（像素，≈100 格）。</summary>
        private const float MaxKillReach = 1600f;
        private static bool ValidNPCIndex(int index) => index >= 0 && index < Main.maxNPCs;
        private static bool ValidProjectileIndex(int index) => index >= 0 && index < Main.maxProjectiles;
        private static bool InTileBounds(int x, int y) => x >= 0 && x < Main.maxTilesX && y >= 0 && y < Main.maxTilesY;
        /// <summary>
        /// 请求服务端破坏指定物块。
        /// <para><paramref name="noItem"/> 表示“掉落由谁负责”：调用方自己生成并同步掉落时传 <c>true</c>
        /// （范围挖掘类工具），希望服务端按原版掉落表生成时传 <c>false</c>。
        /// 原实现固定让服务端带掉落执行，于是客户端已经手工生成的掉落会与服务端重复。</para>
        /// </summary>
        public static void RequestServerKillTile(int x, int y, bool noItem)
        {
            if (Main.netMode == NetmodeID.SinglePlayer || !InTileBounds(x, y))
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.ServerKillTile);
            packet.Write(x);
            packet.Write(y);
            packet.Write(noItem);
            packet.Send();
        }
        /// <summary>
        /// 批量请求服务端破坏物块（范围挖掘工具使用）。
        /// <para>一次挥动可能命中上千格，逐格 <see cref="RequestServerKillTile"/> 会发出上千个独立包；
        /// 这里把坐标打包发送，并按 <see cref="MaxBatchKillTiles"/> 分片以免超出包长度上限。</para>
        /// </summary>
        public static void RequestServerKillTiles(IReadOnlyList<Point16> tiles, bool noItem = true)
        {
            if (Main.netMode == NetmodeID.SinglePlayer || tiles.Count <= 0)
            {
                return;
            }
            for (int start = 0; start < tiles.Count; start += MaxBatchKillTiles)
            {
                int count = Math.Min(MaxBatchKillTiles, tiles.Count - start);
                ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.ServerKillTiles);
                packet.Write(count);
                packet.Write(noItem);
                for (int i = 0; i < count; i++)
                {
                    packet.Write(tiles[start + i].X);
                    packet.Write(tiles[start + i].Y);
                }
                packet.Send();
            }
        }
        /// <summary>
        /// 请求服务端清空并破坏箱子。
        /// <para>箱子内容物由发起方处理（会合并进物质团），服务端只负责清掉箱子数据与方块；
        /// 不做这一步的话，服务端会保留箱子数据，客户端再收到方块同步时就会出现“幽灵箱子”。</para>
        /// </summary>
        public static void RequestChestBreak(int x, int y)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || !InTileBounds(x, y))
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.RequestChestBreak);
            packet.Write(x);
            packet.Write(y);
            packet.Send();
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
        /// <summary>请求服务端广播本地玩家的星空球体状态。</summary>
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
        /// <summary>请求服务端回传所有玩家的星空球体状态（进入世界时使用）。</summary>
        public static void RequestCosmicSphereStates()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }
            NewPacket(AvaritiaMod.SyncMessageType.RequestCosmicSphereStates).Send();
        }
        /// <summary>把本地玩家手持物品的形态同步给服务端（由服务端转发给其它客户端）。</summary>
        public static void SendItemMode(byte mode)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.SyncItemMode);
            packet.Write(mode);
            packet.Send();
        }
        /// <summary>请求服务端回传所有玩家已记录的物品形态（进入世界时使用）。</summary>
        public static void RequestItemModes()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }
            NewPacket(AvaritiaMod.SyncMessageType.RequestItemModes).Send();
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
                case AvaritiaMod.SyncMessageType.ServerKillTile:
                    HandleServerKillTile(reader, whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.ServerKillTiles:
                    HandleServerKillTiles(reader, whoAmI);
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
                case AvaritiaMod.SyncMessageType.SyncItemMode:
                    HandleItemModeRequest(reader, whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.RequestItemModes:
                    HandleRequestItemModes(whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.RequestChestBreak:
                    HandleRequestChestBreak(reader, whoAmI);
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
                case AvaritiaMod.SyncMessageType.SyncItemMode:
                    HandleItemModeBroadcast(reader);
                    break;
                case AvaritiaMod.SyncMessageType.RequestChestBreak:
                    HandleChestBreakBroadcast(reader);
                    break;
                case AvaritiaMod.SyncMessageType.BroadcastItemModes:
                    HandleItemModesSnapshot(reader);
                    break;
            }
        }
        // ---------------------------------------------------------------- 物块
        private static void HandleRequestChestBreak(BinaryReader reader, int whoAmI)
        {
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            if (!InTileBounds(x, y))
            {
                return;
            }
            //箱子内容物只有服务端拥有权威副本：某个箱子从未被任何客户端打开过时，
            //客户端本地那份就是空的。因此必须由服务端取内容 → 生成物质团（自动同步给所有客户端）
            //→ 破坏箱子；再把同一条消息转发给其它客户端做本地清理，避免留下幽灵箱子。
            AvaritiaBreakHelper.LootAndBreakChestOnServer(x, y);
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.RequestChestBreak);
            packet.Write(x);
            packet.Write(y);
            packet.Send(ignoreClient: whoAmI);
            NetMessage.SendTileSquare(-1, x, y, 2, 2);
        }
        /// <summary>服务端：写入客户端上传的工作台内容物，并转发给其它客户端。</summary>
        private static void HandleRequestWholeTable(BinaryReader reader, int whoAmI)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            if (!ValidPlayerIndex(whoAmI) || GetTable(pos) is not { } table)
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
            if (!ValidPlayerIndex(whoAmI) || GetTable(pos) is not { } table)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.BroadcastWholeTable);
            packet.Write(pos.X);
            packet.Write(pos.Y);
            table.NetSend(packet);
            packet.Send(whoAmI);
        }
        private static void HandleChestBreakBroadcast(BinaryReader reader)
        {
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            if (!InTileBounds(x, y))
            {
                return;
            }
            //服务端已经取走内容物并掉落，这里只做本地清理（清空箱子副本 + 破坏方块），不产出任何物品
            AvaritiaBreakHelper.ClearAndBreakChestAt(x, y);
        }
        private static void HandleServerKillTile(BinaryReader reader, int whoAmI)
        {
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            bool noItem = reader.ReadBoolean();
            // 原实现直接用包里的坐标调用 WorldGen，越界坐标会造成异常或破坏地形数据。
            if (!InTileBounds(x, y))
            {
                return;
            }
            // 只接受来自有效玩家、且目标在其附近（1600 像素≈100 格）的请求。
            // 工具本身的判定在客户端完成，这里只是拦住明显的伪造包。
            if (whoAmI >= 0)
            {
                if (!ValidPlayerIndex(whoAmI) || Main.player[whoAmI] is not { active: true } player || !WithinReach(player, x, y))
                {
                    return;
                }
            }
            WorldGen.KillWall(x, y);
            WorldGen.KillTile(x, y, noItem: noItem);
        }
        /// <summary>
        /// 批量破坏：语义与 <see cref="HandleServerKillTile"/> 相同，只是一次处理多格坐标。
        /// <para>越界坐标或超出玩家可达范围的格直接跳过，不影响同一包里其余坐标。</para>
        /// </summary>
        private static void HandleServerKillTiles(BinaryReader reader, int whoAmI)
        {
            int count = reader.ReadInt32();
            bool noItem = reader.ReadBoolean();
            if (count <= 0 || count > MaxBatchKillTiles)
            {
                return;
            }
            Player? player = null;
            if (whoAmI >= 0)
            {
                if (!ValidPlayerIndex(whoAmI) || Main.player[whoAmI] is not { active: true } sender)
                {
                    return;
                }
                player = sender;
            }
            for (int i = 0; i < count; i++)
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                if (!InTileBounds(x, y) || (player is not null && !WithinReach(player, x, y)))
                {
                    continue;
                }
                WorldGen.KillWall(x, y);
                WorldGen.KillTile(x, y, noItem: noItem);
            }
        }
        /// <summary>目标物块是否在玩家可达范围内（用于拦截明显的伪造包）。</summary>
        private static bool WithinReach(Player player, int x, int y)
        {
            float dx = Math.Abs(player.Center.X - (x * 16f + 8f));
            float dy = Math.Abs(player.Center.Y - (y * 16f + 8f));
            return dx <= MaxKillReach && dy <= MaxKillReach;
        }
        // ---------------------------------------------------------------- 合成台
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
            //用原版 TryGet 而不是直接查 ByPosition：它内部会经 TileObjectData.TopLeft 归一化坐标，
            //否则客户端上报的“放置坐标”不是左上角时服务端会找不到实体（内容物就传不过来）。
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
            if (GetCompressor(pos) is not { } compressor)
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
            if (GetCollector(pos) is not { } collector)
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
        // ---------------------------------------------------------------- 星空球体
        private static void HandleRequestCosmicSphere(BinaryReader reader, int whoAmI)
        {
            int playerIndex = reader.ReadInt32();
            bool suit = reader.ReadBoolean();
            bool active = reader.ReadBoolean();
            int startTime = reader.ReadInt32();
            ushort timer = reader.ReadUInt16();
            bool attack = reader.ReadBoolean();
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
        // ---------------------------------------------------------------- 手持物品形态（按玩家同步）
        private static void HandleItemModeRequest(BinaryReader reader, int whoAmI)
        {
            byte mode = reader.ReadByte();
            if (!ValidPlayerIndex(whoAmI))
            {
                return;
            }
            Main.player[whoAmI].GetModPlayer<AvaritiaPlayer>().HeldItemMode = mode;
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.SyncItemMode);
            packet.Write((byte)whoAmI);
            packet.Write(mode);
            packet.Send(ignoreClient: whoAmI);
        }
        private static void HandleItemModeBroadcast(BinaryReader reader)
        {
            int playerIndex = reader.ReadByte();
            byte mode = reader.ReadByte();
            if (!ValidPlayerIndex(playerIndex))
            {
                return;
            }
            Main.player[playerIndex].GetModPlayer<AvaritiaPlayer>().HeldItemMode = mode;
        }
        private static void HandleRequestItemModes(int whoAmI)
        {
            if (!ValidPlayerIndex(whoAmI))
            {
                return;
            }
            ModPacket response = NewPacket(AvaritiaMod.SyncMessageType.BroadcastItemModes);
            response.Write(Main.maxPlayers);
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player plr = Main.player[i];
                byte mode = plr is { active: true } ? plr.GetModPlayer<AvaritiaPlayer>().HeldItemMode : (byte)0;
                response.Write((byte)i);
                response.Write(mode);
            }
            response.Send(whoAmI);
        }
        private static void HandleItemModesSnapshot(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int playerIndex = reader.ReadByte();
                byte mode = reader.ReadByte();
                if (!ValidPlayerIndex(playerIndex))
                {
                    continue;
                }
                Main.player[playerIndex].GetModPlayer<AvaritiaPlayer>().HeldItemMode = mode;
            }
        }
    }
}