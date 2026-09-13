namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class HellstoneSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Sienna;
        protected override int RequiredItemType => ItemID.HellstoneBar;
        public override int RequiredQuantity => 200;
    }
}