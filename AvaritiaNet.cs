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
        /// <summary>
        /// 压缩机 / 收集器槽位的动作类型。
        /// <para>联机下客户端只发意图，具体数量一律由服务端按自己手里的真实数据结算，
        /// 因此这些动作本身不携带“结果数量”。</para>
        /// </summary>
        public enum CompressorAction : byte
        {
            /// <summary>把鼠标上的一叠放入（<c>Item</c> = 鼠标上的整叠；放不下的留在鼠标上）。</summary>
            Insert = 0,
            /// <summary>只放入一个（<c>Item</c> = 类型）。</summary>
            InsertOne = 1,
            /// <summary>全部取出到鼠标。</summary>
            TakeAll = 2,
            /// <summary>取出一半到鼠标。</summary>
            TakeHalf = 3,
            /// <summary>全部取出送背包（Shift+点击）。</summary>
            TakeAllToInventory = 4,
            /// <summary>全部丢弃（Ctrl+点击）。</summary>
            Trash = 5,
            /// <summary>交换：取出到鼠标并放入 <c>Item</c>。</summary>
            Swap = 6,
            /// <summary>请求服务端把当前状态补发给自己（打开界面时用，避免显示过期）。</summary>
            Resync = 7
        }
        /// <summary>请求对压缩机 / 收集器槽位执行一次动作（客户端 → 服务端）。</summary>
        public static void RequestCompressorAction(Point16 position, bool output, CompressorAction action, Item item)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.RequestCompressorAction);
            packet.Write(position.X);
            packet.Write(position.Y);
            packet.Write(output);
            packet.Write((byte)action);
            ItemIO.Send(item, packet, writeStack: true, writeFavorite: true);
            packet.Send();
        }
        /// <summary>回发包里的音效标记（音效只能在客户端放，服务端放了专用服务器上没人听得到）。</summary>
        public const byte SoundNone = 0, SoundGrab = 1, SoundTick = 2;
        /// <summary>
        /// 告诉请求者“这次动作服务端已经处理完了”。没有物品要交付时也要发，
        /// 客户端靠它解除“等待服务端结算”状态（否则只能等超时），并播放对应音效。
        /// </summary>
        private static void SendCompressorAck(int toClient, Point16 position, byte sound = SoundNone)
        {
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.SyncCompressorCursor);
            packet.Write(position.X);
            packet.Write(position.Y);
            packet.Write(false);
            packet.Write(false);
            packet.Write(sound);
            packet.Write(0);
            ItemIO.Send(new Item(), packet, writeStack: true, writeFavorite: true);
            packet.Send(toClient);
        }
        /// <summary>把“客户端应如何调整鼠标 / 背包里的物品”发回请求者（服务端 → 客户端）。</summary>
        /// <param name="toClient">目标客户端。</param>
        /// <param name="position">机器坐标（仅用于日志）。</param>
        /// <param name="removeFromMouse">true = 从鼠标扣除；false = 交付给玩家。</param>
        /// <param name="toInventory">交付时是否直接进背包（Shift 行为）；false = 放到鼠标上。</param>
        /// <param name="sound">客户端要播放的音效标记。</param>
        /// <param name="item">要扣除 / 交付的物品。</param>
        /// <param name="count">数量。</param>
        private static void SendCursorSync(int toClient, Point16 position, bool removeFromMouse, bool toInventory, byte sound, Item item, int count)
        {
            if (item.IsAir || count <= 0)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.SyncCompressorCursor);
            packet.Write(position.X);
            packet.Write(position.Y);
            packet.Write(removeFromMouse);
            packet.Write(toInventory);
            packet.Write(sound);
            packet.Write(count);
            ItemIO.Send(item, packet, writeStack: true, writeFavorite: true);
            packet.Send(toClient);
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
                Warn($"Dropped a packet with protocol version {version} (this build uses {AvaritiaMod.ProtocolVersion}); "
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
                case AvaritiaMod.SyncMessageType.RequestCompressorAction:
                    HandleCompressorAction(reader, whoAmI);
                    break;
                case AvaritiaMod.SyncMessageType.RequestCraft:
                    HandleRequestCraft(reader, whoAmI);
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
                case AvaritiaMod.SyncMessageType.SyncCompressorCursor:
                    HandleSyncCompressorCursor(reader);
                    break;
            }
        }
        // ---------------------------------------------------------------- 合成台
        /// <summary>
        /// 请求合成一次（<paramref name="repeat"/> = Shift 连合）。联机下材料消耗与产物数量
        /// 一律由服务端按自己槽位里的材料结算：每个客户端的槽位镜像都是一份副本，
        /// 各自判定、各自扣材料会让同一份材料合出多份产物。
        /// </summary>
        public static void RequestCraft(Point16 position, bool repeat, bool toInventory)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.RequestCraft);
            packet.Write(position.X);
            packet.Write(position.Y);
            packet.Write(repeat);
            packet.Write(toInventory);
            packet.Send();
        }
        /// <summary>服务端：按自己的材料结算一次合成，扣掉材料、把产物回发给请求者，并广播整台内容物。</summary>
        private static void HandleRequestCraft(BinaryReader reader, int whoAmI)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            bool repeat = reader.ReadBoolean();
            bool toInventory = reader.ReadBoolean();
            if (!CanEditBlockData(whoAmI, pos) || GetTable(pos) is not { } table || GetRequestPlayer(whoAmI) is not { } player)
            {
                SendCompressorAck(whoAmI, pos);
                return;
            }
            if (table.Items is not { } items || items.GetLength(0) <= 0)
            {
                SendCompressorAck(whoAmI, pos);
                return;
            }
            //服务端按自己的材料重新匹配配方：客户端的镜像可能已经过期
            int dim = items.GetLength(0);
            AvaritiaItemSlot[,] slots = new AvaritiaItemSlot[dim, dim];
            for (int x = 0; x < dim; x++)
            {
                for (int y = 0; y < dim; y++)
                {
                    AvaritiaItemSlot slot = new(x, y);
                    slot.SetItemSilently(items[x, y]);
                    slots[x, y] = slot;
                }
            }
            AvaritiaRecipe? recipe = AvaritiaRecipe.FindMatchingRecipe(slots);
            if (recipe is null)
            {
                SendCompressorAck(whoAmI, pos, SoundTick);
                return;
            }
            Item product = recipe.Result.Clone();
            if (repeat)
            {
                int max = Math.Max(1, recipe.GetCraftableCount(slots));
                product.stack = 0;
                for (int i = 0; i < max; i++)
                {
                    recipe = AvaritiaRecipe.FindMatchingRecipe(slots);
                    if (recipe is null)
                    {
                        break;
                    }
                    product.stack += recipe.Result.stack;
                    recipe.ConsumeIngredients(slots);
                }
            }
            else
            {
                recipe.ConsumeIngredients(slots);
            }
            if (product.stack <= 0)
            {
                SendCompressorAck(whoAmI, pos);
                return;
            }
            if (toInventory && AvaritiaUIUtils.FitAmount(player, product) < product.stack)
            {
                //Shift 合成要求产物能整个放进背包；放不下就这一次不合成（材料一点都不扣）
                SendCompressorAck(whoAmI, pos, SoundTick);
                return;
            }
            //材料被扣掉：写回实体并广播整台，让所有客户端（含请求方）看到真实材料
            for (int x = 0; x < dim; x++)
            {
                for (int y = 0; y < dim; y++)
                {
                    items[x, y] = slots[x, y].Item.Clone();
                }
            }
            ModPacket packet = NewPacket(AvaritiaMod.SyncMessageType.BroadcastWholeTable);
            packet.Write(pos.X);
            packet.Write(pos.Y);
            table.NetSend(packet);
            packet.Send();
            //产物交给请求者：Shift 合成直接进背包，否则放到鼠标上
            SendCursorSync(whoAmI, pos, removeFromMouse: false, toInventory, SoundGrab, product, product.stack);
            SendCompressorAck(whoAmI, pos);
        }
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
        /// <summary>方块中心是否在请求玩家的可编辑距离内（仅诊断用，与 <see cref="CanEditBlockData"/> 同一判据）。</summary>
        private static bool InReach(Player player, Point16 pos)
            => Math.Abs(player.Center.X - (pos.X * 16f + 8f)) <= MaxDataEditReach
                && Math.Abs(player.Center.Y - (pos.Y * 16f + 8f)) <= MaxDataEditReach;
        private static NeutroniumCompressorTileEntity? GetCompressor(Point16 pos)
            => TileEntity.ByPosition.TryGetValue(pos, out TileEntity? te) && te is NeutroniumCompressorTileEntity compressor ? compressor : null;
        /// <summary>
        /// 服务端结算一次压缩机槽位动作。
        /// <para>联机下客户端只发意图，取出 / 放入的数量都以服务端手里的真实数据为准：
        /// 客户端那份镜像会过期（输入槽每 3 tick 被消耗一次），任何“按客户端算出的数量写入”的请求
        /// 都会把已经消耗掉的部分重复算给玩家。</para>
        /// </summary>
        private static void HandleCompressorAction(BinaryReader reader, int whoAmI)
        {
            Point16 pos = new(reader.ReadInt16(), reader.ReadInt16());
            bool output = reader.ReadBoolean();
            CompressorAction action = (CompressorAction)reader.ReadByte();
            Item payload = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            if (!CanEditBlockData(whoAmI, pos) || GetRequestPlayer(whoAmI) is not { } player)
            {
                SendCompressorAck(whoAmI, pos);
                return;
            }
            NeutroniumCompressorTileEntity? compressor = GetCompressor(pos);
            NeutronCollectorTileEntity? collector = compressor is null ? GetCollector(pos) : null;
            if (compressor is null && collector is null)
            {
                SendCompressorAck(whoAmI, pos);
                return;
            }
            //收集器只有输出槽且只能取出；放入 / 交换一律拒绝（这些动作只对压缩机输入槽有意义）
            if (compressor is null && action is not (CompressorAction.TakeAll or CompressorAction.TakeHalf or CompressorAction.TakeAllToInventory))
            {
                SendCompressorAck(whoAmI, pos);
                return;
            }
            //服务端此刻的真实内容物（Item 是引用类型，槽位结构本身用局部变量承接，最后再写回）
            Item slot = compressor is not null
                ? output ? compressor.OutputItem : compressor.InputItem
                : collector!.OutputItem;
            Item before = slot.Clone();
            Item toMouse = new();          //需要交付给客户端鼠标的
            int removeFromMouse = 0;       //客户端应从鼠标上扣除的数量
            int toInventory = 0;           //直接放进背包的数量（Shift+点击）
            byte ackSound = SoundNone;     //没有交付物时这一次点击该播什么音
            switch (action)
            {
                case CompressorAction.Insert:
                case CompressorAction.InsertOne:
                    {
                        int want = action == CompressorAction.InsertOne ? 1 : payload.stack;
                        if (payload.IsAir || want <= 0)
                        {
                            break;
                        }
                        if (slot.IsAir)
                        {
                            slot = new Item(payload.type, 0);
                        }
                        //类型不符（客户端镜像过期时会出现）或已满：一个都不收，
                        //客户端只扣除 removeFromMouse（=0），物品自然仍留在它手上 —— 绝不能回发，否则重复。
                        if (slot.type != payload.type)
                        {
                            break;
                        }
                        int add = Math.Min(want, slot.maxStack - slot.stack);
                        if (add <= 0)
                        {
                            break;
                        }
                        slot.stack += add;
                        removeFromMouse = add;
                        break;
                    }
                case CompressorAction.Swap:
                    toMouse = slot.Clone();
                    slot = payload.IsAir ? new Item() : payload.Clone();
                    removeFromMouse = payload.IsAir ? 0 : payload.stack;
                    break;
                case CompressorAction.TakeAll:
                    toMouse = slot.Clone();
                    slot = new Item();
                    break;
                case CompressorAction.TakeHalf:
                    {
                        int half = (int)Math.Ceiling(slot.stack / 2d);
                        if (half <= 0)
                        {
                            break;
                        }
                        toMouse = new Item(slot.type, half);
                        slot.stack -= half;
                        if (slot.stack <= 0)
                        {
                            slot = new Item();
                        }
                        break;
                    }
                case CompressorAction.TakeAllToInventory:
                    {
                        //Shift+点击：只取走背包放得下的部分，剩下的留在机器里（不能先取走再放不下，那样就凭空消失了）
                        int fit = AvaritiaUIUtils.FitAmount(player, slot);
                        if (fit <= 0)
                        {
                            //背包装不下：这一次不取（原版 Shift+点击放不下就是不生效），给个提示音
                            ackSound = SoundTick;
                            break;
                        }
                        toInventory = fit;
                        slot.stack -= fit;
                        if (slot.stack <= 0)
                        {
                            slot = new Item();
                        }
                        break;
                    }
                case CompressorAction.Trash:
                    slot = new Item();
                    ackSound = SoundTick;
                    break;
                case CompressorAction.Resync:
                    if (compressor is not null)
                    {
                        compressor.SendWholeCompressor(whoAmI);
                    }
                    else
                    {
                        collector!.SendWholeCollector(whoAmI);
                    }
                    SendCompressorAck(whoAmI, pos);
                    return;
                default:
                    return;
            }
            //把结算结果写回服务端实体
            if (compressor is not null)
            {
                if (output)
                {
                    compressor.OutputItem = slot;
                }
                else
                {
                    compressor.InputItem = slot;
                }
                //主机与客户端同进程时，本进程的界面也要立刻刷新
                compressor.NeutroniumCompressorUI?.InputSlot?.SetItemSilently(compressor.InputItem);
                compressor.NeutroniumCompressorUI?.OutputSlot?.SetItemSilently(compressor.OutputItem);
            }
            else
            {
                collector!.OutputItem = slot;
                collector.NeutronCollectorUI?.OutputSlot?.SetItemSilently(collector.OutputItem);
            }
            //再把服务端的新状态广播出去（其它客户端与请求方都要看到真实值），最后回发鼠标调整
            ModPacket packet = NewPacket(compressor is not null
                ? AvaritiaMod.SyncMessageType.BroadcastCompressor
                : AvaritiaMod.SyncMessageType.BroadcastCollector);
            packet.Write(pos.X);
            packet.Write(pos.Y);
            if (compressor is not null)
            {
                compressor.NetSend(packet);
            }
            else
            {
                collector!.NetSend(packet);
            }
            packet.Send(ignoreClient: whoAmI);
            if (toInventory > 0)
            {
                //交给客户端自己写进背包：联机下玩家背包以客户端为准，服务端直接改写会被后续同步覆盖掉（物品看起来就消失了）
                Item give = new(before.type, toInventory);
                SendCursorSync(whoAmI, pos, removeFromMouse: false, toInventory: true, SoundGrab, give, toInventory);
            }
            if (removeFromMouse > 0)
            {
                //放入：整叠被收下 = Grab（与原版放入一致），只收下一部分 = MenuTick
                byte insertSound = action == CompressorAction.Swap
                    ? SoundNone //交换已经由交付那一条播过音，避免两声
                    : removeFromMouse >= payload.stack ? SoundGrab : SoundTick;
                SendCursorSync(whoAmI, pos, removeFromMouse: true, toInventory: false, insertSound, new Item(slot.type, removeFromMouse), removeFromMouse);
            }
            if (!toMouse.IsAir && toMouse.stack > 0)
            {
                SendCursorSync(whoAmI, pos, removeFromMouse: false, toInventory: false, SoundGrab, toMouse, toMouse.stack);
            }
            SendCompressorAck(whoAmI, pos, ackSound);
        }
        /// <summary>客户端：按服务端的结算结果调整鼠标 / 背包里的物品（放入时扣除、取出 / 合成时交付）。</summary>
        private static void HandleSyncCompressorCursor(BinaryReader reader)
        {
            reader.ReadInt16();
            reader.ReadInt16();
            bool removeFromMouse = reader.ReadBoolean();
            bool toInventory = reader.ReadBoolean();
            byte sound = reader.ReadByte();
            int count = reader.ReadInt32();
            Item item = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            //服务端已经结算完（无论有没有东西要交付），解除等待状态并播放音效
            CompressorInputSlot.ClearPending();
            if (sound == SoundGrab)
            {
                SoundEngine.PlaySound(SoundID.Grab);
            }
            else if (sound == SoundTick)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
            if (item.IsAir || count <= 0)
            {
                return;
            }
            //鼠标 + 背包的合计：放入应当正好 -count，取出应当正好 +count，多了就是交付重复
            if (removeFromMouse)
            {
                //放入：服务端接受了 count 个，从鼠标上扣掉这么多
                if (Main.mouseItem.type != item.type)
                {
                    return;
                }
                int removed = Math.Min(count, Main.mouseItem.stack);
                Main.mouseItem.stack -= removed;
                if (Main.mouseItem.stack <= 0)
                {
                    Main.mouseItem.TurnToAir();
                }
            }
            else
            {
                //交付：鼠标 → 背包 → 掉在脚下，逐个兜底。
                //每一步都必须把已交付的数量从 give 里扣掉，否则同一样物品会既给鼠标又给背包（复制）。
                item.stack = Math.Min(count, item.stack);
                if (toInventory)
                {
                    AvaritiaUIUtils.MoveItemToPlayerInventory(Main.LocalPlayer, item);
                }
                else if (Main.mouseItem.IsAir)
                {
                    Main.mouseItem = item.Clone();
                    item.TurnToAir();
                }
                else if (Main.mouseItem.type == item.type && Main.mouseItem.maxStack == item.maxStack)
                {
                    int space = Main.mouseItem.maxStack - Main.mouseItem.stack;
                    int add = Math.Min(space, Math.Max(0, item.stack));
                    Main.mouseItem.stack += add;
                    item.stack -= add;
                }
                if (item is { IsAir: false, stack: > 0 })
                {
                    AvaritiaUIUtils.MoveItemToPlayerInventory(Main.LocalPlayer, item);
                }
                if (item is { IsAir: false, stack: > 0 })
                {
                    Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_Misc("AvaritiaCompressor"), item, item.stack);
                    item.TurnToAir();
                }
            }
        }
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
