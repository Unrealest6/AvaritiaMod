namespace AvaritiaMod.Content.Items.Consumables
{
    /// <summary>
    /// 物质团：把挖掘产物压缩存放，右键（或左键使用）可以倒出全部内容。
    /// <para>内容物<b>直接存在 <see cref="ModItem"/> 实例上</b>，配合 <see cref="CloneNewInstances"/> + <see cref="Clone"/>
    /// 让每个物质团各持一份内容，不需要额外的 <c>GlobalItem</c> 中转。</para>
    /// </summary>
    public sealed class MatterCluster : ModItem
    {
        /// <summary>单个物质团的容量上限。</summary>
        public const int Capacity = 4096;
        public override string Texture => "AvaritiaMod/Content/Items/Consumables/MatterCluster1";
        /// <summary>本实例内部保存的物品。</summary>
        internal List<Item> Items { get; private set; } = [];
        /// <summary>本实例已保存的物品总数。</summary>
        internal int CurrentTotal { get; private set; }
        /// <summary>
        /// 每个物质团都要有独立的 ModItem 实例，内容才不会串味：
        /// <c>CloneNewInstances</c> 为默认的 false 时，<c>Item.Clone()</c> 会用默认构造函数重建 ModItem。
        /// </summary>
        protected override bool CloneNewInstances => true;
        /// <summary>克隆时带上内容物：丢到地上、进出背包、物质团合并都走克隆。</summary>
        public override ModItem Clone(Item newEntity)
        {
            MatterCluster clone = (MatterCluster)base.Clone(newEntity);
            clone.Items = [.. Items.Select(item => item.Clone(target => target.maxStack))];
            clone.CurrentTotal = CurrentTotal;
            return clone;
        }
        public override void SaveData(TagCompound tag)
        {
            if (Items.Count == 0)
            {
                return;
            }
            tag["Items"] = Items.Select(ItemIO.Save).ToList();
            tag["Total"] = CurrentTotal;
        }
        public override void LoadData(TagCompound tag)
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
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write((short)Items.Count);
            foreach (Item entry in Items)
            {
                ItemIO.Send(entry, writer, writeStack: true, writeFavorite: true);
            }
        }
        public override void NetReceive(BinaryReader reader)
        {
            Items = [];
            CurrentTotal = 0;
            short count = reader.ReadInt16();
            for (short i = 0; i < count; i++)
            {
                Add(ItemIO.Receive(reader, readStack: true, readFavorite: true));
            }
        }
        /// <summary>取正在被绘制的那个实例（PreDrawInInventory 拿不到 Item，由绘制上下文提供）。</summary>
        private MatterCluster DrawInstance
            => MatterClusterDrawContext.CurrentInventoryItem?.ModItem as MatterCluster ?? this;
        /// <summary>取 tooltip 对应的实例（优先用鼠标指向的那一个）。</summary>
        private MatterCluster HoverInstance
            => Main.HoverItem?.type == Type && Main.HoverItem.ModItem is MatterCluster hovered ? hovered : this;
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.rare = ModContent.RarityType<LightRedRarity>();
            Item.useTime = 7;
            Item.useAnimation = 8;
            Item.useStyle = ItemUseStyleID.Swing;
            Items = [];
            CurrentTotal = 0;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            List<Item> contents = HoverInstance.Items;
            List<string> values = [.. contents.Select(item => item.Name + " x" + item.stack)];
            foreach (TooltipLine line in tooltips.Where(line => line.Mod == "Terraria"))
            {
                line.Text = line.Name == "Tooltip0"
                    ? contents.Sum(item => item.stack) + "/" + Capacity + " " + Language.GetTextValue("LegacyInterface.37")
                    : line.Text;
                if (!Main.keyState.IsKeyDown(Keys.LeftShift) || values.Count <= 0)
                {
                    continue;
                }
                for (int i = 0; i < 2; i++)
                {
                    if (line.Name != "Tooltip" + (i + 2))
                    {
                        continue;
                    }
                    if (values.Count > i)
                    {
                        line.Text = values[i];
                    }
                    else
                    {
                        line.Hide();
                    }
                }
            }
            if (Main.keyState.IsKeyDown(Keys.LeftShift) && values.Count > 2)
            {
                for (int i = 2; i < values.Count; i++)
                {
                    TooltipLine newLine = new(Mod, "MatterClusterItem" + i, values[i]);
                    tooltips.Add(newLine);
                }
            }
            else
            {
                tooltips.RemoveAll(line => line.Name.StartsWith("MatterClusterItem"));
            }
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (DrawInstance.CurrentTotal < Capacity)
            {
                return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
            }
            Texture2D value = ModContent.Request<Texture2D>("AvaritiaMod/Content/Items/Consumables/MatterCluster2").Value;
            spriteBatch.Draw(value, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
            return false;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            Item worldItem = whoAmI >= 0 && whoAmI < Main.item.Length ? Main.item[whoAmI] : Item;
            MatterCluster world = worldItem.ModItem as MatterCluster ?? this;
            if (world.CurrentTotal < Capacity)
            {
                return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
            }
            Texture2D value = ModContent.Request<Texture2D>("AvaritiaMod/Content/Items/Consumables/MatterCluster2").Value;
            Rectangle worldFrame = value.Frame();
            Vector2 textureOrigin = worldFrame.Size() / 2f;
            Vector2 offset = new(worldItem.width / 2f - textureOrigin.X, worldItem.height - worldFrame.Height);
            Vector2 drawPosition = worldItem.position - Main.screenPosition + textureOrigin + offset;
            spriteBatch.Draw(value, drawPosition, worldFrame, lightColor, 0f, textureOrigin, scale, SpriteEffects.None, 0f);
            return false;
        }
        public override bool CanRightClick() => true;
        public override void RightClick(Player player) => DumpContents(player);
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2)
            {
                return false;
            }
            DumpContents(player);
            Item.TurnToAir();
            return true;
        }
        /// <summary>把内部物品全部倒给玩家并清空。</summary>
        private void DumpContents(Player player)
        {
            foreach (Item entry in Items.Where(entry => entry is { IsAir: false, stack: > 0 }))
            {
                //QuickSpawnItem 内部还会克隆一次并把 maxStack 重置，所以生成后把标记补回掉落物上
                int index = player.QuickSpawnItem(entry.GetSource_DropAsItem(), entry.Clone(target => target.maxStack), entry.stack);
                if (index >= 0 && index < Main.item.Length && Main.item[index] is { active: true } spawned)
                {
                    spawned.maxStack = entry.maxStack;
                }
            }
            Items = [];
            CurrentTotal = 0;
        }
        /// <summary>把一个物品并入本实例，返回装不下的剩余部分（空物品表示全部装下）。</summary>
        public Item Add(Item newItem)
        {
            if (newItem is null || newItem.IsAir || newItem.stack <= 0)
            {
                return new Item();
            }
            int remaining = newItem.stack;
            //带实例数据的物品绝不能并堆或拆开：合并是 existing.stack += take，会丢掉后一件的数据，所以判据要连物品自身携带的数据一起看，不能只看 maxStack
            bool canStack = AvaritiaBreakHelper.CanMergeIntoSingleStack(newItem);
            foreach (Item existing in Items)
            {
                if (existing.type != newItem.type || existing.prefix != newItem.prefix)
                {
                    continue;
                }
                if (!canStack || !AvaritiaBreakHelper.CanMergeIntoSingleStack(existing))
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
            //带实例数据的物品每件独占一个条目并强制 maxStack = 1，免得日后又被当成可堆叠物品并堆丢数据
            int perEntry = canStack ? newItem.maxStack : 1;
            while (remaining > 0)
            {
                int canAdd = Math.Min(remaining, perEntry);
                canAdd = Math.Min(canAdd, Capacity - CurrentTotal);
                if (canAdd <= 0)
                {
                    break;
                }
                Item entry = newItem.Clone(target => target.maxStack);
                entry.stack = canAdd;
                if (!canStack)
                {
                    entry.maxStack = 1;
                }
                Items.Add(entry);
                remaining -= canAdd;
                CurrentTotal += canAdd;
            }
            if (remaining <= 0)
            {
                return new Item();
            }
            Item remainder = newItem.Clone(target => target.maxStack);
            remainder.stack = remaining;
            return remainder;
        }
        /// <summary>把 <paramref name="newItem"/> 装进指定物质团实例，返回装不下的剩余部分。</summary>
        public static Item TryAdd(Item clusterItem, Item newItem)
            => clusterItem?.ModItem is MatterCluster cluster ? cluster.Add(newItem) : newItem;
        /// <summary>把另一个实例的内容物直接搬过来（生成世界掉落物后使用，避免克隆路径出意外）。</summary>
        internal void CopyContentsFrom(MatterCluster source)
        {
            Items = source.Items;
            CurrentTotal = source.CurrentTotal;
        }
        /// <summary>把一个物品并入本物品实例，返回装不下的剩余部分。</summary>
        public Item TryAddItem(Item newItem) => Add(newItem);
        public override bool OnPickup(Player player)
        {
            if (Items.Count == 0 || CurrentTotal == 0)
            {
                return true;
            }
            List<MatterCluster> existingClusters = [];
            foreach (Item invItem in player.inventory)
            {
                if (invItem?.type == Item.type && invItem.ModItem is MatterCluster cluster && cluster != this)
                {
                    existingClusters.Add(cluster);
                }
            }
            List<Item> pending = [.. Items];
            Items = [];
            CurrentTotal = 0;
            foreach (MatterCluster cluster in existingClusters)
            {
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    Item leftover = cluster.TryAddItem(pending[i]);
                    if (leftover.IsAir || leftover.stack <= 0)
                    {
                        pending.RemoveAt(i);
                    }
                    else
                    {
                        pending[i] = leftover;
                    }
                }
                if (pending.Count == 0)
                {
                    break;
                }
            }
            if (pending.Count > 0)
            {
                Items = pending;
                CurrentTotal = pending.Sum(entry => entry.stack);
                return true;
            }
            Item.active = false;
            SoundEngine.PlaySound(SoundID.MaxMana);
            return false;
        }
    }
}
