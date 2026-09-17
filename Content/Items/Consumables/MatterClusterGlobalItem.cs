namespace AvaritiaMod.Content.Items.Consumables
{
    /// <summary>
    /// 物质团的内容物按<b>物品实例</b>保存。
    /// <para>原实现把 <c>items</c> / <c>currentTotal</c> 放在 ModItem（每个类型只有一个单例）上，
    /// 于是同屏/背包里的多个物质团共用同一份内容：生成第二个物质团时会把第一个的内容清空，
    /// 表现就是“掉了一地物质团，但加起来数量与箱子里的对不上”。</para>
    /// <para>与形态系统同样的做法：<see cref="InstancePerEntity"/> + 存档/同步都挂在物品实例上。</para>
    /// </summary>
    public sealed class MatterClusterGlobalItem : GlobalItem
    {
        /// <summary>单个物质团的容量上限。</summary>
        public const int Capacity = 4096;
        /// <summary>该实例内部保存的物品。</summary>
        public List<Item> Items { get; set; } = [];
        /// <summary>该实例已保存的物品总数。</summary>
        public int CurrentTotal { get; set; }
        public override bool InstancePerEntity => true;
        public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.ModItem is MatterCluster;
        private static readonly Stack<Item> DrawStack = new();
        /// <summary>
        /// 当前正在绘制背包图标的物质团实例。
        /// <para><c>ModItem.PreDrawInInventory</c> 的签名里没有 Item，而 GlobalItem 的同名钩子会先收到 Item，
        /// 因此在这里压栈、PostDraw 弹栈（与 EternalLib 的 FrameItemDrawContext 同一套做法）。</para>
        /// </summary>
        public static Item? CurrentDrawItem => DrawStack.Count > 0 ? DrawStack.Peek() : null;
        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DrawStack.Push(item);
            return true;
        }
        public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (DrawStack.Count > 0 && ReferenceEquals(DrawStack.Peek(), item))
            {
                DrawStack.Pop();
            }
        }
        /// <summary>取某个物质团实例的数据（不是物质团时返回 null）。</summary>
        public static MatterClusterGlobalItem? Get(Item? item)
            => item is not null && item.TryGetGlobalItem(out MatterClusterGlobalItem data) ? data : null;
        /// <summary>把 <paramref name="newItem"/> 装进指定物质团实例，返回装不下的剩余部分。</summary>
        public static Item TryAdd(Item clusterItem, Item newItem)
            => Get(clusterItem)?.Add(newItem) ?? newItem;
        /// <summary>把一个物品并入本物质团，返回装不下的剩余部分（空物品表示全部装下）。</summary>
        public Item Add(Item newItem)
        {
            if (newItem is null || newItem.IsAir || newItem.stack <= 0)
            {
                return new Item();
            }
            int remaining = newItem.stack;
            foreach (Item existing in Items)
            {
                if (existing.type != newItem.type || existing.prefix != newItem.prefix)
                {
                    continue;
                }
                int space = existing.maxStack - existing.stack;
                if (space <= 0)
                {
                    continue;
                }
                int take = Math.Min(space, remaining);
                take = Math.Min(take, Capacity - CurrentTotal);
                if (take <= 0)
                {
                    continue;
                }
                existing.stack += take;
                remaining -= take;
                CurrentTotal += take;
                if (remaining == 0)
                {
                    return new Item();
                }
            }
            while (remaining > 0)
            {
                int canAdd = Math.Min(remaining, newItem.maxStack);
                canAdd = Math.Min(canAdd, Capacity - CurrentTotal);
                if (canAdd <= 0)
                {
                    break;
                }
                Item entry = newItem.Clone();
                entry.stack = canAdd;
                Items.Add(entry);
                remaining -= canAdd;
                CurrentTotal += canAdd;
            }
            if (remaining <= 0)
            {
                return new Item();
            }
            Item remainder = newItem.Clone();
            remainder.stack = remaining;
            return remainder;
        }
        public override void SaveData(Item item, TagCompound tag)
        {
            if (Items.Count == 0)
            {
                return;
            }
            tag["Items"] = Items.Select(ItemIO.Save).ToList();
            tag["Total"] = CurrentTotal;
        }
        public override void LoadData(Item item, TagCompound tag)
        {
            Items = [];
            CurrentTotal = 0;
            if (!tag.ContainsKey("Items"))
            {
                return;
            }
            foreach (TagCompound entry in tag.GetList<TagCompound>("Items"))
            {
                Item restored = ItemIO.Load(entry);
                if (restored is not null && !restored.IsAir && restored.stack > 0)
                {
                    Items.Add(restored);
                }
            }
            CurrentTotal = tag.ContainsKey("Total") ? tag.GetInt("Total") : Items.Sum(entry => entry.stack);
        }
        public override void NetSend(Item item, BinaryWriter writer)
        {
            writer.Write((short)Items.Count);
            foreach (Item entry in Items)
            {
                ItemIO.Send(entry, writer, writeStack: true, writeFavorite: true);
            }
        }
        public override void NetReceive(Item item, BinaryReader reader)
        {
            Items = [];
            CurrentTotal = 0;
            short count = reader.ReadInt16();
            for (short i = 0; i < count; i++)
            {
                Add(ItemIO.Receive(reader, readStack: true, readFavorite: true));
            }
        }
    }
}
