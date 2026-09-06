namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class AmberSingularity : Singularity
    {
        protected override Color OverlayColor => new(191, 82, 0);
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.Amber;
        public override int RequiredQuantity => 600;
    }
}