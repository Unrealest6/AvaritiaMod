using AvaritiaMod;

namespace AvaritiaMod.Common.UI
{
    public sealed class DoubleCompressedCraftingTableUI : CraftingTableUI
    {
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