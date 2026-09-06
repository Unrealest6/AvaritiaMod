namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class TungstenSingularity : Singularity
    {
        protected override Color OverlayColor => new(139, 175, 140);
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.TungstenBar;
        public override int RequiredQuantity => 400;
    }
}