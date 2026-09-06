namespace AvaritiaMod.Common.UI
{
    public sealed class AvaritiaCreateItemSlot : AvaritiaOutputSlot
    {
        private AvaritiaRecipe? _cachedRecipe;
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Parent.Parent is not CraftingTableUI parent)
            {
                return;
            }
            _cachedRecipe = AvaritiaRecipe.FindMatchingRecipe(parent.Slots);
            if (_cachedRecipe is null)
            {
                Item = new Item();
                return;
            }
            Item = _cachedRecipe.Result.Clone();
            if (!Main.keyState.IsKeyDown(Keys.LeftShift))
            {
                return;
            }
            int maxCount = _cachedRecipe.GetCraftableCount(parent.Slots);
            Item.stack *= maxCount;
        }
        public override void LeftClick(UIMouseEvent evt)
        {
            if (Parent.Parent is CraftingTableUI parent)
            {
                if (Main.keyState.IsKeyDown(Keys.LeftShift))
                {
                    if (!CraftRepeatedly())
                    {
                        return;
                    }
                }
                else
                {
                    _cachedRecipe = AvaritiaRecipe.FindMatchingRecipe(parent.Slots);
                    if (_cachedRecipe is not null)
                    {
                        Item = _cachedRecipe.Result.Clone();
                        _cachedRecipe.ConsumeIngredients(parent.Slots);
                    }
                }
            }
            base.LeftClick(evt);
        }
        public override void RightClick(UIMouseEvent evt)
        {
            if (Parent.Parent is CraftingTableUI parent)
            {
                if (Main.keyState.IsKeyDown(Keys.LeftShift))
                {
                    if (!CraftRepeatedly())
                    {
                        return;
                    }
                }
                else
                {
                    _cachedRecipe = AvaritiaRecipe.FindMatchingRecipe(parent.Slots);
                    if (_cachedRecipe is not null)
                    {
                        Item = _cachedRecipe.Result.Clone();
                        _cachedRecipe.ConsumeIngredients(parent.Slots);
                    }
                }
            }
            base.RightClick(evt);
        }
        private bool CraftRepeatedly()
        {
            if (Parent.Parent is not CraftingTableUI parent)
            {
                return false;
            }
            int stack = 0;
            _cachedRecipe = AvaritiaRecipe.FindMatchingRecipe(parent.Slots);
            if (_cachedRecipe is null)
            {
                return false;
            }
            Item = _cachedRecipe.Result.Clone();
            int maxCount = _cachedRecipe.GetCraftableCount(parent.Slots);
            for (int i = 0; i < maxCount; i++)
            {
                _cachedRecipe = AvaritiaRecipe.FindMatchingRecipe(parent.Slots);
                if (_cachedRecipe == null)
                {
                    break;
                }
                stack += _cachedRecipe.Result.stack;
                _cachedRecipe.ConsumeIngredients(parent.Slots);
            }
            Item.stack = stack;
            return Item.stack > 0;
        }
    }
}