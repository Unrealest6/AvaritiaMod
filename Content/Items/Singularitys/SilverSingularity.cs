namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class SilverSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Silver;
        protected override int RequiredItemType => ItemID.SilverBar;
        public override int RequiredQuantity => 400;
    }
}