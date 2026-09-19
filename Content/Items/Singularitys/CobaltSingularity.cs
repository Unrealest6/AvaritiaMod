namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-钴：中子态素压缩机消耗 300 个钴锭产出，用于合成无尽催化剂。</summary>
    public sealed class CobaltSingularity : Singularity
    {
        protected override Color OverlayColor => new(37, 118, 171);
        protected override int RequiredItemType => ItemID.CobaltBar;
        public override int RequiredQuantity => 300;
    }
}