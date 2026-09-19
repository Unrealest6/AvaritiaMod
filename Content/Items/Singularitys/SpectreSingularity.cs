namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-幽灵：中子态素压缩机消耗 200 个幽灵锭产出，用于合成无尽催化剂。</summary>
    public sealed class SpectreSingularity : Singularity
    {
        protected override Color OverlayColor => new(90, 220, 255);
        protected override int RequiredItemType => ItemID.SpectreBar;
        public override int RequiredQuantity => 200;
    }
}