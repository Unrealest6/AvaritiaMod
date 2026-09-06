namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class EmeraldSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Green;
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.Emerald;
        public override int RequiredQuantity => 800;
    }
}