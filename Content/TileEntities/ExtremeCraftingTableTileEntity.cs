namespace AvaritiaMod.Content.TileEntities
{
    public sealed class ExtremeCraftingTableTileEntity : CraftingTableTileEntity
    {
        public override Bounded<byte, RecipeSize> Size => 9;
        public override ushort TileType => (ushort)ModContent.TileType<ExtremeCraftingTableTile>();
    }
}