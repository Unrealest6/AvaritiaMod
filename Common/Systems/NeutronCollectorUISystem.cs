namespace AvaritiaMod.Common.Systems
{
    /// <summary>
    /// 中子态素收集器UI系统
    /// </summary>
    public sealed class NeutronCollectorUISystem : AvaritiaUISystem<NeutronCollectorUI, NeutronCollectorTileEntity>
    {
        protected override void ShowUITileEntity(NeutronCollectorTileEntity tileEntity)
        {
            tileEntity.NeutronCollectorUI = CurrentUI;
        }
    }
}