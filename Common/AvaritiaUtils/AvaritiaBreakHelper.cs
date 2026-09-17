namespace AvaritiaMod.Common.AvaritiaUtils
{
    /// <summary>
    /// 挖掘类工具（世界崩解之镐 / 星球吞噬之铲 / 自然之毁）的公共逻辑。
    /// <para>原实现把这套逻辑分别抄在 4 个文件里，并且每次取掉落都用
    /// <c>typeof(WorldGen).GetMethod("KillTile_GetItemDrops", …)</c> + <c>Invoke</c> 反射调用，
    /// 其中一处还把 <c>GetMethod</c> 写进了双层循环内部（一次挥动上千次反射查找）；
    /// “本地 KillTile + 手动发 ServerKillTile”也各自手写，导致有些分支忘了通知服务端。</para>
    /// </summary>
    public static class AvaritiaBreakHelper
    {
        /// <summary>缓存的原版掉落计算方法（原版为私有静态方法，只能反射调用，必须缓存）。</summary>
        private static readonly MethodInfo? KillTileGetItemDropsMethod =
            typeof(WorldGen).GetMethod("KillTile_GetItemDrops", BindingFlags.NonPublic | BindingFlags.Static);
        public static bool InBounds(int x, int y) => x >= 0 && x < Main.maxTilesX && y >= 0 && y < Main.maxTilesY;
        /// <summary>
        /// 把多格方块（箱子等）的任意一格换算成左上角坐标。
        /// <para>原版与 tModLoader 的多格判定都以左上角为准，帧坐标每 36 像素（2 格）为一组：
        /// 右列/下行的帧坐标不是 36 的整数倍，各减一格才是左上角。</para>
        /// </summary>
        public static Point16 GetMultiTileOrigin(Tile tile, int x, int y)
        {
            int originX = x;
            int originY = y;
            if (tile.TileFrameX % 36 != 0)
            {
                originX--;
            }
            if (tile.TileFrameY % 36 != 0)
            {
                originY--;
            }
            return new Point16(originX, originY);
        }
        /// <summary>
        /// 取指定物块的原版掉落（包含概率、条件与运气等全部判定）。
        /// </summary>
        /// <returns>是否存在有效掉落</returns>
        public static bool TryGetDrop(int x, int y, Tile tile, out int itemType, out int stack)
        {
            itemType = 0;
            stack = 0;
            if (KillTileGetItemDropsMethod is null || !InBounds(x, y))
            {
                return false;
            }
            // 参数表：x, y, tile, type, stack, prefix, size, noItem
            object[] parameters = [x, y, tile, 0, 0, 0, 0, false];
            KillTileGetItemDropsMethod.Invoke(null, parameters);
            itemType = (int)parameters[3];
            stack = (int)parameters[4];
            return itemType > 0 && stack > 0;
        }
        /// <summary>
        /// 破坏物块：本地立即破坏以保证手感，多人客户端同时通知服务端破坏，
        /// 掉落由调用方自己用 <see cref="TryGetDrop"/> + <see cref="SpawnDrop"/> 负责
        /// （服务端不会再生成一份）。
        /// </summary>
        public static void BreakTile(int x, int y, bool noItem = true)
        {
            if (!InBounds(x, y))
            {
                return;
            }
            //只有客户端才需要“请求服务端”；服务端自己执行即可，否则会把包广播回所有客户端
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                AvaritiaNet.RequestServerKillTile(x, y, noItem: true);
            }
            WorldGen.KillTile(x, y, noItem: noItem);
        }
        /// <summary>
        /// 破坏物块并交给原版掉落表处理（树木、草等需要原版特殊逻辑的方块）。
        /// <para>多人下由服务端负责生成并同步掉落，本地只做破坏表现，
        /// 避免客户端与服务端各掉一份；单人则直接走原版路径。</para>
        /// </summary>
        public static void BreakTileWithVanillaDrops(int x, int y)
        {
            if (!InBounds(x, y))
            {
                return;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                AvaritiaNet.RequestServerKillTile(x, y, noItem: false);
                WorldGen.KillTile(x, y, noItem: true);
                return;
            }
            WorldGen.KillTile(x, y);
        }
        /// <summary>生成掉落物，并在多人客户端上同步给其它玩家。</summary>
        public static void SpawnDrop(int x, int y, Item item)
        {
            if (item is not { stack: > 0 } || item.IsAir)
            {
                return;
            }
            int index = Item.NewItem(item.GetSource_DropAsItem(), new Vector2(x * 16f, y * 16f), item);
            if (index >= 0 && Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index, 1f);
            }
        }
        /// <summary>
        /// 把一批掉落合并（按类型 + 前缀）后装入物质团，单个物质团上限 4096，装不下的部分才散落。
        /// <para>原先这段逻辑在两个工具里各写了一遍，并且直接给 ModItem 单例上的
        /// <c>cluster.items</c> / <c>currentTotal</c> 赋值——同屏多个物质团会互相覆盖，
        /// 表现就是“掉了一地物质团但数量对不上”。现在按物品实例的数据操作
        /// （见 <see cref="MatterClusterGlobalItem"/>），并且用“逐团回填剩余”的方式循环，
        /// 既不会丢东西也不会死循环。</para>
        /// </summary>
        public static void SpawnAsClusters(List<Item> drops, Vector2 position)
        {
            Dictionary<(int type, int prefix), int> merged = [];
            foreach (Item item in drops)
            {
                if (item is null || item.type <= ItemID.None || item.stack <= 0)
                {
                    continue;
                }
                (int type, int prefix) key = (item.type, item.prefix);
                merged.TryAdd(key, 0);
                merged[key] += item.stack;
            }
            if (merged.Count == 0)
            {
                return;
            }
            List<Item> remainingItems = [];
            foreach (KeyValuePair<(int type, int prefix), int> kv in merged)
            {
                Item entry = new(kv.Key.type);
                entry.Prefix(kv.Key.prefix);
                entry.stack = kv.Value;
                remainingItems.Add(entry);
            }
            int x = (int)(position.X / 16f);
            int y = (int)(position.Y / 16f);
            //安全阀：正常一次挥动的物品量远小于此，纯粹是防止任何意外导致死循环
            for (int guard = 0; remainingItems.Count > 0 && guard < 512; guard++)
            {
                Item clusterItem = new(ModContent.ItemType<MatterCluster>());
                List<Item> nextRound = [];
                foreach (Item entry in remainingItems)
                {
                    Item leftover = MatterClusterGlobalItem.TryAdd(clusterItem, entry);
                    if (leftover is { IsAir: false, stack: > 0 })
                    {
                        nextRound.Add(leftover);
                    }
                }
                bool filled = MatterClusterGlobalItem.Get(clusterItem) is { CurrentTotal: > 0 };
                if (filled && MatterClusterGlobalItem.Get(clusterItem) is { } clusterData)
                {
                    //用 Type/Stack 版本生成：Item.NewItem 对传入的实例执行的是 Clone()，
                    //按物品实例保存的内容不保证会被复制过去，因此生成后显式写入世界物品实例。
                    int index = Item.NewItem(clusterItem.GetSource_DropAsItem(), position, clusterItem.type, clusterItem.stack);
                    if (index >= 0)
                    {
                        if (MatterClusterGlobalItem.Get(Main.item[index]) is { } worldData)
                        {
                            worldData.Items = clusterData.Items;
                            worldData.CurrentTotal = clusterData.CurrentTotal;
                        }
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index, 1f);
                        }
                    }
                }
                if (nextRound.Count == remainingItems.Count && !filled)
                {
                    //一个都没装进去（例如物质团被禁用）：剩下的正常掉落，避免死循环
                    foreach (Item leftover in nextRound)
                    {
                        SpawnDrop(x, y, leftover);
                    }
                    return;
                }
                remainingItems = nextRound;
            }
        }
        /// <summary>
        /// 取出一个箱子的内容物（返回内容物 + 箱子本体的掉落）并清空箱子物品栏。
        /// <para><b>必须真正清空：</b><see cref="Chest.DestroyChest"/> 在箱内还有物品时直接返回 false，
        /// 而 <c>WorldGen.KillTile</c> 会通过 <c>CheckTileBreakability2_ShouldTileSurvive</c> 调用它——
        /// 一旦失败就判定“该格应当存活”，箱子便永远挖不掉，且每次挥动都会把同一批物品再取一次。</para>
        /// </summary>
        private static List<Item> ExtractChestLoot(Chest chest, int chestX, int chestY)
        {
            List<Item> loot = [];
            Item[] slots = chest.item;
            for (int i = 0; i < slots.Length; i++)
            {
                Item slot = slots[i];
                if (slot is null || slot.IsAir || slot.stack <= 0)
                {
                    continue;
                }
                loot.Add(slot.Clone());
                //真正清空箱子物品栏（旧实现只快照，导致 DestroyChest 一直失败）
                slots[i] = new Item();
            }
            if (TryGetDrop(chestX, chestY, Framing.GetTileSafely(chestX, chestY), out int chestItemType, out int chestItemStack))
            {
                //箱子本体也并入掉落，最终合并成物质团
                loot.Add(new Item(chestItemType, chestItemStack));
            }
            return loot;
        }
        /// <summary>
        /// 取出箱子（内容物 + 箱子本体）并彻底破坏它；该位置没有箱子时返回 null。
        /// <para>单人 / 服务端：就地取内容并破坏，内容由调用方并入物质团。</para>
        /// <para>多人客户端：箱子内容物是<b>服务端权威数据</b>——箱子从未被任何客户端打开过时，
        /// 本地副本就是空的。因此这里绝不能在本地清空/破坏，只发请求让服务端取内容并生成物质团，
        /// 服务端随后会把“清空并破坏”的广播发回来做本地清理。</para>
        /// <para>同一个箱子会被最多 4 个相邻格子的判定命中，因此用
        /// <paramref name="processedChests"/> 在本次挖掘内去重。</para>
        /// </summary>
        public static List<Item>? TakeChestLoot(Tile tile, int x, int y, HashSet<int> processedChests)
        {
            if (!InBounds(x, y))
            {
                return null;
            }
            int chestIndex = FindChestIndex(tile, x, y);
            if (chestIndex < 0 || chestIndex >= Main.chest.Length || Main.chest[chestIndex] is not { } chest)
            {
                return null;
            }
            if (!processedChests.Add(chestIndex))
            {
                //同一个箱子本次挖掘已经处理过：返回空列表，表示“这里是箱子但不要再产出掉落”
                return [];
            }
            int chestX = chest.x;
            int chestY = chest.y;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                AvaritiaNet.RequestChestBreak(chestX, chestY);
                return [];
            }
            List<Item> loot = ExtractChestLoot(chest, chestX, chestY);
            ClearAndBreakChest(chest, chestIndex, chestX, chestY, dropContents: false);
            return loot;
        }
        /// <summary>
        /// 服务端侧：取出箱子内容物并破坏箱子，再把内容生成物质团
        /// （服务端生成的物品会由原版自动同步给所有客户端）。
        /// <para>多人下必须由服务端来完成“取内容 + 掉落”：客户端在从未打开过该箱子时
        /// 本地副本是空的，靠客户端取内容会造成“箱子被破坏但物品全部丢失”。</para>
        /// </summary>
        public static void LootAndBreakChestOnServer(int x, int y)
        {
            if (!InBounds(x, y))
            {
                return;
            }
            Tile tile = Framing.GetTileSafely(x, y);
            int chestIndex = FindChestIndex(tile, x, y);
            if (chestIndex < 0 || chestIndex >= Main.chest.Length || Main.chest[chestIndex] is not { } chest)
            {
                return;
            }
            int chestX = chest.x;
            int chestY = chest.y;
            List<Item> loot = ExtractChestLoot(chest, chestX, chestY);
            ClearAndBreakChest(chest, chestIndex, chestX, chestY, dropContents: false);
            SpawnAsClusters(loot, new Vector2(chestX * 16f + 16f, chestY * 16f + 16f));
        }
        /// <summary>按帧坐标（多格以左上角为准）定位箱子下标；找不到返回 -1。</summary>
        public static int FindChestIndex(Tile tile, int x, int y)
        {
            Point16 origin = GetMultiTileOrigin(tile, x, y);
            int chestIndex = Chest.FindChest(origin.X, origin.Y);
            if (chestIndex < 0)
            {
                //兜底：帧坐标不是标准 2x2 时（例如别的模组的箱子）按邻近格子猜一次
                chestIndex = Chest.FindChestByGuessing(x, y);
            }
            return chestIndex;
        }
        /// <summary>本地清理：清空箱子副本并破坏方块（不产出掉落，内容物由服务端负责）。</summary>
        public static void ClearAndBreakChestAt(int x, int y)
        {
            if (!InBounds(x, y))
            {
                return;
            }
            Tile tile = Framing.GetTileSafely(x, y);
            int chestIndex = FindChestIndex(tile, x, y);
            if (chestIndex < 0 || chestIndex >= Main.chest.Length || Main.chest[chestIndex] is not { } chest)
            {
                return;
            }
            ClearAndBreakChest(chest, chestIndex, chest.x, chest.y, dropContents: false);
        }
        /// <summary>
        /// 清空箱子物品栏并破坏箱子占用的多格物块。
        /// </summary>
        /// <param name="chest">箱子实例</param>
        /// <param name="chestIndex">箱子在 <see cref="Main.chest"/> 中的下标</param>
        /// <param name="dropContents">是否把内容物掉落到世界中（调用方不想自己处理掉落时用）</param>
        public static void ClearAndBreakChest(Chest chest, int chestIndex, int chestX, int chestY, bool dropContents)
        {
            Item[] slots = chest.item;
            for (int i = 0; i < slots.Length; i++)
            {
                Item slot = slots[i];
                if (slot is null || slot.IsAir || slot.stack <= 0)
                {
                    continue;
                }
                if (dropContents)
                {
                    SpawnDrop(chestX, chestY, slot.Clone());
                }
                slots[i] = new Item();
            }
            //箱内已空，DestroyChest 这次才会真的清掉箱子数据
            if (!Chest.DestroyChest(chestX, chestY) && chestIndex >= 0 && chestIndex < Main.chest.Length)
            {
                //兜底：直接清掉数据槽，避免箱子一直处于“不可破坏”的状态
                Main.chest[chestIndex] = null;
            }
            BreakChestTiles(chestX, chestY);
        }
        /// <summary>
        /// 破坏箱子占用的多格物块。
        /// <para>必须以<b>左上角</b>坐标调用（原版多格判定按左上角为准）；
        /// 箱子数据已清空后 <c>KillTile</c> 才会真正破坏它，子方块由原版的多格逻辑一并处理，
        /// 余下若还有残留再按各自左上角补一次。</para>
        /// </summary>
        public static void BreakChestTiles(int originX, int originY)
        {
            BreakTile(originX, originY);
            for (int dx = 0; dx < 2; dx++)
            {
                for (int dy = 0; dy < 2; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }
                    int x = originX + dx;
                    int y = originY + dy;
                    if (!InBounds(x, y) || !Framing.GetTileSafely(x, y).HasTile)
                    {
                        continue;
                    }
                    BreakTile(x, y);
                }
            }
        }
    }
}
