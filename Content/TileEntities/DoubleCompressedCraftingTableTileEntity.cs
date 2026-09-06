namespace AvaritiaMod.Content.TileEntities
{
    public sealed class DoubleCompressedCraftingTableTileEntity : CraftingTableTileEntity
    {
        public override BoundedSize Size => 6;
        public override ushort TileType => (ushort)ModContent.TileType<DoubleCompressedCraftingTableTile>();
    }
}