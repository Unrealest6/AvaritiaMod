namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class HallowedSingularity : Singularity
    {
        protected override Color OverlayColor => new(150, 133, 100);
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.HallowedBar;
        public override int RequiredQuantity => 600;
    }
}