namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-神圣：中子态素压缩机消耗 600 个神圣锭产出，用于合成无尽催化剂。</summary>
    public sealed class HallowedSingularity : Singularity
    {
        protected override Color OverlayColor => new(150, 133, 100);
        protected override int RequiredItemType => ItemID.HallowedBar;
        public override int RequiredQuantity => 600;
    }
}