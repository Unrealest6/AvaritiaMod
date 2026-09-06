namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class RubySingularity : Singularity
    {
        protected override Color OverlayColor => Color.Red;
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.Ruby;
        public override int RequiredQuantity => 600;
    }
}