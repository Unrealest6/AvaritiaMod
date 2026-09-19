namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-钛金：中子态素压缩机消耗 300 个钛金锭产出，用于合成无尽催化剂。</summary>
    public sealed class TitaniumSingularity : Singularity
    {
        protected override Color OverlayColor => new(90, 90, 200);
        protected override int RequiredItemType => ItemID.TitaniumBar;
        public override int RequiredQuantity => 300;
    }
}