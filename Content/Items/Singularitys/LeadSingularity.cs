namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-铅：中子态素压缩机消耗 400 个铅锭产出，用于合成无尽催化剂。</summary>
    public sealed class LeadSingularity : Singularity
    {
        protected override Color OverlayColor => new(62, 82, 114);
        protected override int RequiredItemType => ItemID.LeadBar;
        public override int RequiredQuantity => 400;
    }
}