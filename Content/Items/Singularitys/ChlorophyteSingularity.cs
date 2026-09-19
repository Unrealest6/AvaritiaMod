namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-叶绿：中子态素压缩机消耗 200 个叶绿锭产出，用于合成无尽催化剂。</summary>
    public sealed class ChlorophyteSingularity : Singularity
    {
        protected override Color OverlayColor => new(36, 97, 51);
        protected override int RequiredItemType => ItemID.ChlorophyteBar;
        public override int RequiredQuantity => 200;
    }
}