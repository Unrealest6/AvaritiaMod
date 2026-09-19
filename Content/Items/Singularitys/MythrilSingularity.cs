namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-秘银：中子态素压缩机消耗 300 个秘银锭产出，用于合成无尽催化剂。</summary>
    public sealed class MythrilSingularity : Singularity
    {
        protected override Color OverlayColor => new(22, 119, 125);
        protected override int RequiredItemType => ItemID.MythrilBar;
        public override int RequiredQuantity => 300;
    }
}