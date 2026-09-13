namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class RubySingularity : Singularity
    {
        protected override Color OverlayColor => Color.Red;
        protected override int RequiredItemType => ItemID.Ruby;
        public override int RequiredQuantity => 600;
    }
}