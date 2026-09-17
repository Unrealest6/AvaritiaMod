namespace AvaritiaMod.Content.Items.Consumables
{
    /// <summary>
    /// 物质团：把挖掘产物压缩存放，右键（或左键使用）可以倒出全部内容。
    /// <para>内容物保存在物品实例上（<see cref="MatterClusterGlobalItem"/>），
    /// 因此同屏多个物质团互不影响，数量也不会“对不上”。</para>
    /// </summary>
    public sealed class MatterCluster : ModItem
    {
        public override string Texture => "AvaritiaMod/Content/Items/Consumables/MatterCluster1";
        /// <summary>本实例的内部物品（物品实例数据）。</summary>
        internal List<Item> items
        {
            get => Data?.Items ?? [];
            set
            {
                if (Data is { } data)
                {
                    data.Items = value;
                }
            }
        }
        /// <summary>本实例已保存的物品总数。</summary>
        internal int currentTotal
        {
            get => Data?.CurrentTotal ?? 0;
            set
            {
                if (Data is { } data)
                {
                    data.CurrentTotal = value;
                }
            }
        }
        /// <summary>取当前物品实例的数据。</summary>
        private MatterClusterGlobalItem? Data => MatterClusterGlobalItem.Get(Item);
        /// <summary>取背包绘制中的实例数据（PreDrawInInventory 拿不到 Item，由 GlobalItem 提供）。</summary>
        private MatterClusterGlobalItem? DrawData
            => MatterClusterGlobalItem.Get(MatterClusterGlobalItem.CurrentDrawItem) ?? Data;
        /// <summary>取 tooltip 对应实例的数据（优先用鼠标指向的那个物品）。</summary>
        private MatterClusterGlobalItem? HoverData
            => MatterClusterGlobalItem.Get(Main.HoverItem?.type == Type ? Main.HoverItem : Item) ?? Data;
        public override void SetDefaults()
        {
            Item.rare = ModContent.RarityType<LightRedRarity>();
            Item.useTime = 7;
            Item.useAnimation = 8;
            Item.useStyle = ItemUseStyleID.Swing;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            List<Item> contents = HoverData?.Items ?? [];
            List<string> values = [.. contents.Select(item => item.Name + " x" + item.stack)];
            foreach (TooltipLine line in tooltips.Where(line => line.Mod == "Terraria"))
            {
                line.Text = line.Name == "Tooltip0"
                    ? contents.Sum(item => item.stack) + "/" + MatterClusterGlobalItem.Capacity + " " + Language.GetTextValue("LegacyInterface.37")
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
            if ((DrawData?.CurrentTotal ?? 0) < MatterClusterGlobalItem.Capacity)
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
            if ((MatterClusterGlobalItem.Get(worldItem)?.CurrentTotal ?? 0) < MatterClusterGlobalItem.Capacity)
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
            if (Data is not { } data)
            {
                return;
            }
            foreach (Item entry in data.Items.Where(entry => entry is { IsAir: false, stack: > 0 }))
            {
                player.QuickSpawnItem(entry.GetSource_DropAsItem(), entry.Clone(), entry.stack);
            }
            data.Items = [];
            data.CurrentTotal = 0;
        }
        /// <summary>把一个物品并入本物品实例，返回装不下的剩余部分。</summary>
        public Item TryAddItem(Item newItem) => MatterClusterGlobalItem.TryAdd(Item, newItem);
        public override bool OnPickup(Player player)
        {
            if (Data is not { } data || data.Items.Count == 0 || data.CurrentTotal == 0)
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
            List<Item> pending = [.. data.Items];
            data.Items = [];
            data.CurrentTotal = 0;
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
                data.Items = pending;
                data.CurrentTotal = pending.Sum(entry => entry.stack);
                return true;
            }
            Item.active = false;
            SoundEngine.PlaySound(SoundID.MaxMana);
            return false;
        }
    }
}