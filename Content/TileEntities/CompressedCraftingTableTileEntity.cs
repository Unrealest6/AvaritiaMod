namespace AvaritiaMod.Content.TileEntities
{
    public sealed class CompressedCraftingTableTileEntity : CraftingTableTileEntity
    {
        public override BoundedSize Size => 3;
        public override ushort TileType => (ushort)ModContent.TileType<CompressedCraftingTableTile>();
    }
}