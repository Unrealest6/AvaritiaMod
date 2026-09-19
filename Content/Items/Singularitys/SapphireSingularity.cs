namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-蓝宝石：中子态素压缩机消耗 800 个蓝宝石产出，用于合成无尽催化剂。</summary>
    public sealed class SapphireSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Blue;
        protected override int RequiredItemType => ItemID.Sapphire;
        public override int RequiredQuantity => 800;
    }
}