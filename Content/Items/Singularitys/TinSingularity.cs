namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-锡：中子态素压缩机消耗 400 个锡锭产出，用于合成无尽催化剂。</summary>
    public sealed class TinSingularity : Singularity
    {
        protected override Color OverlayColor => new(129, 125, 93);
        protected override int RequiredItemType => ItemID.TinBar;
        public override int RequiredQuantity => 400;
    }
}