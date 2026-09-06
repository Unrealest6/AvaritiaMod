namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class MythrilSingularity : Singularity
    {
        protected override Color OverlayColor => new(22, 119, 125);
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.MythrilBar;
        public override int RequiredQuantity => 300;
    }
}