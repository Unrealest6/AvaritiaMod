namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class DiamondSingularity : Singularity
    {
        protected override Color OverlayColor => new(155, 200, 202);
        protected override int RequiredItemType => ItemID.Diamond;
        public override int RequiredQuantity => 600;
    }
}