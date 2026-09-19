namespace AvaritiaMod.Common.AvaritiaUtils
{
    /// <summary>
    /// 放置端暂存的工作台内容物（按方块坐标）。
    /// <para>多人客户端放下工作台时本地实体还没建立，只能先记下来，等首次右键（实体就绪）时补写。</para>
    /// </summary>
    internal static class CraftingTablePending
    {
        private static readonly Dictionary<Point16, Item[,]> Items = [];
        public static void Set(Point16 position, Item[,] items) => Items[position] = items;
        public static bool TryTake(Point16 position, out Item[,]? items) => Items.Remove(position, out items);
        public static void Remove(Point16 position) => Items.Remove(position);
        /// <summary>清空暂存：换世界或卸载时调用（坐标在不同世界之间没有意义，残留会套用到新世界的同坐标方块上）。</summary>
        public static void Clear() => Items.Clear();
    }
}
