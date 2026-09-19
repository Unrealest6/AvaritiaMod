namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-金：中子态素压缩机消耗 400 个金锭产出，用于合成无尽催化剂。</summary>
    public sealed class GoldSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Gold;
        protected override int RequiredItemType => ItemID.GoldBar;
        public override int RequiredQuantity => 400;
    }
}