namespace AvaritiaMod.Content.Items.Consumables
{
    public sealed class MatterCluster : ModItem
    {
        internal List<Item> items = [];
        internal int currentTotal;
        private List<string> ItemsValue => [.. items.Select(item => item.Name + " x" + item.stack)];
        public override string Texture => "AvaritiaMod/Content/Items/Consumables/MatterCluster1";
        public override void SetDefaults()
        {
            Item.rare = ModContent.RarityType<LightRedRarity>();
            Item.useTime = 7;
            Item.useAnimation = 8;
            Item.useStyle = ItemUseStyleID.Swing;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            foreach (TooltipLine line in tooltips.Where(line => line.Mod == "Terraria"))
            {
                line.Text = line.Name == "Tooltip0" ? items.Sum(item => item.stack) + "/4096 " + Language.GetTextValue("LegacyInterface.37") : line.Text;
                if (!Main.keyState.IsKeyDown(Keys.LeftShift) || ItemsValue.Count <= 0)
                {
                    continue;
                }
                for (int i = 0; i < 2; i++)
                {
                    if (line.Name != "Tooltip" + (i + 2))
                    {
                        continue;
                    }
                    if (ItemsValue.Count > i)
                    {
                        line.Text = ItemsValue[i];
                    }
                    else
                    {
                        line.Hide();
                    }
                }
            }
            if (Main.keyState.IsKeyDown(Keys.LeftShift) && ItemsValue.Count > 2)
            {
                for (int i = 2; i < ItemsValue.Count; i++)
                {
                    TooltipLine newLine = new(Mod, "MatterClusterItem" + i, ItemsValue[i]);
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
            if (items.Sum(item => item.stack) < 4096)
            {
                return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
            }
            Texture2D value = ModContent.Request<Texture2D>("AvaritiaMod/Content/Items/Consumables/MatterCluster2").Value;
            spriteBatch.Draw(value, position, frame, drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
            return false;
        }
        public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
        {
            if (items.Sum(item => item.stack) < 4096)
            {
                return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
            }
            Texture2D value = ModContent.Request<Texture2D>("AvaritiaMod/Content/Items/Consumables/MatterCluster2").Value;
            Rectangle frame = value.Frame();
            Vector2 vector = frame.Size() / 2f;
            Vector2 vector2 = new(Item.width / 2f - vector.X, Item.height - frame.Height);
            Vector2 vector3 = Item.position - Main.screenPosition + vector + vector2;
            spriteBatch.Draw(value, vector3, frame, lightColor, 0f, vector, scale, SpriteEffects.None, 0f);
            return false;
        }
        public override bool CanRightClick() => true;
        public override void RightClick(Player player)
        {
            if (items.Sum(item => item.stack) > 0)
            {
                foreach (Item item in items.Where(item => item.type != ItemID.None))
                {
                    player.QuickSpawnItem(item.GetSource_DropAsItem(), item.Clone(), item.stack);
                }
            }
            items = [];
        }
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse != 2)
            {
                return false;
            }
            RightClick(player);
            Item.TurnToAir();
            return true;
        }
        public Item TryAddItem(Item newItem)
        {
            if (newItem.IsAir)
            {
                return new Item();
            }
            int remaining = newItem.stack;
            foreach (Item existing in items)
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
                take = Math.Min(take, 4096 - currentTotal);
                if (take <= 0)
                {
                    continue;
                }
                existing.stack += take;
                remaining -= take;
                currentTotal += take;
                if (remaining == 0)
                {
                    return new Item();
                }
            }
            while (remaining > 0)
            {
                int maxStack = newItem.maxStack;
                int canAdd = Math.Min(remaining, maxStack);
                canAdd = Math.Min(canAdd, 4096 - currentTotal);
                if (canAdd <= 0)
                {
                    break;
                }
                Item newEntry = newItem.Clone();
                newEntry.stack = canAdd;
                items.Add(newEntry);
                remaining -= canAdd;
                currentTotal += canAdd;
            }
            if (remaining <= 0)
            {
                return new Item();
            }
            Item remainder = newItem.Clone();
            remainder.stack = remaining;
            return remainder;
        }
        public override bool OnPickup(Player player)
        {
            if (items.Count == 0 || currentTotal == 0)
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
            List<Item> itemsToAdd = [.. items];
            items.Clear();
            currentTotal = 0;
            foreach (MatterCluster cluster in existingClusters)
            {
                for (int i = itemsToAdd.Count - 1; i >= 0; i--)
                {
                    Item leftover = cluster.TryAddItem(itemsToAdd[i]);
                    if (leftover.IsAir || leftover.stack <= 0)
                    {
                        itemsToAdd.RemoveAt(i);
                    }
                    else
                    {
                        itemsToAdd[i] = leftover;
                    }
                }
                if (itemsToAdd.Count == 0)
                {
                    break;
                }
            }
            if (itemsToAdd.Count > 0)
            {
                items = itemsToAdd;
                currentTotal = items.Sum(item => item.stack);
                return true;
            }
            Item.active = false;
            SoundEngine.PlaySound(SoundID.MaxMana);
            return false;
        }
        public override void NetSend(BinaryWriter writer)
        {
            writer.Write((short)items.Count);
            foreach (Item item in items)
            {
                ItemIO.Send(item, writer, writeStack: true, writeFavorite: true);
            }
        }
        public override void NetReceive(BinaryReader reader)
        {
            short count = reader.ReadInt16();
            for (short i = 0; i < count; i++)
            {
                TryAddItem(ItemIO.Receive(reader, readStack: true, readFavorite: true));
            }
        }
    }
}