namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-钨：中子态素压缩机消耗 400 个钨锭产出，用于合成无尽催化剂。</summary>
    public sealed class TungstenSingularity : Singularity
    {
        protected override Color OverlayColor => new(139, 175, 140);
        protected override int RequiredItemType => ItemID.TungstenBar;
        public override int RequiredQuantity => 400;
    }
}