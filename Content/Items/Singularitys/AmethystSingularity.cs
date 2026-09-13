namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class AmethystSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Purple;
        protected override int RequiredItemType => ItemID.Amethyst;
        public override int RequiredQuantity => 800;
    }
}