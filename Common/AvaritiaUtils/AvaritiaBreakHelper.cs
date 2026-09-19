namespace AvaritiaMod.Common.AvaritiaUtils
{
    /// <summary>
    /// 无尽贪婪自己的“掉落收纳”实现：把一次挖掘收到的掉落合并后装进物质团。
    /// </summary>
    public static class AvaritiaBreakHelper
    {
        /// <summary>
        /// 把一批掉落合并（同类同前缀的普通物品）后装入物质团，单个物质团上限 4096，超出的部分转入下一个物质团。
        /// <para><b>合并必须保留物品实例</b>：只合并普通可堆叠物品并沿用原实例，带实例数据的物品
        /// （工作台内容物、被改过的 maxStack、形态…）各自独立，否则进物质团时会因重建实例而丢失数据。</para>
        /// </summary>
        public static void SpawnAsClusters(List<Item> drops, Vector2 position)
        {
            List<Item> remaining = MergeStackableItems(drops);
            int x = (int)(position.X / 16f);
            int y = (int)(position.Y / 16f);
            //安全阀：正常一次挥动的物品量远小于此，纯粹是防止任何意外导致死循环
            for (int guard = 0; remaining.Count > 0 && guard < 512; guard++)
            {
                Item clusterItem = new(ModContent.ItemType<MatterCluster>());
                List<Item> leftovers = [];
                foreach (Item entry in remaining)
                {
                    if (MatterCluster.TryAdd(clusterItem, entry) is { IsAir: false, stack: > 0 } leftover)
                    {
                        leftovers.Add(leftover);
                    }
                }
                if (clusterItem.ModItem is not MatterCluster { CurrentTotal: > 0 })
                {
                    //一个都没装进去（例如物质团被禁用）：剩下的正常掉落，避免死循环
                    foreach (Item leftover in leftovers)
                    {
                        BreakHelper.SpawnDrop(x, y, leftover);
                    }
                    return;
                }
                SpawnClusterInWorld(clusterItem, position);
                remaining = leftovers;
            }
        }
        /// <summary>
        /// 把普通可堆叠物品按 <c>类型 + 前缀 + maxStack</c> 合并（沿用原实例，保住实例数据），
        /// 其余物品各自独立成一条。
        /// </summary>
        private static List<Item> MergeStackableItems(List<Item> drops)
        {
            List<Item> result = [];
            Dictionary<(int type, int prefix, int maxStack), Item> mergeTable = [];
            foreach (Item item in drops.Where(item => item is { type: > ItemID.None, stack: > 0 }))
            {
                if (!CanMergeIntoSingleStack(item))
                {
                    result.Add(item);
                    continue;
                }
                (int type, int prefix, int maxStack) key = (item.type, item.prefix, item.maxStack);
                if (mergeTable.TryGetValue(key, out Item? existing))
                {
                    existing.stack += item.stack;
                    continue;
                }
                mergeTable[key] = item;
                result.Add(item);
            }
            return result;
        }
        /// <summary>
        /// 生成一个物质团：内容物按物品实例存在 ModItem 上，<c>Clone()</c> 会带上；
        /// 生成后再显式搬一次，避免任何克隆路径出意外。
        /// </summary>
        private static void SpawnClusterInWorld(Item clusterItem, Vector2 position)
        {
            int index = Item.NewItem(clusterItem.GetSource_DropAsItem(), position, clusterItem);
            if (index < 0)
            {
                return;
            }
            if (clusterItem.ModItem is MatterCluster source && Main.item[index].ModItem is MatterCluster worldCluster)
            {
                worldCluster.CopyContentsFrom(source);
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index, 1f);
            }
        }
        /// <summary>
        /// 该物品能否和其它同类型同前缀的物品合并成“一堆”再进物质团；只允许<b>普通可堆叠物品</b>，
        /// 带实例数据（工作台内容物、形态）的物品一旦被合并或重建就只剩空壳。
        /// <para>物质团里的合并（<see cref="MatterCluster.Add"/>）也用它做门禁：合并会丢掉后一件，其数据随之丢失。</para>
        /// </summary>
        internal static bool CanMergeIntoSingleStack(Item item) =>
            item.maxStack == GetDefaultMaxStack(item.type) && item.ModItem switch
            {
                CraftingTableItem table => !table.HasContents,
                FrameItem frameItem => frameItem.Mode == 0,
                _ => true
            };

        /// <summary>类型 → 默认 maxStack 缓存（用于判断某件物品的 maxStack 是否被改过）。</summary>
        private static readonly Dictionary<int, int> DefaultMaxStackCache = [];
        /// <summary>清空缓存：模组重载后物品类型号会重排，旧缓存会让“能否合并”的判断出错。</summary>
        public static void ClearCache() => DefaultMaxStackCache.Clear();
        private static int GetDefaultMaxStack(int type)
        {
            if (DefaultMaxStackCache.TryGetValue(type, out int cached))
            {
                return cached;
            }
            int maxStack = new Item(type).maxStack;
            DefaultMaxStackCache[type] = maxStack;
            return maxStack;
        }
    }
}