namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-铂：中子态素压缩机消耗 400 个铂金锭产出，用于合成无尽催化剂。</summary>
    public sealed class PlatinumSingularity : Singularity
    {
        protected override Color OverlayColor => new(128, 151, 184);
        protected override int RequiredItemType => ItemID.PlatinumBar;
        public override int RequiredQuantity => 400;
    }
}