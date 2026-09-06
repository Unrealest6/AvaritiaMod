namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class GoldSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Gold;
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.GoldBar;
        public override int RequiredQuantity => 400;
    }
}