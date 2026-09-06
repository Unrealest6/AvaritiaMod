namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class SilverSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Silver;
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.SilverBar;
        public override int RequiredQuantity => 400;
    }
}