namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class SpectreSingularity : Singularity
    {
        protected override Color OverlayColor => new(90, 220, 255);
        protected override int RequiredItemType => ItemID.SpectreBar;
        public override int RequiredQuantity => 200;
    }
}