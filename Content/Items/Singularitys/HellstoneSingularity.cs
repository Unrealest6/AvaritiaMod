namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-狱石：中子态素压缩机消耗 200 个狱石锭产出，用于合成无尽催化剂。</summary>
    public sealed class HellstoneSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Sienna;
        protected override int RequiredItemType => ItemID.HellstoneBar;
        public override int RequiredQuantity => 200;
    }
}