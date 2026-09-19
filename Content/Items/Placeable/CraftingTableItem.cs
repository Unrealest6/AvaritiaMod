namespace AvaritiaMod.Content.Items.Placeable
{
    public abstract class CraftingTableItem : ModItem
    {
        internal Item[,]? Items { get; private set; }
        protected abstract BoundedSize GridSize { get; }
        protected abstract int TileType { get; }
        protected abstract string TagMaxStack { get; }
        protected abstract string TagItems { get; }
        /// <summary>
        /// 本物品是否带有内容物。
        /// <para>带内容物时用 <c>maxStack = 1</c> 标记不可堆叠，但该标记会被 <c>Item.Clone()</c> 按 <c>SetDefaults</c> 重建物品时丢掉，
        /// 所以另存这个布尔值，存档与界面更新都以它为准。</para>
        /// </summary>
        private bool _hasContents;
        internal bool HasContents => _hasContents;
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
            _hasContents = false;
            Items = new Item[GridSize, GridSize];
            for (byte x = 0; x < GridSize; x++)
            {
                for (byte y = 0; y < GridSize; y++)
                {
                    Items[x, y] = new Item();
                }
            }
        }
        /// <summary>
        /// 带内容物的工作台物品必须让每个物品实例持有自己的 ModItem 实例。
        /// <para><c>CloneNewInstances</c> 默认为 <c>false</c> 时，<c>Item.Clone()</c> 用<b>默认构造函数</b>重建 ModItem，
        /// <see cref="Items"/> 会被丢空，工作台掉进物质团或从物质团倒出来都会丢掉里面存的物品。</para>
        /// </summary>
        protected override bool CloneNewInstances => true;
        /// <summary>
        /// 克隆物品时把 <see cref="Items"/> 复制过去，并补回 <see cref="Item.maxStack"/>（<c>Item.Clone()</c> 会把它重置）。
        /// </summary>
        public override ModItem Clone(Item newEntity)
        {
            CraftingTableItem clone = (CraftingTableItem)base.Clone(newEntity);
            clone._hasContents = _hasContents;
            if (Item is not null)
            {
                //Item.Clone()（含原版 Item.NewItem 内部的克隆）按 SetDefaults 重建物品并重置 maxStack，这里补回来以保住“带内容物 → 不可堆叠”这个标记
                newEntity.maxStack = Item.maxStack;
            }
            if (Items is not null)
            {
                clone.Items = new Item[Items.GetLength(0), Items.GetLength(1)];
                for (int x = 0; x < Items.GetLength(0); x++)
                {
                    for (int y = 0; y < Items.GetLength(1); y++)
                    {
                        clone.Items[x, y] = Items[x, y]?.Clone() ?? new Item();
                    }
                }
            }
            return clone;
        }
        /// <summary>写入内容物（工作台方块掉落时调用），并按“是否为空”设置 <see cref="Item.maxStack"/>。</summary>
        internal void SetContents(Item[,]? source)
        {
            int sourceWidth = source?.GetLength(0) ?? 0;
            int sourceHeight = source?.GetLength(1) ?? 0;
            _hasContents = false;
            Items = new Item[GridSize, GridSize];
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    Item cell = source is not null && x < sourceWidth && y < sourceHeight
                        ? source[x, y]?.Clone() ?? new Item()
                        : new Item();
                    Items[x, y] = cell;
                    if (cell is { IsAir: false, stack: > 0 })
                    {
                        _hasContents = true;
                    }
                }
            }
            //带内容物的工作台物品不可堆叠（否则两件内容物会被堆到一起而只保留一份数据）
            Item.maxStack = _hasContents ? 1 : Item.CommonMaxStack;
        }
        /// <summary>
        /// 带内容物的工作台物品<b>永远不与任何东西堆叠</b>（双向）。
        /// <para>原版按“目标那一堆”的 <c>maxStack</c> 判定堆叠：背包里已有空工作台时，带内容物那件会被并进去，
        /// 合并后槽位上留下的是<b>空工作台</b>的 ModItem，内容物当场消失。空工作台之间仍可正常堆叠。</para>
        /// </summary>
        public override bool CanStack(Item item)
            => !_hasContents && item.ModItem is not CraftingTableItem { HasContents: true };
        public override void UpdateInventory(Player player)
        {
            //保险：任何路径（克隆、世界掉落、其它模组搬运）把 maxStack 重置掉之后补回来
            if (_hasContents && Item.maxStack != 1)
            {
                Item.maxStack = 1;
            }
        }
        public override void SaveData(TagCompound tag)
        {
            tag[TagMaxStack] = Item.maxStack;
            if (!_hasContents || Items is null)
            {
                return;
            }
            Item?[] flat = new Item[GridSize * GridSize];
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    flat[x * GridSize + y] = Items[x, y];
                }
            }
            tag[TagItems] = flat;
        }
        public override void LoadData(TagCompound tag)
        {
            //按“存档里有没有内容物”判断，而不是按 maxStack：maxStack 可能被克隆路径重置成可堆叠，而内容物其实还在存档里
            if (!tag.ContainsKey(TagItems))
            {
                _hasContents = false;
                for (int x = 0; x < GridSize; x++)
                {
                    for (int y = 0; y < GridSize; y++)
                    {
                        Items?[x, y] = new Item();
                    }
                }
                return;
            }
            Item[] flat = tag.Get<Item[]>(TagItems);
            _hasContents = false;
            for (int x = 0; x < GridSize; x++)
            {
                for (int y = 0; y < GridSize; y++)
                {
                    int idx = x * GridSize + y;
                    Item cell = (idx < flat.Length ? flat[idx] : null) ?? new Item();
                    Items?[x, y] = cell;
                    if (cell is { IsAir: false, stack: > 0 })
                    {
                        _hasContents = true;
                    }
                }
            }
            if (_hasContents)
            {
                Item.maxStack = 1;
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
            _hasContents = false;
            for (byte x = 0; x < size; x++)
            {
                for (byte y = 0; y < size; y++)
                {
                    Item cell = ItemIO.Receive(reader, readStack: true, readFavorite: true);
                    Items[x, y] = cell;
                    if (cell is { IsAir: false, stack: > 0 })
                    {
                        _hasContents = true;
                    }
                }
            }
            if (_hasContents)
            {
                Item.maxStack = 1;
            }
        }
        /// <summary>把本物品保存的内容物写入工作台实体（尺寸以实体为准，物品数组更小时补空）。</summary>
        internal void ApplyTo(CraftingTableTileEntity entity) => entity.ApplyItems(Items);
    }
}
