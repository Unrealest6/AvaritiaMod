namespace AvaritiaMod.Common.UI
{
    public sealed class AvaritiaRecipeItemSlot : UIElement
    {
        private static Item? _highlightedResult;
        private readonly Item _resultItem;
        private readonly AvaritiaRecipe _recipe;
        private readonly int _size;
        private static Dictionary<int, (int total, Queue<int> indices)> ScanBackpack()
        {
            Dictionary<int, (int total, Queue<int> indices)> result = [];
            for (int i = 0; i < 50; i++)
            {
                Item inv = Main.LocalPlayer.inventory[i];
                if (inv?.IsAir != false)
                {
                    continue;
                }
                if (!result.TryGetValue(inv.type, out (int total, Queue<int> indices) entry))
                {
                    entry = (0, new Queue<int>());
                    result[inv.type] = entry;
                }
                result[inv.type] = (entry.total + inv.stack, entry.indices);
                entry.indices.Enqueue(i);
            }
            return result;
        }
        private static int[]? FindBestAssignment(List<List<(int type, int need, int maxStack)>> slotOptions, IReadOnlyDictionary<int, int> available)
        {
            int slotCount = slotOptions.Count;
            if (slotCount == 0)
            {
                return [];
            }
            List<int> typeList = [];
            foreach (List<(int type, int need, int maxStack)> opt in slotOptions)
            {
                foreach ((int t, int _, int _) in opt)
                {
                    if (!typeList.Contains(t))
                    {
                        typeList.Add(t);
                    }
                }
            }
            foreach (List<(int type, int need, int maxStack)> opt in slotOptions)
            {
                if (!opt.Any(o => available.TryGetValue(o.type, out int cnt) && cnt >= o.need))
                {
                    return null;
                }
            }
            long low = 0, high = available.Values.Any() ? available.Values.Max() : 0;
            int[]? bestAssignment = null;
            int[] sortedIndices = [.. Enumerable.Range(0, slotCount).OrderBy(i => slotOptions[i].Count)];
            while (low <= high)
            {
                long mid = (low + high) / 2;
                if (mid == 0)
                {
                    low = mid + 1; continue;
                }
                int[]? assignment = TryAllocate(slotOptions, typeList, available, mid, sortedIndices);
                if (assignment != null)
                {
                    bestAssignment = assignment;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }
            return bestAssignment;
        }
        private static int[]? TryAllocate(List<List<(int type, int need, int maxStack)>> slotOptions, List<int> typeList, IReadOnlyDictionary<int, int> available, long K, int[] sortedIndices)
        {
            int slotCount = slotOptions.Count;
            int[] assignment = new int[slotCount];
            long[] capacities = new long[typeList.Count];
            for (int i = 0; i < typeList.Count; i++)
            {
                capacities[i] = available.GetValueOrDefault(typeList[i]) / K;
            }
            long[] used = new long[typeList.Count];
            foreach (int idx in sortedIndices)
            {
                List<(int type, int need, int maxStack)> options = slotOptions[idx];
                double bestScore = -1.0;
                int bestType = -1;
                long bestNeed = 0;
                foreach ((int type, int need, int maxStack) in options)
                {
                    if (need > maxStack)
                    {
                        continue;
                    }
                    int typeIdx = typeList.IndexOf(type);
                    if (typeIdx == -1)
                    {
                        continue;
                    }
                    long newUsed = used[typeIdx] + need;
                    if (newUsed > capacities[typeIdx])
                    {
                        continue;
                    }
                    double minRemainingRatio = double.MaxValue;
                    for (int i = 0; i < typeList.Count; i++)
                    {
                        long remaining = capacities[i] - (i == typeIdx ? newUsed : used[i]);
                        double ratio = (double)remaining / capacities[i];
                        if (ratio < minRemainingRatio)
                        {
                            minRemainingRatio = ratio;
                        }
                    }
                    if (!(minRemainingRatio > bestScore))
                    {
                        continue;
                    }
                    bestScore = minRemainingRatio;
                    bestType = type;
                    bestNeed = need;
                }
                if (bestType == -1)
                {
                    return null;
                }
                assignment[idx] = bestType;
                int tIdx = typeList.IndexOf(bestType);
                used[tIdx] += bestNeed;
            }
            return assignment;
        }
        private static Item? TakeFromList(List<Item> source, int amount)
        {
            if (source.Count == 0)
            {
                return null;
            }
            for (int i = 0; i < source.Count; i++)
            {
                Item item = source[i];
                if (item.stack < amount)
                {
                    continue;
                }
                if (item.stack == amount)
                {
                    source.RemoveAt(i);
                    return item;
                }
                item.stack -= amount;
                Item clone = item.Clone();
                clone.stack = amount;
                return clone;
            }
            Item? template = null;
            List<Item> toRemove = [];
            int collected = 0;
            for (int i = 0; i < source.Count && collected < amount; i++)
            {
                Item item = source[i];
                if (template == null)
                {
                    template = item;
                }
                int take = Math.Min(amount - collected, item.stack);
                collected += take;
                if (take == item.stack)
                {
                    toRemove.Add(item);
                }
                else
                {
                    item.stack -= take;
                }
            }
            foreach (Item rem in toRemove)
            {
                source.Remove(rem);
            }
            Item? result = template?.Clone();
            if (result == null)
            {
                return null;
            }
            result.stack = amount;
            return result;
        }
        private static void SafeReturnToInventory(Item item)
        {
            if (item.IsAir || item.stack <= 0)
            {
                return;
            }
            int maxStack = item.maxStack;
            while (item.stack > 0)
            {
                int toPut = Math.Min(item.stack, maxStack);
                Item part = item.Clone();
                part.stack = toPut;
                item.stack -= toPut;
                bool placed = false;
                for (int i = 0; i < 50; i++)
                {
                    Item? inv = Main.LocalPlayer.inventory[i];
                    if (inv?.IsAir != false || inv.type != part.type || inv.maxStack != part.maxStack || inv.stack >= inv.maxStack)
                    {
                        continue;
                    }
                    int space = inv.maxStack - inv.stack;
                    int add = Math.Min(space, part.stack);
                    inv.stack += add;
                    part.stack -= add;
                    if (part.stack > 0)
                    {
                        continue;
                    }
                    placed = true;
                    break;
                }
                if (!placed)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        if (Main.LocalPlayer.inventory[i]?.IsAir == false)
                        {
                            continue;
                        }
                        Main.LocalPlayer.inventory[i] = part.Clone();
                        placed = true;
                        part.TurnToAir();
                        SoundEngine.PlaySound(SoundID.Grab);
                        break;
                    }
                }
                if (placed && part.stack <= 0)
                {
                    continue;
                }
                Main.LocalPlayer.QuickSpawnItem(null, part, part.stack);
            }
        }
        public AvaritiaRecipeItemSlot(AvaritiaRecipe recipe)
        {
            _recipe = recipe;
            _resultItem = recipe.Result;
            _size = recipe.Size;
            _highlightedResult = null;
            Width.Set(72, 0f);
            Height.Set(72, 0f);
        }
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle rect = GetDimensions().ToRectangle();
            if (IsMouseHovering && !_resultItem.IsAir)
            {
                Main.HoverItem = _resultItem.Clone();
                Main.hoverItemName = _resultItem.Name;
            }
            bool isHighlighted = _highlightedResult?.type == _resultItem.type;
            Texture2D bg = isHighlighted ? TextureAssets.InventoryBack14.Value : TextureAssets.InventoryBack8.Value;
            Color tint = isHighlighted ? Color.White : Color.White * 0.4f;
            AvaritiaUIUtils.DrawItemSlot(spriteBatch, _resultItem, rect.TopLeft(), bg, itemColor: tint, scale: isHighlighted ? 1.4f : 1.2f);
        }
        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            if (Parent.Parent.Parent.Parent is not CraftingTableUI parent)
            {
                return;
            }
            _highlightedResult = _resultItem;
            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    IReadOnlyList<Item>? items = _recipe.GetIngredientList(x, y);
                    if (items is null)
                    {
                        continue;
                    }
                    parent.Slots?[x, y].ShowItems = items.Count > 0 ? [.. items] : [];
                }
            }
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            if (Parent.Parent.Parent.Parent is not CraftingTableUI parent)
            {
                return;
            }
            if (_highlightedResult?.type != _resultItem.type)
            {
                return;
            }
            _highlightedResult = null;
            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    parent.Slots?[x, y].ShowItems = [];
                }
            }
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        public override void LeftDoubleClick(UIMouseEvent evt)
        {
            base.LeftDoubleClick(evt);
            Dictionary<int, (int total, Queue<int> indices)> backpack = ScanBackpack();
            DistributeIngredients(backpack);
            Recipe.FindRecipes();
        }
        private void DistributeIngredients(Dictionary<int, (int total, Queue<int> indices)> backpack)
        {
            if (Parent.Parent.Parent.Parent is not CraftingTableUI parent)
            {
                return;
            }
            Dictionary<int, List<Item>> slotItemPool = [];
            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    AvaritiaItemSlot? slot = parent.Slots?[x, y];
                    if (slot?.Item.IsAir != false)
                    {
                        continue;
                    }
                    int type = slot.Item.type;
                    if (!slotItemPool.ContainsKey(type))
                    {
                        slotItemPool[type] = [];
                    }
                    slotItemPool[type].Add(slot.Item);
                    slot.Item = new Item();
                }
            }
            HashSet<int> neededTypes = [];
            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    IReadOnlyList<Item>? items = _recipe.GetIngredientList(x, y);
                    if (items is null)
                    {
                        continue;
                    }
                    foreach (Item ing in items)
                    {
                        neededTypes.Add(ing.type);
                    }
                }
            }
            Dictionary<int, List<Item>> backpackPool = [];
            foreach (KeyValuePair<int, (int total, Queue<int> indices)> kv in backpack)
            {
                int type = kv.Key;
                if (!neededTypes.Contains(type))
                {
                    continue;
                }
                Queue<int> indices = kv.Value.indices;
                if (!backpackPool.ContainsKey(type))
                {
                    backpackPool[type] = [];
                }
                int remaining = kv.Value.total;
                while (remaining > 0 && indices.Count > 0)
                {
                    int idx = indices.Peek();
                    Item? inv = Main.LocalPlayer.inventory[idx];
                    if (inv == null || inv.IsAir || inv.type != type)
                    {
                        indices.Dequeue(); continue;
                    }
                    int take = Math.Min(inv.stack, remaining);
                    Item? clone = inv.Clone();
                    clone.stack = take;
                    backpackPool[type].Add(clone);
                    inv.stack -= take;
                    remaining -= take;
                    if (inv.stack > 0)
                    {
                        continue;
                    }
                    inv.TurnToAir(); indices.Dequeue();
                }
            }
            Dictionary<int, List<Item>> totalPool = [];
            foreach (KeyValuePair<int, List<Item>> kv in slotItemPool)
            {
                totalPool[kv.Key] = kv.Value;
            }
            foreach (KeyValuePair<int, List<Item>> kv in backpackPool)
            {
                if (totalPool.ContainsKey(kv.Key))
                {
                    totalPool[kv.Key].AddRange(kv.Value);
                }
                else
                {
                    totalPool[kv.Key] = kv.Value;
                }
            }
            Dictionary<int, int> originalAvailable = [];
            foreach (KeyValuePair<int, List<Item>> kv in totalPool)
            {
                originalAvailable[kv.Key] = kv.Value.Sum(i => i.stack);
            }
            List<List<(int type, int need, int maxStack)>> slotOptions = [];
            List<(int x, int y)> slotCoords = [];
            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    IReadOnlyList<Item>? items = _recipe.GetIngredientList(x, y);
                    if (items is null)
                    {
                        continue;
                    }
                    List<(int type, int need, int maxStack)> options = [.. items.Select(req => (req.type, need: req.stack, maxStack: req.maxStack))];
                    if (options.Count <= 0)
                    {
                        continue;
                    }
                    slotOptions.Add(options);
                    slotCoords.Add((x, y));
                }
            }
            int[]? assignment = FindBestAssignment(slotOptions, originalAvailable);
            if (assignment != null)
            {
                Dictionary<int, int> totalNeeded = [];
                Dictionary<int, List<(int x, int y, int need, int maxStack)>> typeGroups = [];
                for (int i = 0; i < assignment.Length; i++)
                {
                    int type = assignment[i];
                    (int x, int y) = slotCoords[i];
                    IReadOnlyList<Item>? items = _recipe.GetIngredientList(x, y);
                    if (items is null)
                    {
                        continue;
                    }
                    Item req = items.First(r => r.type == type);
                    if (!typeGroups.ContainsKey(type))
                    {
                        typeGroups[type] = [];
                    }
                    typeGroups[type].Add((x, y, req.stack, req.maxStack));
                    totalNeeded[type] = totalNeeded.GetValueOrDefault(type) + req.stack;
                }
                int maxCrafts = int.MaxValue;
                foreach ((int type, int need) in totalNeeded)
                {
                    if (!originalAvailable.TryGetValue(type, out int avail))
                    {
                        maxCrafts = 0;
                        break;
                    }
                    maxCrafts = Math.Min(maxCrafts, avail / need);
                }
                maxCrafts = Math.Max(0, maxCrafts);
                if (maxCrafts == 0)
                {
                    List<(int type, int x, int y, int need, int maxStack)> allAssignments = [];
                    for (int i = 0; i < slotOptions.Count; i++)
                    {
                        List<(int type, int need, int maxStack)> options = slotOptions[i];
                        (int x, int y) = slotCoords[i];
                        if (options.Count == 1)
                        {
                            (int type, int need, int maxStack) opt = options[0];
                            allAssignments.Add((opt.type, x, y, opt.need, opt.maxStack));
                        }
                        else
                        {
                            (int type, int need, int maxStack) bestOption = options.Where(o => originalAvailable.TryGetValue(o.type, out int cnt) && cnt > 0)
                                .OrderByDescending(o => o.need).FirstOrDefault();
                            if (bestOption != default)
                            {
                                allAssignments.Add((bestOption.type, x, y, bestOption.need, bestOption.maxStack));
                            }
                            else
                            {
                                parent.Slots?[x, y].Item = new Item();
                            }
                        }
                    }
                    foreach (IGrouping<int, (int type, int x, int y, int need, int maxStack)> group in allAssignments.GroupBy(a => a.type))
                    {
                        int type = group.Key;
                        List<(int x, int y, int need, int maxStack)> positions = [.. group.Select(a => (a.x, a.y, a.need, a.maxStack))];
                        int totalAvailable = originalAvailable.GetValueOrDefault(type);
                        SmartDistributeSingleType(type, positions, totalAvailable, totalPool.GetValueOrDefault(type, []));
                    }
                }
                else
                {
                    foreach (KeyValuePair<int, List<(int x, int y, int need, int maxStack)>> group in typeGroups)
                    {
                        int type = group.Key;
                        List<(int x, int y, int need, int maxStack)> positions = group.Value;
                        int totalNeed = totalNeeded[type];
                        int totalToDistribute = totalNeed * maxCrafts;
                        SmartDistributeSingleType(type, positions, totalToDistribute, totalPool.GetValueOrDefault(type, []));
                    }
                }
            }
            else
            {
                List<(int type, int x, int y, int need, int maxStack)> allAssignments = [];
                for (int i = 0; i < slotOptions.Count; i++)
                {
                    List<(int type, int need, int maxStack)> options = slotOptions[i];
                    (int x, int y) = slotCoords[i];
                    if (options.Count == 1)
                    {
                        (int type, int need, int maxStack) = options[0];
                        allAssignments.Add((type, x, y, need, maxStack));
                    }
                    else
                    {
                        (int type, int need, int maxStack) bestOption = options.Where(o => originalAvailable.TryGetValue(o.type, out int cnt) && cnt > 0)
                            .OrderByDescending(o => o.need).FirstOrDefault();
                        if (bestOption != default)
                        {
                            allAssignments.Add((bestOption.type, x, y, bestOption.need, bestOption.maxStack));
                        }
                        else
                        {
                            parent.Slots?[x, y].Item = new Item();
                        }
                    }
                }
                foreach (IGrouping<int, (int type, int x, int y, int need, int maxStack)> group in allAssignments.GroupBy(a => a.type))
                {
                    int type = group.Key;
                    List<(int x, int y, int need, int maxStack)> positions = [.. group.Select(a => (a.x, a.y, a.need, a.maxStack))];
                    int totalAvailable = originalAvailable.GetValueOrDefault(type);
                    SmartDistributeSingleType(type, positions, totalAvailable, totalPool.GetValueOrDefault(type, []));
                }
            }
            foreach (Item? item in from items in totalPool.Values from item in items where item.stack > 0 select item)
            {
                SafeReturnToInventory(item);
            }
        }
        private void SmartDistributeSingleType(int type, List<(int x, int y, int need, int maxStack)> positions, int totalAvailable, List<Item> pool)
        {
            if (Parent.Parent.Parent.Parent is not CraftingTableUI parent)
            {
                return;
            }
            int slotCount = positions.Count;
            if (slotCount == 0 || totalAvailable <= 0)
            {
                return;
            }
            long totalNeed = positions.Sum(p => (long)p.need);
            int[] target = new int[slotCount];
            if (totalNeed == 0)
            {
                return;
            }
            long assignedSum = 0;
            for (int i = 0; i < slotCount; i++)
            {
                long ideal = totalAvailable * positions[i].need / totalNeed;
                target[i] = (int)Math.Min(ideal, positions[i].maxStack);
                assignedSum += target[i];
            }
            int remainder = totalAvailable - (int)assignedSum;
            List<int> order = [.. Enumerable.Range(0, slotCount).OrderByDescending(i => positions[i].need)];
            foreach (int i in order)
            {
                if (remainder <= 0)
                {
                    break;
                }
                int space = positions[i].maxStack - target[i];
                if (space <= 0)
                {
                    continue;
                }
                int add = Math.Min(remainder, space);
                target[i] += add;
                remainder -= add;
            }
            for (int i = 0; i < slotCount; i++)
            {
                if (target[i] <= 0)
                {
                    continue;
                }
                (int x, int y, _, _) = positions[i];
                Item? taken = TakeFromList(pool, target[i]);
                parent.Slots?[x, y].Item = taken ?? new Item(type, target[i]);
            }
        }
    }
}