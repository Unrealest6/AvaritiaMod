namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-钯金：中子态素压缩机消耗 300 个钯金锭产出，用于合成无尽催化剂。</summary>
    public sealed class PalladiumSingularity : Singularity
    {
        protected override Color OverlayColor => new(227, 52, 14);
        protected override int RequiredItemType => ItemID.PalladiumBar;
        public override int RequiredQuantity => 300;
    }
}