namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-翡翠：中子态素压缩机消耗 800 个翡翠产出，用于合成无尽催化剂。</summary>
    public sealed class EmeraldSingularity : Singularity
    {
        protected override Color OverlayColor => Color.Green;
        protected override int RequiredItemType => ItemID.Emerald;
        public override int RequiredQuantity => 800;
    }
}