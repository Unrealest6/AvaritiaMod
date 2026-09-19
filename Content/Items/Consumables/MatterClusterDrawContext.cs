namespace AvaritiaMod.Content.Items.Consumables
{
    /// <summary>
    /// 背包绘制时把“正在被绘制的那个物品实例”压栈，供 <see cref="MatterCluster"/> 查询。
    /// <para><c>ModItem.PreDrawInInventory</c> 的签名里没有 <see cref="Item"/>，而 <c>GlobalItem</c> 的同名钩子能先收到 Item，
    /// 因此在这里压栈、PostDraw 弹栈；本类不保存任何数据，内容物存在 ModItem 实例上。</para>
    /// </summary>
    public sealed class MatterClusterDrawContext : GlobalItem
    {
        private static readonly Stack<Item> DrawStack = new();
        /// <summary>当前正在绘制背包图标的物质团实例。</summary>
        public static Item? CurrentInventoryItem => DrawStack.Count > 0 ? DrawStack.Peek() : null;
        public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.ModItem is MatterCluster;
        public override bool PreDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DrawStack.Push(item);
            return true;
        }
        public override void PostDrawInInventory(Item item, SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (DrawStack.Count > 0 && ReferenceEquals(DrawStack.Peek(), item))
            {
                DrawStack.Pop();
            }
        }
    }
}
