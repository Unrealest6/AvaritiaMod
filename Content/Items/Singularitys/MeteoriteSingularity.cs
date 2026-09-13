namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class MeteoriteSingularity : Singularity
    {
        protected override Color OverlayColor => new(95, 71, 82);
        protected override int RequiredItemType => ItemID.MeteoriteBar;
        public override int RequiredQuantity => 100;
    }
}