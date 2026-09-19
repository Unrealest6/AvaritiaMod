namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-猩红：中子态素压缩机消耗 600 个猩红锭产出，用于合成无尽催化剂。</summary>
    public sealed class CrimtaneSingularity : Singularity
    {
        protected override Color OverlayColor => new(54, 10, 27);
        protected override int RequiredItemType => ItemID.CrimtaneBar;
        public override int RequiredQuantity => 600;
    }
}