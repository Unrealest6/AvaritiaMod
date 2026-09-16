namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 压缩工作台UI组件
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class CompressedCraftingTableUI : CraftingTableUI
    {
        /// <summary>
        /// 构造方法，使用反射构造
        /// </summary>
        /// <param name="tileEntity">工作台物块实体实例</param>
        public CompressedCraftingTableUI(CraftingTableTileEntity tileEntity) : base(tileEntity) { }
        protected override BoundedSize Size => 3;
        protected override string TitleText => ModContent.GetModItem(ModContent.ItemType<CompressedCraftingTable>()).DisplayName.Value;
    }
}