namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class OrichalcumSingularity : Singularity
    {
        protected override Color OverlayColor => new(205, 30, 200);
        protected override int RequiredItemType => ItemID.OrichalcumBar;
        public override int RequiredQuantity => 300;
    }
}