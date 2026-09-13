namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class TopazSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Yellow;
        protected override int RequiredItemType => ItemID.Topaz;
        public override int RequiredQuantity => 800;
    }
}