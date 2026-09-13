namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class ChlorophyteSingularity : Singularity
    {
        protected override Color OverlayColor => new(36, 97, 51);
        protected override int RequiredItemType => ItemID.ChlorophyteBar;
        public override int RequiredQuantity => 200;
    }
}