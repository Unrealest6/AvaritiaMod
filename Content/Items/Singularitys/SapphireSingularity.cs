namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class SapphireSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Blue;
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.Sapphire;
        public override int RequiredQuantity => 800;
    }
}