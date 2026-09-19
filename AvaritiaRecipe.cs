namespace AvaritiaMod
{
    public sealed class AvaritiaRecipe
    {
        public delegate AvaritiaRecipe? orig_Register(AvaritiaRecipe recipe);
        public delegate AvaritiaRecipe? hook_Register(orig_Register orig, AvaritiaRecipe recipe);
        public static event hook_Register? Hook_Register;
        public static IReadOnlySet<AvaritiaRecipe> Recipes => _recipes;
        private static readonly HashSet<AvaritiaRecipe> _recipes = [];
        /// <summary>
        /// 匹配结果缓存。
        /// </summary>
        private static int[]? _cachedContents;
        private static int _cachedDim;
        private static AvaritiaRecipe? _cachedRecipe;
        private static bool _cacheValid;
        public static AvaritiaRecipe? FindMatchingRecipe(AvaritiaItemSlot[,]? slots)
        {
            if (slots is null)
            {
                return null;
            }
            int dim = slots.GetLength(0);
            if (_cacheValid && _cachedContents is not null && dim == _cachedDim && slots.GetLength(1) == dim
                && ContentsUnchanged(slots, _cachedContents, dim))
            {
                return _cachedRecipe;
            }
            byte size = (byte)Math.Sqrt(slots.Length);
            AvaritiaRecipe? result = null;
            foreach (AvaritiaRecipe recipe in _recipes)
            {
                if (recipe.Size == size && !recipe.Result.IsAir && recipe.Matches(slots))
                {
                    result = recipe;
                    break;
                }
            }
            _cachedContents = SnapshotContents(slots, dim);
            _cachedDim = dim;
            _cachedRecipe = result;
            _cacheValid = true;
            return result;
        }
        /// <summary>清空匹配缓存（配方集合发生变化时调用；可合成数量的缓存按槽位内容快照自校验，无需清）。</summary>
        public static void InvalidateCache()
        {
            _cachedContents = null;
            _cachedRecipe = null;
            _cacheValid = false;
        }
        /// <summary>清空所有跨会话的静态状态（模组卸载时调用：配方实例与缓存都会持有旧的物品类型号）。</summary>
        public static void ResetStatics()
        {
            InvalidateCache();
            _recipes.Clear();
            Hook_Register = null;
        }
        /// <summary>把槽位内容拍平成 [type, stack] 序列（仅用于缓存比较）。</summary>
        private static int[] SnapshotContents(AvaritiaItemSlot[,] slots, int dim)
        {
            int[] contents = new int[dim * slots.GetLength(1) * 2];
            int index = 0;
            for (int x = 0; x < dim; x++)
            {
                for (int y = 0; y < slots.GetLength(1); y++)
                {
                    Item item = slots[x, y].Item;
                    contents[index++] = item.type;
                    contents[index++] = item.stack;
                }
            }
            return contents;
        }
        /// <summary>逐格比较槽位内容是否与快照一致（配方只关心物品类型与堆叠数）。</summary>
        private static bool ContentsUnchanged(AvaritiaItemSlot[,] slots, int[] snapshot, int dim)
        {
            if (slots.GetLength(1) != dim || snapshot.Length != dim * dim * 2)
            {
                return false;
            }
            int index = 0;
            for (int x = 0; x < dim; x++)
            {
                for (int y = 0; y < dim; y++)
                {
                    Item item = slots[x, y].Item;
                    if (snapshot[index++] != item.type || snapshot[index++] != item.stack)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        public BoundedSize Size { get; }
        public Item Result { get; }
        public bool IsOrdered { get; }
        private List<Item>[,] _ingredients;
        public AvaritiaRecipe(int type, BoundedSize size, int stack = 1, bool isOrdered = true)
        {
            Item item = new(type, stack);
            Result = item;
            Size = size;
            IsOrdered = isOrdered;
            _ingredients = new List<Item>[Size, Size];
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    _ingredients[x, y] = [];
                }
            }
        }
        public AvaritiaRecipe? Register()
        {
            orig_Register register = (AvaritiaRecipe _) =>
            {
                if (_recipes.Any(recipe => recipe == this))
                {
                    return null;
                }
                _recipes.Add(this);
                //配方集合变了，匹配结果缓存需要失效
                InvalidateCache();
                return this;
            };
            return Hook_Register != null ? Hook_Register(register, this) : register.Invoke(this);
        }
        public Item? GetIngredient(int x, int y) => x < 0 || x >= Size || y < 0 || y >= Size || _ingredients[x, y].Count <= 0 ? null : _ingredients[x, y][0];
        public IReadOnlyList<Item>? GetIngredientList(int x, int y) => x < 0 || x >= Size || y < 0 || y >= Size ? null : _ingredients[x, y].AsReadOnly();
        public AvaritiaRecipe AddIngredient(int itemType, int stack = 1)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (_ingredients[x, y].Count > 0)
                    {
                        continue;
                    }
                    return AddIngredient(x, y, itemType, stack);
                }
            }
            throw new InvalidOperationException("There is no space in the recipe to add ingredients.");
        }
        public AvaritiaRecipe AddIngredient(int x, int y, int itemType, int stack = 1) => AddToSlot(x, y, itemType, stack);
        public AvaritiaRecipe AddIngredients(Ingredient ingredient)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (_ingredients[x, y].Count > 0)
                    {
                        continue;
                    }
                    return AddIngredients(x, y, ingredient);
                }
            }
            return this;
        }
        public AvaritiaRecipe AddIngredients(int x, int y, Ingredient ingredient)
        {
            foreach ((int, int) item in ingredient.items)
            {
                AddToSlot(x, y, item.Item1, item.Item2);
            }
            return this;
        }
        public AvaritiaRecipe AddIngredients(Ingredient?[] ingredients)
        {
            if (ingredients.Length > Size * Size)
            {
                throw new ArgumentOutOfRangeException(nameof(ingredients));
            }
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (ingredients.Length <= x + y * Size || ingredients[x + y * Size]?.items is not { } items)
                    {
                        continue;
                    }
                    foreach ((int, int) item in items)
                    {
                        if (item is { Item1: > 0, Item2: > 0 })
                        {
                            AddToSlot(x, y, item.Item1, item.Item2);
                        }
                    }
                }
            }
            return this;
        }
        public AvaritiaRecipe ClearIngredient(int x, int y)
        {
            _ingredients[x, y] = [];
            return this;
        }
        public AvaritiaRecipe ClearAllIngredient()
        {
            _ingredients = new List<Item>[Size, Size];
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    _ingredients[x, y] = [];
                }
            }
            return this;
        }
        public AvaritiaRecipe ReplaceIngredient(int x, int y, int itemType, int stack = 1)
        {
            _ingredients[x, y] = [];
            return AddToSlot(x, y, itemType, stack);
        }
        public AvaritiaRecipe ReplaceIngredients(int x, int y, Ingredient ingredient)
        {
            _ingredients[x, y] = [];
            return AddIngredients(x, y, ingredient);
        }
        public bool Matches(AvaritiaItemSlot[,]? slots) => IsOrdered ? MatchesOrdered(slots) : MatchesUnordered(slots);
        public int GetCraftableCount(AvaritiaItemSlot[,]? slots)
        {
            if (slots is null)
            {
                return 0;
            }
            //可合成数量同样按槽位内容快照缓存：无序配方要跑一次回溯搜索，而按住 Shift 时每帧都会调用
            int dim = slots.GetLength(0);
            if (_countContents is not null && dim == _countDim && ContentsUnchanged(slots, _countContents, dim))
            {
                return _cachedCraftCount;
            }
            int count = CalculateCraftableCount(slots);
            _countContents = SnapshotContents(slots, dim);
            _countDim = dim;
            _cachedCraftCount = count;
            return count;
        }
        private int[]? _countContents;
        private int _countDim;
        private int _cachedCraftCount;
        private int CalculateCraftableCount(AvaritiaItemSlot[,] slots)
        {
            if (!Matches(slots))
            {
                return 0;
            }
            int materialLimit;
            if (IsOrdered)
            {
                materialLimit = CalculateOrderedCraftCount(slots);
            }
            else
            {
                if (!FindBestUnorderedCombination(slots, out _, out int maxCrafts))
                {
                    return 0;
                }
                materialLimit = maxCrafts;
            }
            return Math.Min(materialLimit, Result.maxStack / Result.stack);
        }
        public void ConsumeIngredients(AvaritiaItemSlot[,]? slots)
        {
            if (slots is null)
            {
                return;
            }
            if (IsOrdered)
            {
                ConsumeOrdered(slots);
            }
            else
            {
                if (MatchesUnordered(slots))
                {
                    ConsumeUnordered(slots);
                }
            }
        }
        private AvaritiaRecipe AddToSlot(int x, int y, int itemType, int stack)
        {
            List<Item> list = _ingredients[x, y];
            Item? existing = list.FirstOrDefault(item => item.type == itemType);
            if (existing != null)
            {
                existing.stack += stack;
            }
            else
            {
                list.Add(new Item(itemType, stack));
            }
            return this;
        }
        private void ConsumeUnordered(AvaritiaItemSlot[,] slots)
        {
            if (!FindBestUnorderedCombination(slots, out Dictionary<int, int>? bestRequirement, out _) || bestRequirement == null)
            {
                return;
            }
            foreach ((int type, int need) in bestRequirement)
            {
                int remaining = need;
                List<(int x, int y, Item item)> candidates = [];
                for (int x = 0; x < slots.GetLength(0); x++)
                {
                    for (int y = 0; y < slots.GetLength(1); y++)
                    {
                        Item item = slots[x, y].Item;
                        if (!item.IsAir && item.type == type)
                        {
                            candidates.Add((x, y, item));
                        }
                    }
                }
                candidates.Sort((a, b) => a.item.stack.CompareTo(b.item.stack));
                foreach ((int _, int _, Item item) in candidates)
                {
                    if (remaining <= 0)
                    {
                        break;
                    }
                    int take = Math.Min(item.stack, remaining);
                    item.stack -= take;
                    remaining -= take;
                    if (item.stack <= 0)
                    {
                        item.TurnToAir();
                    }
                }
            }
        }
        private int CalculateOrderedCraftCount(AvaritiaItemSlot[,] slots)
        {
            int min = int.MaxValue;
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    List<Item> reqList = _ingredients[x, y];
                    if (reqList.Count == 0)
                    {
                        continue;
                    }
                    Item slotItem = slots[x, y].Item;
                    Item? match = reqList.FirstOrDefault(r => r.type == slotItem.type);
                    if (match == null)
                    {
                        return 0;
                    }
                    int possible = slotItem.stack / match.stack;
                    if (possible < min)
                    {
                        min = possible;
                    }
                }
            }
            return min == int.MaxValue ? 0 : min;
        }
        private bool FindBestUnorderedCombination(AvaritiaItemSlot[,] slots, out Dictionary<int, int>? bestRequirement, out int maxCrafts)
        {
            bestRequirement = null;
            maxCrafts = 0;
            int gridSize = slots.GetLength(0);
            HashSet<int> neededTypes = [];
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    foreach (Item req in _ingredients[x, y])
                    {
                        neededTypes.Add(req.type);
                    }
                }
            }
            Dictionary<int, int> stock = [];
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Item item = slots[x, y].Item;
                    if (!item.IsAir && neededTypes.Contains(item.type))
                    {
                        stock[item.type] = stock.GetValueOrDefault(item.type) + item.stack;
                    }
                }
            }
            List<(int x, int y, List<(int type, int need, int maxStack)> options)> recipeSlots = [];
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    List<Item> options = _ingredients[x, y];
                    if (options.Count > 0)
                    {
                        recipeSlots.Add((x, y, [.. options.Select(op => (op.type, op.stack, op.maxStack))]));
                    }
                }
            }
            recipeSlots.Sort((a, b) => a.options.Count.CompareTo(b.options.Count));
            List<(int type, int need)> currentSelection = [];
            Dictionary<int, int> currentReqs = [];
            int bestCrafts = 0;
            List<(int type, int need)> bestSelection = [];
            Backtrack(0);
            if (bestSelection.Count == 0)
            {
                return false;
            }
            bestRequirement = [];
            foreach ((int type, int need) in bestSelection)
            {
                bestRequirement[type] = bestRequirement.GetValueOrDefault(type) + need;
            }
            maxCrafts = bestCrafts;
            return true;
            void Backtrack(int index)
            {
                if (index == recipeSlots.Count)
                {
                    int crafts = int.MaxValue;
                    foreach (KeyValuePair<int, int> kv in currentReqs)
                    {
                        if (!stock.TryGetValue(kv.Key, out int avail) || avail < kv.Value)
                        {
                            return;
                        }
                        int possible = avail / kv.Value;
                        if (possible < crafts)
                        {
                            crafts = possible;
                        }
                    }
                    if (crafts <= bestCrafts)
                    {
                        return;
                    }
                    bestCrafts = crafts;
                    bestSelection = [.. currentSelection];
                    return;
                }
                (_, _, List<(int type, int need, int maxStack)> options) = recipeSlots[index];
                foreach ((int type, int need, int maxStack) in options.OrderByDescending(o => o.need))
                {
                    if (need > maxStack || !stock.ContainsKey(type))
                    {
                        continue;
                    }
                    int oldNeed = currentReqs.GetValueOrDefault(type);
                    int newNeed = oldNeed + need;
                    currentReqs[type] = newNeed;
                    currentSelection.Add((type, need));
                    Backtrack(index + 1);
                    currentSelection.RemoveAt(currentSelection.Count - 1);
                    if (oldNeed == 0)
                    {
                        currentReqs.Remove(type);
                    }
                    else
                    {
                        currentReqs[type] = oldNeed;
                    }
                }
            }
        }
        private bool MatchesUnordered(AvaritiaItemSlot[,]? slots)
        {
            if (slots is null)
            {
                return false;
            }
            int gridSize = slots.GetLength(0);
            HashSet<int> neededTypes = [];
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    foreach (Item req in _ingredients[x, y])
                    {
                        neededTypes.Add(req.type);
                    }
                }
            }
            List<(int x, int y, int type, int stack)> availableItems = [];
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Item item = slots[x, y].Item;
                    if (!item.IsAir && neededTypes.Contains(item.type))
                    {
                        availableItems.Add((x, y, item.type, item.stack));
                    }
                }
            }
            for (int rx = 0; rx < Size; rx++)
            {
                for (int ry = 0; ry < Size; ry++)
                {
                    List<Item> options = _ingredients[rx, ry];
                    if (options.Count == 0)
                    {
                        continue;
                    }
                    bool matched = false;
                    foreach (Item opt in options)
                    {
                        int need = opt.stack;
                        int idx = availableItems.FindIndex(g => g.type == opt.type && g.stack >= need);
                        if (idx == -1)
                        {
                            continue;
                        }
                        (int gx, int gy, int gtype, int gstack) = availableItems[idx];
                        availableItems.RemoveAt(idx);
                        if (gstack > need)
                        {
                            availableItems.Insert(idx, (gx, gy, gtype, gstack - need));
                        }
                        matched = true;
                        break;
                    }
                    if (!matched)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        private bool MatchesOrdered(AvaritiaItemSlot[,]? slots)
        {
            if (slots is null)
            {
                return false;
            }
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    List<Item> reqList = _ingredients[x, y];
                    Item slotItem = slots[x, y].Item;
                    bool slotEmpty = slotItem.IsAir;
                    bool reqEmpty = reqList.Count == 0;
                    if (slotEmpty && reqEmpty)
                    {
                        continue;
                    }
                    if (slotEmpty != reqEmpty)
                    {
                        return false;
                    }
                    Item? match = reqList.FirstOrDefault(r => r.type == slotItem.type);
                    if (match == null || slotItem.stack < match.stack)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        private void ConsumeOrdered(AvaritiaItemSlot[,] slots)
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    List<Item> reqList = _ingredients[x, y];
                    if (reqList.Count == 0)
                    {
                        continue;
                    }
                    Item slotItem = slots[x, y].Item;
                    Item match = reqList.First(r => r.type == slotItem.type);
                    slotItem.stack -= match.stack;
                    if (slotItem.stack <= 0)
                    {
                        slotItem.TurnToAir();
                    }
                }
            }
        }
    }
    public readonly record struct Ingredient(IEnumerable<(int, int)> items)
    {
        public static implicit operator Ingredient(int type) => new([(type, 1)]);
        public static implicit operator Ingredient((int, int) item) => new([(item.Item1, item.Item2)]);
        public static implicit operator Ingredient(Item item) => new([(item.type, item.stack)]);
        public static implicit operator Ingredient(List<int> items) => new(items.Select(item => (item, 1)));
        public static implicit operator Ingredient(List<(int, int)> items) => new(items.Select(item => (item.Item1, item.Item2)));
        public static implicit operator Ingredient(List<Item> items) => new(items.Select(item => (item.type, item.stack)));
    }
    public record struct BoundedSize
    {
        public const byte MinSize = 2;
        public const byte MaxSize = 15;
        private byte Size { get => Math.Clamp(field, MinSize, MaxSize); }
        private BoundedSize(byte size) => Size = size;
        public static implicit operator BoundedSize(byte size) => new(size);
        public static implicit operator byte(BoundedSize boundedSize) => boundedSize.Size;
    }
}