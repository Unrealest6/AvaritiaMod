namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-铜：中子态素压缩机消耗 400 个铜锭产出，用于合成无尽催化剂。</summary>
    public sealed class CopperSingularity : Singularity
    {
        protected override Color OverlayColor => Color.SaddleBrown;
        protected override int RequiredItemType => ItemID.CopperBar;
        public override int RequiredQuantity => 400;
    }
}