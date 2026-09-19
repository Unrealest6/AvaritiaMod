namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-魔矿：中子态素压缩机消耗 600 个魔矿锭产出，用于合成无尽催化剂。</summary>
    public sealed class DemoniteSingularity : Singularity
    {
        protected override Color OverlayColor => Color.DarkSlateBlue;
        protected override int RequiredItemType => ItemID.DemoniteBar;
        public override int RequiredQuantity => 600;
    }
}