namespace AvaritiaMod.Content.Tiles
{
    public sealed class CompressedCraftingTableTile : CraftingTableTile<CompressedCraftingTableUI, CompressedCraftingTableTileEntity, CompressedCraftingTable>
    {
        public override void SetStaticDefaults()
        {
            HitSound = SoundID.Dig;
            DustType = DustID.WoodFurniture;
            MinPick = 70;
            base.SetStaticDefaults();
        }
    }
}