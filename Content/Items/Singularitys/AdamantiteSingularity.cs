namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class AdamantiteSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Firebrick;
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.AdamantiteBar;
        public override int RequiredQuantity => 300;
    }
}