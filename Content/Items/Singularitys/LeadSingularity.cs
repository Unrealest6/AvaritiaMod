namespace AvaritiaMod.Content.Items.Singularitys
{
    public sealed class LeadSingularity : Singularity
    {
        protected override Color OverlayColor => new(62, 82, 114);
        protected override int RequiredItemType => ItemID.LeadBar;
        public override int RequiredQuantity => 400;
    }
}