namespace AvaritiaMod.Content.Tiles
{
    public sealed class DoubleCompressedCraftingTableTile : CraftingTableTile<DoubleCompressedCraftingTableUI, DoubleCompressedCraftingTableTileEntity, DoubleCompressedCraftingTable>
    {
        public override void SetStaticDefaults()
        {
            HitSound = SoundID.Dig;
            DustType = DustID.WoodFurniture;
            MinPick = 140;
            base.SetStaticDefaults();
        }
    }
}