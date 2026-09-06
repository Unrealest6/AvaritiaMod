namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class CrimtaneSingularity : Singularity
    {
        protected override Color OverlayColor => new(54, 10, 27);
        protected override Color UnderlayColor => OverlayColor.AddRGB(-64);
        protected override int RequiredItemType => ItemID.CrimtaneBar;
        public override int RequiredQuantity => 600;
    }
}