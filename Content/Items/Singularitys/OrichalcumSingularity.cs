namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-山铜：中子态素压缩机消耗 300 个山铜锭产出，用于合成无尽催化剂。</summary>
    public sealed class OrichalcumSingularity : Singularity
    {
        protected override Color OverlayColor => new(205, 30, 200);
        protected override int RequiredItemType => ItemID.OrichalcumBar;
        public override int RequiredQuantity => 300;
    }
}