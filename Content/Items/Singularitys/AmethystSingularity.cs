namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class AmethystSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Purple;
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.Amethyst;
        public override int RequiredQuantity => 800;
    }
}