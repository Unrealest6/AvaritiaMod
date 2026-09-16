namespace AvaritiaMod.Common.Systems
{
    /// <summary>
    /// 工作台UI系统
    /// </summary>
    public sealed class CraftingTableUISystem : AvaritiaUISystem<CraftingTableUI, CraftingTableTileEntity>
    {
        protected override void ShowUITileEntity(CraftingTableTileEntity tileEntity)
        {
            tileEntity.CraftingTableUI = CurrentUI;
        }
    }
}