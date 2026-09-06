namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class TopazSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Yellow;
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.Topaz;
        public override int RequiredQuantity => 800;
    }
}