namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-银：中子态素压缩机消耗 400 个银锭产出，用于合成无尽催化剂。</summary>
    public sealed class SilverSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Silver;
        protected override int RequiredItemType => ItemID.SilverBar;
        public override int RequiredQuantity => 400;
    }
}