namespace AvaritiaMod.Content.TileEntities
{
    public sealed class DoubleCompressedCraftingTableTileEntity : CraftingTableTileEntity
    {
        public override Bounded<byte, RecipeSize> Size => 6;
        public override ushort TileType => (ushort)ModContent.TileType<DoubleCompressedCraftingTableTile>();
    }
}