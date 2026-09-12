namespace AvaritiaMod.Common.Systems
{
    public sealed class CraftingTableUISystem : AvaritiaUISystem<CraftingTableUI, CraftingTableTileEntity>
    {
        protected override void ShowUITileEntity(CraftingTableTileEntity tileEntity)
        {
            tileEntity.CraftingTableUI = CurrentUI;
        }
    }
}