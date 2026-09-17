namespace AvaritiaMod.Content.TileEntities
{
    public abstract class CraftingTableTileEntity : ModTileEntity
    {
        public Item[,]? Items { get; set; }
        public abstract BoundedSize Size { get; }
        public abstract ushort TileType { get; }
        public virtual string ItemTag => FullName + "Items";
        public CraftingTableUI? CraftingTableUI { get; set; }
        internal StyleDimension[] Styles { get; private set; } = new StyleDimension[2];
        public static void SendSlotChange(Point16 tilePos, int x, int y, Item item)
            => AvaritiaNet.RequestSyncSlot(tilePos, x, y, item);
        protected CraftingTableTileEntity()
        {
            Items = new Item[Size, Size];
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    Items[x, y] = new Item();
                }
            }
        }
        public override bool IsTileValidForEntity(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            return tile.HasTile && tile.TileType == TileType;
        }
        /// <summary>
        /// 把一批内容物写入本实体（尺寸以实体为准，源数组更小时补空）。
        /// </summary>
        public void ApplyItems(Item[,]? source)
        {
            if (Items is null)
            {
                return;
            }
            int sourceWidth = source?.GetLength(0) ?? 0;
            int sourceHeight = source?.GetLength(1) ?? 0;
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    Items[x, y] = source is not null && x < sourceWidth && y < sourceHeight
                        ? source[x, y].Clone() ?? new Item()
                        : new Item();
                }
            }
        }
        /// <summary>内容物里是否至少有一件物品（用于避免用空数组覆盖已有数据）。</summary>
        public static bool HasAnyItem(Item[,]? source)
        {
            if (source is null)
            {
                return false;
            }
            foreach (Item entry in source)
            {
                if (entry is { IsAir: false, stack: > 0 })
                {
                    return true;
                }
            }
            return false;
        }
        public override void SaveData(TagCompound tag)
        {
            Item?[] flatItems = new Item[Size * Size];
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    flatItems[x * Size + y] = Items?[x, y].Clone();
                }
            }
            tag[ItemTag] = flatItems;
        }
        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey(ItemTag))
            {
                Item[] flatItems = tag.Get<Item[]>(ItemTag);
                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        int index = x * Size + y;
                        Items?[x, y] = index < flatItems.Length ? flatItems[index] : new Item();
                    }
                }
            }
            else
            {
                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        Items?[x, y] = new Item();
                    }
                }
            }
        }
        public override void NetSend(BinaryWriter writer)
        {
            if (Items is null)
            {
                Items = new Item[Size, Size];
                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        Items[x, y] = new Item();
                    }
                }
            }
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    ItemIO.Send(Items[x, y], writer, writeStack: true, writeFavorite: true);
                }
            }
        }
        public override void NetReceive(BinaryReader reader)
        {
            if (Items is null)
            {
                Items = new Item[Size, Size];
                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        Items[x, y] = new Item();
                    }
                }
            }
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    Items[x, y] = ItemIO.Receive(reader, readStack: true, readFavorite: true);
                }
            }
        }
    }
}