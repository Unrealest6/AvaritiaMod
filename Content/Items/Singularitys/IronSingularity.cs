namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-铁：中子态素压缩机消耗 400 个铁锭产出，用于合成无尽催化剂。</summary>
    public sealed class IronSingularity : Singularity
    {
        protected override Color OverlayColor => Color.DimGray;
        protected override int RequiredItemType => ItemID.IronBar;
        public override int RequiredQuantity => 400;
    }
}