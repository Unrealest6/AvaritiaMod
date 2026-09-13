namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class PalladiumSingularity : Singularity
    {
        protected override Color OverlayColor => new(227, 52, 14);
        protected override int RequiredItemType => ItemID.PalladiumBar;
        public override int RequiredQuantity => 300;
    }
}