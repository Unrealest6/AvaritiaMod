using AvaritiaMod;

namespace AvaritiaMod.Content.TileEntities
{
    public sealed class ExtremeCraftingTableTileEntity : CraftingTableTileEntity
    {
        public override BoundedSize Size => 9;
        public override ushort TileType => (ushort)ModContent.TileType<ExtremeCraftingTableTile>();
    }
}