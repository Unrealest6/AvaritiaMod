namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class TitaniumSingularity : Singularity
    {
        protected override Color OverlayColor => new(90, 90, 200);
        protected override int RequiredItemType => ItemID.TitaniumBar;
        public override int RequiredQuantity => 300;
    }
}