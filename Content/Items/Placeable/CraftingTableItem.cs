namespace AvaritiaMod.Content.Items.Placeable
{
    public abstract class CraftingTableItem : ModItem
    {
        internal Item[,]? Items { get; private set; }
        protected abstract byte GridSize { get; }
        protected abstract int TileType { get; }
        protected abstract string TagMaxStack { get; }
        protected abstract string TagItems { get; }
        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 34;
            Item.maxStack = Item.CommonMaxStack;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = TileType;
            Items = new Item[GridSize, GridSize];
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    Items[x, y] = new Item();
                }
            }
        }
        public override void SaveData(TagCompound tag)
        {
            tag[TagMaxStack] = Item.maxStack;
            if (Item.maxStack != 1)
            {
                return;
            }
            Item?[] flat = new Item[GridSize * GridSize];
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    flat[x * GridSize + y] = Items?[x, y];
                }
            }
            tag[TagItems] = flat;
        }
        public override void LoadData(TagCompound tag)
        {
            int maxStack = tag.GetInt(TagMaxStack);
            if (maxStack == 1 && tag.ContainsKey(TagItems))
            {
                Item[] flat = tag.Get<Item[]>(TagItems);
                for (int x = 0; x < GridSize; x++)
                {
                    for (int y = 0; y < GridSize; y++)
                    {
                        int idx = x * GridSize + y;
                        Items?[x, y] = (idx < flat.Length ? flat[idx] : null) ?? new Item();
                    }
                }
                Item.maxStack = maxStack;
            }
            else
            {
                for (int x = 0; x < GridSize; x++)
                {
                    for (int y = 0; y < GridSize; y++)
                    {
                        Items?[x, y] = new Item();
                    }
                }
            }
        }
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(GridSize);
            for (byte x = 0; x < GridSize; x++)
            {
                for (byte y = 0; y < GridSize; y++)
                {
                    ItemIO.Send(Items?[x, y] ?? new Item(), writer, writeStack: true, writeFavorite: true);
                }
            }
        }
        public override void NetReceive(BinaryReader reader)
        {
            byte size = reader.ReadByte();
            Items = new Item[size, size];
            for (byte x = 0; x < size; x++)
            {
                for (byte y = 0; y < size; y++)
                {
                    Items[x, y] = ItemIO.Receive(reader, readStack: true, readFavorite: true);
                }
            }
        }
    }
}