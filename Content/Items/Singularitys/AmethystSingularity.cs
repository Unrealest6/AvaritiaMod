namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-紫晶：中子态素压缩机消耗 800 个紫晶产出，用于合成无尽催化剂。</summary>
    public sealed class AmethystSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Purple;
        protected override int RequiredItemType => ItemID.Amethyst;
        public override int RequiredQuantity => 800;
    }
}