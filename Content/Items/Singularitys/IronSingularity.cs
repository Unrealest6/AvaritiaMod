namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class IronSingularity : Singularity
    {
        protected override Color OverlayColor => Color.DimGray;
        protected override int RequiredItemType => ItemID.IronBar;
        public override int RequiredQuantity => 400;
    }
}