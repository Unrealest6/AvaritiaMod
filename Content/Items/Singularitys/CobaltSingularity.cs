namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class CobaltSingularity : Singularity
    {
        protected override Color OverlayColor => new(37, 118, 171);
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.CobaltBar;
        public override int RequiredQuantity => 300;
    }
}