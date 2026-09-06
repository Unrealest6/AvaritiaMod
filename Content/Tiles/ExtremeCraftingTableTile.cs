namespace AvaritiaMod.Content.Tiles
{
    public sealed class ExtremeCraftingTableTile : CraftingTableTile<ExtremeCraftingTableUI, ExtremeCraftingTableTileEntity, ExtremeCraftingTable>
    {
        protected override bool IsSolid => false;
        public override void SetStaticDefaults()
        {
            HitSound = SoundID.Dig;
            DustType = DustID.MoonBoulder;
            MinPick = 210;
            base.SetStaticDefaults();
        }
        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset) => frameYOffset += (int)(36 * (Main.GameUpdateCount / 9 % 5));
    }
}