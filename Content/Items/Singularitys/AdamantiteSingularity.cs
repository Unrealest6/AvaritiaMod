namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-精金：中子态素压缩机消耗 300 个精金锭产出，用于合成无尽催化剂。</summary>
    public sealed class AdamantiteSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Firebrick;
        protected override int RequiredItemType => ItemID.AdamantiteBar;
        public override int RequiredQuantity => 300;
    }
}