namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class CopperSingularity : Singularity
    {
        protected override Color OverlayColor => Color.SaddleBrown;
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.CopperBar;
        public override int RequiredQuantity => 400;
    }
}