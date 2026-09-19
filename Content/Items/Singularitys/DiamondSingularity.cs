namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-钻石：中子态素压缩机消耗 600 个钻石产出，用于合成无尽催化剂。</summary>
    public sealed class DiamondSingularity : Singularity
    {
        protected override Color OverlayColor => new(155, 200, 202);
        protected override int RequiredItemType => ItemID.Diamond;
        public override int RequiredQuantity => 600;
    }
}