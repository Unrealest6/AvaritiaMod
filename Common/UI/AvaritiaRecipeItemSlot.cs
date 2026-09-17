namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 无尽贪婪合成配方槽位UI元素
    /// </summary>
    public sealed class AvaritiaRecipeItemSlot : UIElement
    {
        /// <summary>
        /// 结果物品实例
        /// </summary>
        private readonly Item _resultItem;
        /// <summary>
        /// 配方实例
        /// </summary>
        private readonly AvaritiaRecipe _recipe;
        /// <summary>
        /// 配方尺寸
        /// </summary>
        private readonly BoundedSize _size;
        /// <summary>
        /// 所属工作台界面（首次访问时沿父链解析并缓存）。
        /// </summary>
        private CraftingTableUI? _owner;
        /// <summary>
        /// 所属工作台界面。高亮状态由它统一持有，这样点击新配方时旧配方会自动失去高亮。
        /// </summary>
        private CraftingTableUI? Owner => _owner ??= Parent?.Parent?.Parent?.Parent as CraftingTableUI;
        /// <summary>
        /// 扫描玩家背包的前 50 个物品栏，统计每种物品的总数量及其在背包中的索引队列。
        /// 用于后续自动分配合成材料时快速查找和扣除物品。
        /// </summary>
        /// <returns>字典，键为物品类型 ID，值为该物品的总数量和对应的背包索引队列。</returns>
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
        /// <summary>
        /// 为每个配方槽位选择一种材料类型，使得在材料限制下能够合成的份数最多。
        /// 通过二分搜索最大可合成份数 K，并调用 <see cref="TryAllocate"/> 验证可行性。
        /// </summary>
        /// <param name="slotOptions">每个槽位可接受的材料选项列表，每项包含类型、单份需求和最大堆叠。</param>
        /// <param name="available">当前可用的材料总量，键为物品类型 ID，值为总数量。</param>
        /// <returns>成功时返回每个槽位分配的材料类型数组；若无法分配则返回 null。</returns>
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
        /// <summary>
        /// 在给定目标合成份数 K 下，尝试为每个槽位分配一种材料。
        /// 采用贪心策略：优先处理可选材料较少的槽位，并选择使各材料剩余容量比例最均衡的选项。
        /// </summary>
        /// <param name="slotOptions">每个槽位可接受的材料选项列表。</param>
        /// <param name="typeList">所有涉及的材料类型列表（去重）。</param>
        /// <param name="available">材料总量字典。</param>
        /// <param name="K">目标合成份数。</param>
        /// <param name="sortedIndices">按槽位选项数量升序排列的槽位索引数组。</param>
        /// <returns>成功时返回每个槽位分配的材料类型数组；若无法分配则返回 null。</returns>
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
        /// <summary>
        /// 从物品列表中取出指定数量的物品。优先从单个堆叠中扣除；若不足，则合并多个堆叠。
        /// 取出的物品以新的 <see cref="Item"/> 实例返回，同时更新源列表。
        /// </summary>
        /// <param name="source">物品来源列表。</param>
        /// <param name="amount">需要取出的数量。</param>
        /// <returns>取出的物品实例；若无法取出足够数量则返回 null。</returns>
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
        /// <summary>
        /// 将物品安全地放回玩家背包。优先合并到已有的相同堆叠中，其次寻找空位放入，
        /// 若背包已满则直接在玩家位置生成掉落物。
        /// </summary>
        /// <param name="item">要放回的物品实例。</param>
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
        /// <summary>
        /// 构造方法，初始化配方结果槽位，绑定指定的配方并设置 UI 尺寸。
        /// </summary>
        /// <param name="recipe">该槽位对应的合成配方。</param>
        public AvaritiaRecipeItemSlot(AvaritiaRecipe recipe)
        {
            _recipe = recipe;
            _resultItem = recipe.Result;
            _size = recipe.Size;
            Width.Set(72, 0f);
            Height.Set(72, 0f);
        }
        /// <summary>
        /// 绘制槽位背景和结果物品。若鼠标悬停则显示物品提示；若当前配方被高亮则使用高亮背景和放大效果。
        /// </summary>
        /// <param name="spriteBatch">用于绘制的 SpriteBatch 实例。</param>
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            Rectangle rect = GetDimensions().ToRectangle();
            if (IsMouseHovering && !_resultItem.IsAir)
            {
                Main.HoverItem = _resultItem.Clone();
                Main.hoverItemName = _resultItem.Name;
            }
            bool isHighlighted = ReferenceEquals(Owner?.HighlightedRecipeSlot, this);
            Texture2D bg = isHighlighted ? TextureAssets.InventoryBack14.Value : TextureAssets.InventoryBack8.Value;
            Color tint = isHighlighted ? Color.White : Color.White * 0.4f;
            AvaritiaUIUtils.DrawItemSlot(spriteBatch, _resultItem, rect.TopLeft(), bg, itemColor: tint, scale: isHighlighted ? 1.4f : 1.2f);
        }
        /// <summary>
        /// 处理左键单击：高亮当前配方（并自动取消上一个高亮），
        /// 并在合成台各槽位中显示该配方所需的材料列表。
        /// </summary>
        /// <param name="evt">鼠标事件参数。</param>
        public override void LeftClick(UIMouseEvent evt)
        {
            base.LeftClick(evt);
            if (Owner is not { } parent)
            {
                return;
            }
            parent.HighlightRecipe(this);
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
        /// <summary>
        /// 处理右键单击：若当前配方已高亮，则取消高亮并清除各槽位显示的材料列表。
        /// </summary>
        /// <param name="evt">鼠标事件参数。</param>
        public override void RightClick(UIMouseEvent evt)
        {
            base.RightClick(evt);
            if (Owner is not { } parent || !ReferenceEquals(parent.HighlightedRecipeSlot, this))
            {
                return;
            }
            parent.ClearRecipeHighlight(this);
            for (int x = 0; x < _size; x++)
            {
                for (int y = 0; y < _size; y++)
                {
                    parent.Slots?[x, y].ShowItems = [];
                }
            }
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        /// <summary>
        /// 处理左键双击：扫描背包，自动将配方所需的材料分配到合成台槽位中，并刷新配方列表。
        /// </summary>
        /// <param name="evt">鼠标事件参数。</param>
        public override void LeftDoubleClick(UIMouseEvent evt)
        {
            base.LeftDoubleClick(evt);
            Dictionary<int, (int total, Queue<int> indices)> backpack = ScanBackpack();
            DistributeIngredients(backpack);
            Recipe.FindRecipes();
        }
        /// <summary>
        /// 核心自动分配逻辑。收集合成台现有物品和背包中配方需要的材料，构建每个槽位的可选材料列表，
        /// 调用 <see cref="FindBestAssignment"/> 计算最佳分配方案，然后按最大可合成份数将材料分配到各槽位，
        /// 最后将剩余材料安全退回背包。
        /// </summary>
        /// <param name="backpack">已扫描的背包物品统计信息。</param>
        private void DistributeIngredients(Dictionary<int, (int total, Queue<int> indices)> backpack)
        {
            if (Owner is not { } parent)
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
        /// <summary>
        /// 将单一类型的材料按需求比例分配到多个槽位。分配时受每个槽位的最大堆叠限制，
        /// 并从提供的物品池中取出对应数量的物品放入槽位。
        /// </summary>
        /// <param name="type">材料类型 ID。</param>
        /// <param name="positions">需要该材料的槽位列表，包含坐标、单份需求和最大堆叠。</param>
        /// <param name="totalAvailable">该材料的总可用数量。</param>
        /// <param name="pool">该材料的物品池，用于实际取出物品。</param>
        private void SmartDistributeSingleType(int type, List<(int x, int y, int need, int maxStack)> positions, int totalAvailable, List<Item> pool)
        {
            if (Owner is not { } parent)
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