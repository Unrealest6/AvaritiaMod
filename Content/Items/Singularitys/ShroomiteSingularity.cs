namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class ShroomiteSingularity : Singularity
    {
        protected override Color OverlayColor => new(44, 26, 233);
        protected override int RequiredItemType => ItemID.ShroomiteBar;
        public override int RequiredQuantity => 200;
    }
}