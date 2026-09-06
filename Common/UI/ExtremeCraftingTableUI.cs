using AvaritiaMod;

namespace AvaritiaMod.Common.UI
{
    public sealed class ExtremeCraftingTableUI : CraftingTableUI
    {
        public ExtremeCraftingTableUI(CraftingTableTileEntity tileEntity) : base(tileEntity) { }
        protected override BoundedSize Size => 9;
        protected override Vector2 PanelSize => new(830, 528);
        protected override Vector2 ListSize => new(92, 480);
        protected override float ListHAlign => 0.72f;
        protected override float ScrollbarHAlign => 0.76f;
        protected override float ArmorHAlign => 0.785f;
        protected override float ArmorVAlign => 0.46f;
        protected override float createSlotHAlign => 0.975f;
        protected override int ScrollbarViewMin => 200;
        protected override int ScrollbarViewMax => 1000;
        protected override string TitleText => "";
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            Title?.SetText(Lang.GetItemNameValue(ModContent.ItemType<ExtremeCraftingTable>()).ApplyGradient("Rainbow"));
        }
    }
}