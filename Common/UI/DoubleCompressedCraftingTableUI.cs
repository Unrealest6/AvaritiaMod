namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 二重压缩工作台UI组件
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class DoubleCompressedCraftingTableUI : CraftingTableUI
    {
        /// <summary>
        /// 构造方法，使用反射构造
        /// </summary>
        /// <param name="tileEntity">工作台物块实体实例</param>
        public DoubleCompressedCraftingTableUI(CraftingTableTileEntity tileEntity) : base(tileEntity) { }
        protected override BoundedSize Size => 6;
        protected override Vector2 PanelSize => new(650, 364);
        protected override Vector2 ListSize => new(92, 320);
        protected override float ListHAlign => 0.65f;
        protected override float ScrollbarHAlign => 0.71f;
        protected override float ArmorHAlign => 0.735f;
        protected override float ArmorVAlign => 0.44f;
        protected override float createSlotHAlign => 0.97f;
        protected override int ScrollbarViewMin => 133;
        protected override int ScrollbarViewMax => 667;
        protected override string TitleText => ModContent.GetModItem(ModContent.ItemType<DoubleCompressedCraftingTable>()).DisplayName.Value;
    }
}