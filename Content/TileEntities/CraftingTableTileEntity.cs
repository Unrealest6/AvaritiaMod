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
        {
            ModPacket packet = ModContent.GetInstance<AvaritiaMod>().GetPacket();
            packet.Write((byte)AvaritiaMod.SyncMessageType.SyncSlot);
            packet.Write(tilePos.X);
            packet.Write(tilePos.Y);
            packet.Write((byte)x);
            packet.Write((byte)y);
            ItemIO.Send(item, packet, writeStack: true, writeFavorite: true);
            packet.Send();
        }
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