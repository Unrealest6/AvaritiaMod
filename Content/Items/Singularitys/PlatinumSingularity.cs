namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class PlatinumSingularity : Singularity
    {
        protected override Color OverlayColor => new(128, 151, 184);
        protected override int RequiredItemType => ItemID.PlatinumBar;
        public override int RequiredQuantity => 400;
    }
}