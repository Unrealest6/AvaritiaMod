namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-蘑菇：中子态素压缩机消耗 200 个蘑菇矿锭产出，用于合成无尽催化剂。</summary>
    public sealed class ShroomiteSingularity : Singularity
    {
        protected override Color OverlayColor => new(44, 26, 233);
        protected override int RequiredItemType => ItemID.ShroomiteBar;
        public override int RequiredQuantity => 200;
    }
}