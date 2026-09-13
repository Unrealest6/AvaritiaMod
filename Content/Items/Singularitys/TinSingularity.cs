namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class TinSingularity : Singularity
    {
        protected override Color OverlayColor => new(129, 125, 93);
        protected override int RequiredItemType => ItemID.TinBar;
        public override int RequiredQuantity => 400;
    }
}