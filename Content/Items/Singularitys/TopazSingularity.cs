namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-黄玉：中子态素压缩机消耗 800 个黄玉产出，用于合成无尽催化剂。</summary>
    public sealed class TopazSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Yellow;
        protected override int RequiredItemType => ItemID.Topaz;
        public override int RequiredQuantity => 800;
    }
}