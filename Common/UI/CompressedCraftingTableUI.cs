namespace AvaritiaMod.Common.UI
{
    public sealed class CompressedCraftingTableUI : CraftingTableUI
    {
        public CompressedCraftingTableUI(CraftingTableTileEntity tileEntity) : base(tileEntity) { }
        protected override BoundedSize Size => 3;
        protected override string TitleText => ModContent.GetModItem(ModContent.ItemType<CompressedCraftingTable>()).DisplayName.Value;
    }
}