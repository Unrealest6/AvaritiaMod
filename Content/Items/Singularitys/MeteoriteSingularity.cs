namespace AvaritiaMod.Content.Items.Singularitys
{
    /// <summary>奇点-陨石：中子态素压缩机消耗 100 个陨石锭产出，用于合成无尽催化剂。</summary>
    public sealed class MeteoriteSingularity : Singularity
    {
        protected override Color OverlayColor => new(95, 71, 82);
        protected override int RequiredItemType => ItemID.MeteoriteBar;
        public override int RequiredQuantity => 100;
    }
}