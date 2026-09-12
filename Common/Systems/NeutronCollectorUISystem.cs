namespace AvaritiaMod.Common.Systems
{
    public sealed class NeutronCollectorUISystem : AvaritiaUISystem<NeutronCollectorUI, NeutronCollectorTileEntity>
    {
        protected override void ShowUITileEntity(NeutronCollectorTileEntity tileEntity)
        {
            tileEntity.NeutronCollectorUI = CurrentUI;
        }
    }
}