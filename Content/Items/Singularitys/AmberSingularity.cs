namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-琥珀：中子态素压缩机消耗 600 个琥珀产出，用于合成无尽催化剂。</summary>
    public sealed class AmberSingularity : Singularity
    {
        protected override Color OverlayColor => new(191, 82, 0);
        protected override int RequiredItemType => ItemID.Amber;
        public override int RequiredQuantity => 600;
    }
}