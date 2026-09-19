namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-红玉：中子态素压缩机消耗 600 个红玉产出，用于合成无尽催化剂。</summary>
    public sealed class RubySingularity : Singularity
    {
        protected override Color OverlayColor => Color.Red;
        protected override int RequiredItemType => ItemID.Ruby;
        public override int RequiredQuantity => 600;
    }
}