namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class DemoniteSingularity : Singularity
    {
        protected override Color OverlayColor => Color.DarkSlateBlue;
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.DemoniteBar;
        public override int RequiredQuantity => 600;
    }
}