namespace AvaritiaMod.Common.Systems
{
    public sealed class NeutroniumCompressorUISystem : AvaritiaUISystem<NeutroniumCompressorUI, NeutroniumCompressorTileEntity>
    {
        protected override void ShowUITileEntity(NeutroniumCompressorTileEntity tileEntity)
        {
            tileEntity.NeutroniumCompressorUI = CurrentUI;
        }
    }
}